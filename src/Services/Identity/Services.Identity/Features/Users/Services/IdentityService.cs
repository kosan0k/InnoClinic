using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Identity.Features.Users.Models;
using Services.Identity.Shared.Configurations;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Services.Identity.Features.Users.Services;

/// <summary>
/// Implementation of identity management operations using Keycloak Admin API.
/// Authenticates using Client Credentials Flow (Service Account).
/// </summary>
public class IdentityService : IIdentityService
{
    // Internal DTOs for Keycloak API response
    public class KeycloakTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; init; } = string.Empty;
    }

    private readonly HttpClient _httpClient;
    private readonly ILogger<IdentityService> _logger;
    private readonly AuthOptions _authOptions;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public IdentityService(
        HttpClient httpClient,
        ILogger<IdentityService> logger,
        IOptions<AuthOptions> authOptions)
    {
        _httpClient = httpClient;
        _logger = logger;
        _authOptions = authOptions.Value;
    }

    public async Task<Result<string, Exception>> RegisterUserAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var tokenResult = await EnsureAdminTokenAsync(cancellationToken);

        if (tokenResult.IsFailure)
            return tokenResult.Error;

        try
        {
            using var createUserHttpRequestMessage = GetCreateUserRequestMessage(request);
            createUserHttpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Value);

            return await Result
                .Try(
                    func: () => _httpClient.SendAsync(createUserHttpRequestMessage, cancellationToken),
                    errorHandler: ex => new Exception("Failed to send create user request", ex))
                .CheckIf(
                    predicate: response => !response.IsSuccessStatusCode,
                    func: async response =>
                    {
                        var error = await response.Content.ReadAsStringAsync(cancellationToken);
                        return new Exception($"Create user request failed with status code {response.StatusCode}: {error}");
                    })
                // Keycloak may return the user ID in the Location header
                .Map(response => response.Headers.Location?.ToString() ?? string.Empty)
                .BindIf(
                    predicate: locationHeader => !string.IsNullOrEmpty(locationHeader),
                    func: locationHeader =>
                    {
                        var userId = locationHeader!.Split('/').LastOrDefault();
                        return Result.SuccessIf(
                            isSuccess: !string.IsNullOrEmpty(userId),
                            value: userId!,
                            error: new Exception("Failed to parse user ID from response"));
                    })
                // Fallback: search for the user by email
                .OnFailureCompensate(
                    _ => GetUserByEmailAsync(request.Email, cancellationToken)
                            .Map(user => user.Id))
                // Log error on failure
                .TapError(err => _logger.LogError(err, "Error on registering user"));
        }
        catch (Exception ex)
        {
            var error = new Exception($"Exception while creating user {request.Username}", ex);
            return error;
        }
    }

    private async Task<Result<KeycloakUser, Exception>> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var tokenResult = await EnsureAdminTokenAsync(cancellationToken);

        if (tokenResult.IsFailure)
            return tokenResult.Error;

        try
        {
            var endpoint = $"{_authOptions.KeycloakBaseUrl}/admin/realms/{_authOptions.Realm}/users?email={Uri.EscapeDataString(email)}&exact=true";

            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Value);

            return await Result
                .Try(
                    func: () => _httpClient.SendAsync(request, cancellationToken),
                    errorHandler: ex => new Exception("Failed to send get user by email request", ex))
                .CheckIf(
                    predicate: response => !response.IsSuccessStatusCode,
                    func: response => new Exception($"Get user by email request failed with status code {response.StatusCode}"))
                .MapTry(
                    func: async response =>
                    {
                        var users = await response.Content.ReadFromJsonAsync<List<KeycloakUser>>(JsonOptions, cancellationToken);
                        var user = users?.FirstOrDefault();
                        return user ?? throw new Exception($"User with email '{email}' not found");
                    },
                    errorHandler: ex => new Exception("Failed to read user response", ex));
        }
        catch (Exception ex)
        {
            var error = new Exception($"Exception while getting user by email {email}", ex);
            return error;
        }
    }

    /// <summary>
    /// Ensures we have a valid admin token, refreshing if necessary.
    /// Uses Client Credentials Flow for Service Account authentication.
    /// </summary>
    private async Task<Result<string, Exception>> EnsureAdminTokenAsync(CancellationToken cancellationToken)
    {
        bool TokenIsValid() => !string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry.AddMinutes(-1);

        // Quick check without lock
        if (TokenIsValid())
            return _accessToken!;

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (TokenIsValid())
            {
                return _accessToken!;
            }
            else
            {
                return await RefreshAdminTokenAsync(cancellationToken);
            }
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<Result<string, Exception>> RefreshAdminTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var tokenEndpoint = $"{_authOptions.KeycloakBaseUrl}/realms/{_authOptions.Realm}/protocol/openid-connect/token";

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _authOptions.AdminClientId,
                ["client_secret"] = _authOptions.AdminClientSecret
            });

            return await Result
                .Try(
                    func: () => _httpClient.PostAsync(tokenEndpoint, content, cancellationToken),
                    errorHandler: ex => new Exception("Failed to send token request", ex))
                .CheckIf(
                    predicate: response => !response.IsSuccessStatusCode,
                    func: response => new Exception($"Token request failed with status code {response.StatusCode}"))
                .MapTry(
                    func: response => response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(JsonOptions, cancellationToken),
                    errorHandler: ex => new Exception("Failed to read token response", ex))
                .CheckIf(
                    predicate: tokenResponse => tokenResponse?.AccessToken is null,
                    func: _ => new Exception("Token response was empty"))
                .Tap(
                    tokenResponse =>
                    {
                        _accessToken = tokenResponse!.AccessToken;
                        _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                        _logger.LogDebug($"Successfully obtained admin token, expires in {tokenResponse.ExpiresIn} seconds");
                    })
                .Map(tokenResponse => tokenResponse!.AccessToken);
        }
        catch (Exception ex)
        {
            var error = new Exception("Exception while obtaining admin token", ex);
            return error;
        }
    }

    private HttpRequestMessage GetCreateUserRequestMessage(RegisterUserRequest request)
    {
        var usersEndpoint = $"{_authOptions.KeycloakBaseUrl}/admin/realms/{_authOptions.Realm}/users";

        var keycloakUser = new
        {
            username = request.Username,
            email = request.Email,
            firstName = request.FirstName,
            lastName = request.LastName,
            enabled = request.Enabled,
            emailVerified = request.EmailVerified,
            attributes = request.Attributes,
            credentials = new[]
            {
                    new
                    {
                        type = "password",
                        value = request.Password,
                        temporary = false
                    }
                }
        };

        var json = JsonSerializer.Serialize(keycloakUser, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, usersEndpoint)
        {
            Content = content
        };

        return httpRequest;
    }
}