using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Identity.Features.Users.Models;
using Services.Identity.Shared.Configurations;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Services.Identity.Features.Users.Services;

public class KeycloakIdentityService : IIdentityService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<KeycloakIdentityService> _logger;
    private readonly AuthOptions _authOptions;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public KeycloakIdentityService(
        HttpClient httpClient,
        ILogger<KeycloakIdentityService> logger,
        IOptions<AuthOptions> authOptions)
    {
        _httpClient = httpClient;
        _logger = logger;
        _authOptions = authOptions.Value;
    }

    public async Task<Result<string, Exception>> RegisterUserAsync(
        RegisterUserRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var usersEndpoint = $"{_authOptions.KeycloakBaseUrl}/admin/realms/{_authOptions.Realm}/users";
            var payload = CreateUserPayload(request);

            var createResult = await SendCreateRequest(usersEndpoint, payload, ct);

            if (createResult.IsFailure)
                return createResult.Error;

            return await ExtractUserIdFromLocation(response: createResult.Value)
                .OnFailureCompensate(() => GetUserIdByEmailFallback(request.Email, ct))
                .TapError(err => _logger.LogError(err, "Failed to register user {Username}", request.Username));
        }
        catch (Exception ex)
        {
            var error = new Exception($"Unhandled exception while creating user {request.Username}", ex);
            return error;
        }
    }

    private static StringContent CreateUserPayload(RegisterUserRequest request)
    {
        var model = new
        {
            username = request.Username,
            email = request.Email,
            firstName = request.FirstName,
            lastName = request.LastName,
            enabled = request.Enabled,
            emailVerified = request.EmailVerified,
            attributes = request.Attributes,
            credentials = new[] { new { type = "password", value = request.Password, temporary = false } }
        };

        return new StringContent(JsonSerializer.Serialize(model, JsonOptions), Encoding.UTF8, "application/json");
    }

    private Task<Result<HttpResponseMessage, Exception>> SendCreateRequest(string url, StringContent content, CancellationToken ct)
    {
        return Result
            .Try(
                func: () => _httpClient.PostAsync(url, content, ct),
                errorHandler: ex => new Exception("Error on sending create request", ex))
            .Ensure(
                predicate: res => res.IsSuccessStatusCode,
                errorPredicate: res => new Exception($"Keycloak returned {res.StatusCode}"))
            .MapError(ex => new Exception("HTTP request failed", ex));
    }

    private static Result<string, Exception> ExtractUserIdFromLocation(HttpResponseMessage response)
    {
        var location = response.Headers.Location?.ToString();
        var id = location?.Split('/').LastOrDefault();

        return string.IsNullOrEmpty(id)
            ? Result.Failure<string, Exception>(new Exception("Location header missing or invalid"))
            : Result.Success<string, Exception>(id!);
    }

    private Task<Result<string, Exception>> GetUserIdByEmailFallback(string email, CancellationToken ct)
    {
        var endpoint = $"{_authOptions.KeycloakBaseUrl}/admin/realms/{_authOptions.Realm}/users?email={Uri.EscapeDataString(email)}&exact=true";

        return Result
            .Try(
                func: () => _httpClient.GetFromJsonAsync<List<KeycloakUser>>(endpoint, JsonOptions, ct),
                errorHandler: ex => new Exception("Error on requesting users", ex))
            .Map(users => users?.FirstOrDefault())
            .Ensure(
                predicate: user => user != null,
                error:  new Exception($"User {email} not found during fallback lookup"))
            .Map(user => user!.Id)
            .MapError(ex => new Exception("Fallback lookup failed", ex));
    }    
}