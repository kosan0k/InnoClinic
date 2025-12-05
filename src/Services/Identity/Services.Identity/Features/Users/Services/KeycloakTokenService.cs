using CSharpFunctionalExtensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Services.Identity.Shared.Configurations;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Services.Identity.Features.Users.Services;

public class KeycloakTokenService
{
    // Internal DTO
    private class KeycloakTokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; init; } = "";
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; init; }
    }

    private readonly AuthOptions _options;
    private readonly IMemoryCache _cache;
    private readonly HttpClient _httpClient;

    // Static lock to ensure only one refresh happens across the application instance
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public KeycloakTokenService(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<AuthOptions> authOptions)
    {
        _options = authOptions.Value;
        _cache = cache;
        _httpClient = httpClient;
    }

    public async Task<Result<string, Exception>> GetTokenAsync(CancellationToken ct = default)
    {
        // Try get from cache
        if (_cache.TryGetValue("KeycloakAdminToken", out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
            return cachedToken!;

        await _lock.WaitAsync(ct);
        try
        {
            // Double-check cache after lock
            if (_cache.TryGetValue("KeycloakAdminToken", out cachedToken) && !string.IsNullOrEmpty(cachedToken))
                return cachedToken!;

            // Execute Refresh
            return await FetchNewTokenAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private Task<Result<string, Exception>> FetchNewTokenAsync(CancellationToken ct)
    {
        var endpoint = $"{_options.KeycloakBaseUrl}/realms/{_options.Realm}/protocol/openid-connect/token";
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret
        });

        return Result
            .Try(
                func: () => _httpClient.PostAsync(endpoint, content, ct),
                errorHandler: ex => new Exception("Failed to send token request", ex))
            .CheckIf(
                predicate: response => !response.IsSuccessStatusCode,
                func: response => new Exception($"Token request failed with status code {response.StatusCode}"))
            .MapTry(
                func: response => response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(ct),
                errorHandler: ex => new Exception("Failed to read token response", ex))
            .CheckIf(
                predicate: tokenResponse => tokenResponse?.AccessToken is null,
                func: _ => new Exception("Token response was empty"))
            .Map(dto =>
            {
                // Cache the token, subtracting 60 seconds for safety buffer
                var expiry = TimeSpan.FromSeconds(dto!.ExpiresIn - 60);
                _cache.Set("KeycloakAdminToken", dto.AccessToken, expiry);
                return dto.AccessToken;
            });
    }
}