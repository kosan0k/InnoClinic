using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Identity.Models;
using Services.Identity.Shared.Configurations;

namespace Services.Identity.Features.Auth.Services;

/// <summary>
/// Implementation of identity management operations using Keycloak Admin API.
/// Authenticates using Client Credentials Flow (Service Account).
/// </summary>
public class IdentityService : IIdentityService
{
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

    /// <summary>
    /// Ensures we have a valid admin token, refreshing if necessary.
    /// Uses Client Credentials Flow for Service Account authentication.
    /// </summary>
    private async Task<Result<string>> EnsureAdminTokenAsync(CancellationToken cancellationToken)
    {
        // Quick check without lock
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry.AddMinutes(-1))
        {
            return Result.Success(_accessToken);
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry.AddMinutes(-1))
            {
                return Result.Success(_accessToken);
            }

            return await RefreshAdminTokenAsync(cancellationToken);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<Result<string>> RefreshAdminTokenAsync(CancellationToken cancellationToken)
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

            var response = await _httpClient.PostAsync(tokenEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to obtain admin token: {StatusCode} - {Error}",
                    response.StatusCode, error);
                return Result.Failure<string>($"Failed to obtain admin token: {response.StatusCode}");
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions, cancellationToken);
            
            if (tokenResponse?.AccessToken == null)
            {
                return Result.Failure<string>("Token response was empty");
            }

            _accessToken = tokenResponse.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

            _logger.LogDebug("Successfully obtained admin token, expires in {ExpiresIn} seconds",
                tokenResponse.ExpiresIn);

            return Result.Success(_accessToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while obtaining admin token");
            return Result.Failure<string>($"Exception while obtaining admin token: {ex.Message}");
        }
    }

    public async Task<Result<string>> RegisterUserAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        var tokenResult = await EnsureAdminTokenAsync(cancellationToken);
        if (tokenResult.IsFailure)
        {
            return Result.Failure<string>(tokenResult.Error);
        }

        try
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

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, usersEndpoint)
            {
                Content = content
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Value);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to create user {Username}: {StatusCode} - {Error}",
                    request.Username, response.StatusCode, error);
                return Result.Failure<string>($"Failed to create user: {response.StatusCode} - {error}");
            }

            // Keycloak returns the user ID in the Location header
            var locationHeader = response.Headers.Location?.ToString();
            if (string.IsNullOrEmpty(locationHeader))
            {
                // Fallback: search for the user by username
                var userResult = await GetUserByEmailAsync(request.Email, cancellationToken);
                if (userResult.IsFailure)
                {
                    return Result.Failure<string>("User created but failed to retrieve user ID");
                }
                return Result.Success(userResult.Value.Id);
            }

            var userId = locationHeader.Split('/').LastOrDefault();
            if (string.IsNullOrEmpty(userId))
            {
                return Result.Failure<string>("Failed to parse user ID from response");
            }

            _logger.LogInformation("Successfully created user {Username} with ID {UserId}",
                request.Username, userId);

            return Result.Success(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while creating user {Username}", request.Username);
            return Result.Failure<string>($"Exception while creating user: {ex.Message}");
        }
    }

    public async Task<Result> AssignRealmRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
    {
        var tokenResult = await EnsureAdminTokenAsync(cancellationToken);
        if (tokenResult.IsFailure)
        {
            return Result.Failure(tokenResult.Error);
        }

        try
        {
            // First, get the role representation
            var roleEndpoint = $"{_authOptions.KeycloakBaseUrl}/admin/realms/{_authOptions.Realm}/roles/{roleName}";
            
            using var getRoleRequest = new HttpRequestMessage(HttpMethod.Get, roleEndpoint);
            getRoleRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Value);

            var roleResponse = await _httpClient.SendAsync(getRoleRequest, cancellationToken);
            if (!roleResponse.IsSuccessStatusCode)
            {
                var error = await roleResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to get role {RoleName}: {StatusCode} - {Error}",
                    roleName, roleResponse.StatusCode, error);
                return Result.Failure($"Role '{roleName}' not found");
            }

            var role = await roleResponse.Content.ReadFromJsonAsync<RoleRepresentation>(JsonOptions, cancellationToken);
            if (role == null)
            {
                return Result.Failure($"Failed to parse role '{roleName}'");
            }

            // Now assign the role to the user
            var assignEndpoint = $"{_authOptions.KeycloakBaseUrl}/admin/realms/{_authOptions.Realm}/users/{userId}/role-mappings/realm";
            
            var json = JsonSerializer.Serialize(new[] { role }, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var assignRequest = new HttpRequestMessage(HttpMethod.Post, assignEndpoint)
            {
                Content = content
            };
            assignRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Value);

            var assignResponse = await _httpClient.SendAsync(assignRequest, cancellationToken);
            
            if (!assignResponse.IsSuccessStatusCode)
            {
                var error = await assignResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to assign role {RoleName} to user {UserId}: {StatusCode} - {Error}",
                    roleName, userId, assignResponse.StatusCode, error);
                return Result.Failure($"Failed to assign role: {assignResponse.StatusCode}");
            }

            _logger.LogInformation("Successfully assigned realm role {RoleName} to user {UserId}",
                roleName, userId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while assigning role {RoleName} to user {UserId}",
                roleName, userId);
            return Result.Failure($"Exception while assigning role: {ex.Message}");
        }
    }

    public async Task<Result> AssignClientRoleAsync(string userId, string clientId, string roleName, CancellationToken cancellationToken = default)
    {
        var tokenResult = await EnsureAdminTokenAsync(cancellationToken);
        if (tokenResult.IsFailure)
        {
            return Result.Failure(tokenResult.Error);
        }

        try
        {
            // First, get the client's internal ID
            var clientsEndpoint = $"{_authOptions.KeycloakBaseUrl}/admin/realms/{_authOptions.Realm}/clients?clientId={clientId}";
            
            using var getClientRequest = new HttpRequestMessage(HttpMethod.Get, clientsEndpoint);
            getClientRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Value);

            var clientResponse = await _httpClient.SendAsync(getClientRequest, cancellationToken);
            if (!clientResponse.IsSuccessStatusCode)
            {
                return Result.Failure($"Failed to find client '{clientId}'");
            }

            var clients = await clientResponse.Content.ReadFromJsonAsync<List<ClientRepresentation>>(JsonOptions, cancellationToken);
            var client = clients?.FirstOrDefault();
            if (client == null)
            {
                return Result.Failure($"Client '{clientId}' not found");
            }

            // Get the role from the client
            var roleEndpoint = $"{_authOptions.KeycloakBaseUrl}/admin/realms/{_authOptions.Realm}/clients/{client.Id}/roles/{roleName}";
            
            using var getRoleRequest = new HttpRequestMessage(HttpMethod.Get, roleEndpoint);
            getRoleRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Value);

            var roleResponse = await _httpClient.SendAsync(getRoleRequest, cancellationToken);
            if (!roleResponse.IsSuccessStatusCode)
            {
                return Result.Failure($"Role '{roleName}' not found in client '{clientId}'");
            }

            var role = await roleResponse.Content.ReadFromJsonAsync<RoleRepresentation>(JsonOptions, cancellationToken);
            if (role == null)
            {
                return Result.Failure($"Failed to parse role '{roleName}'");
            }

            // Assign the client role to the user
            var assignEndpoint = $"{_authOptions.KeycloakBaseUrl}/admin/realms/{_authOptions.Realm}/users/{userId}/role-mappings/clients/{client.Id}";
            
            var json = JsonSerializer.Serialize(new[] { role }, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var assignRequest = new HttpRequestMessage(HttpMethod.Post, assignEndpoint)
            {
                Content = content
            };
            assignRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Value);

            var assignResponse = await _httpClient.SendAsync(assignRequest, cancellationToken);
            
            if (!assignResponse.IsSuccessStatusCode)
            {
                var error = await assignResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to assign client role {ClientId}/{RoleName} to user {UserId}: {StatusCode} - {Error}",
                    clientId, roleName, userId, assignResponse.StatusCode, error);
                return Result.Failure($"Failed to assign client role: {assignResponse.StatusCode}");
            }

            _logger.LogInformation("Successfully assigned client role {ClientId}/{RoleName} to user {UserId}",
                clientId, roleName, userId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while assigning client role {ClientId}/{RoleName} to user {UserId}",
                clientId, roleName, userId);
            return Result.Failure($"Exception while assigning client role: {ex.Message}");
        }
    }

    public async Task<Result<KeycloakUser>> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var tokenResult = await EnsureAdminTokenAsync(cancellationToken);
        if (tokenResult.IsFailure)
        {
            return Result.Failure<KeycloakUser>(tokenResult.Error);
        }

        try
        {
            var endpoint = $"{_authOptions.KeycloakBaseUrl}/admin/realms/{_authOptions.Realm}/users/{userId}";
            
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Value);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure<KeycloakUser>($"User with ID '{userId}' not found");
            }

            var user = await response.Content.ReadFromJsonAsync<KeycloakUser>(JsonOptions, cancellationToken);
            if (user == null)
            {
                return Result.Failure<KeycloakUser>("Failed to parse user response");
            }

            return Result.Success(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting user {UserId}", userId);
            return Result.Failure<KeycloakUser>($"Exception while getting user: {ex.Message}");
        }
    }

    public async Task<Result<KeycloakUser>> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var tokenResult = await EnsureAdminTokenAsync(cancellationToken);
        if (tokenResult.IsFailure)
        {
            return Result.Failure<KeycloakUser>(tokenResult.Error);
        }

        try
        {
            var endpoint = $"{_authOptions.KeycloakBaseUrl}/admin/realms/{_authOptions.Realm}/users?email={Uri.EscapeDataString(email)}&exact=true";
            
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Value);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure<KeycloakUser>($"Failed to search for user with email '{email}'");
            }

            var users = await response.Content.ReadFromJsonAsync<List<KeycloakUser>>(JsonOptions, cancellationToken);
            var user = users?.FirstOrDefault();
            
            if (user == null)
            {
                return Result.Failure<KeycloakUser>($"User with email '{email}' not found");
            }

            return Result.Success(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting user by email {Email}", email);
            return Result.Failure<KeycloakUser>($"Exception while getting user: {ex.Message}");
        }
    }

    public async Task<Result> DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var tokenResult = await EnsureAdminTokenAsync(cancellationToken);
        if (tokenResult.IsFailure)
        {
            return Result.Failure(tokenResult.Error);
        }

        try
        {
            var endpoint = $"{_authOptions.KeycloakBaseUrl}/admin/realms/{_authOptions.Realm}/users/{userId}";
            
            using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Value);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to delete user {UserId}: {StatusCode} - {Error}",
                    userId, response.StatusCode, error);
                return Result.Failure($"Failed to delete user: {response.StatusCode}");
            }

            _logger.LogInformation("Successfully deleted user {UserId}", userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while deleting user {UserId}", userId);
            return Result.Failure($"Exception while deleting user: {ex.Message}");
        }
    }
}

// Internal DTOs for Keycloak API responses
internal record TokenResponse
{
    public string? AccessToken { get; init; }
    public int ExpiresIn { get; init; }
    public string? TokenType { get; init; }
}

internal record RoleRepresentation
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public bool Composite { get; init; }
    public bool ClientRole { get; init; }
    public string? ContainerId { get; init; }
}

internal record ClientRepresentation
{
    public string? Id { get; init; }
    public string? ClientId { get; init; }
    public string? Name { get; init; }
}

