using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Identity.Shared.Configurations;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Services.Identity.Features.Auth;

/// <summary>
/// Token-related actions for obtaining and refreshing JWT tokens.
/// </summary>
public static class TokenActions
{
    /// <summary>
    /// Response model for token endpoints.
    /// </summary>
    public record TokenResponse(
        string AccessToken,
        string RefreshToken,
        string TokenType,
        int ExpiresIn);

    /// <summary>
    /// Request model for refresh token endpoint.
    /// </summary>
    public record RefreshTokenRequest(string RefreshToken);

    /// <summary>
    /// Gets the current access and refresh tokens from the authenticated session.
    /// </summary>
    public static async Task<IResult> GetTokensAsync(HttpContext context)
    {
        var accessToken = await context.GetTokenAsync("access_token");
        var refreshToken = await context.GetTokenAsync("refresh_token");
        var expiresAt = await context.GetTokenAsync("expires_at");

        if (string.IsNullOrEmpty(accessToken))
        {
            return Results.Unauthorized();
        }

        // Calculate remaining expiration time
        var expiresIn = 0;

        if (!string.IsNullOrEmpty(expiresAt) && 
            DateTimeOffset.TryParse(expiresAt, out var expiration))
        {
            expiresIn = Math.Max(0, (int)(expiration - DateTimeOffset.UtcNow).TotalSeconds);
        }

        return Results.Ok(new TokenResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken ?? string.Empty,
            TokenType: "Bearer",
            ExpiresIn: expiresIn));
    }

    /// <summary>
    /// Refreshes the access token using a refresh token.
    /// </summary>
    public static async Task<IResult> RefreshTokenAsync(
        RefreshTokenRequest request,
        IOptions<AuthOptions> authOptions,
        IHttpClientFactory httpClientFactory,
        ILogger<TokenResponse> logger)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Results.BadRequest(new { Error = "Refresh token is required" });
        }

        var options = authOptions.Value;
        var tokenEndpoint = $"{options.KeycloakBaseUrl}/realms/{options.Realm}/protocol/openid-connect/token";

        try
        {
            using var client = httpClientFactory.CreateClient("KeycloakTokenClient");
            
            var tokenRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = options.ClientId,
                ["client_secret"] = options.ClientSecret,
                ["refresh_token"] = request.RefreshToken
            };

            var response = await client.PostAsync(
                tokenEndpoint,
                new FormUrlEncodedContent(tokenRequest));

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Token refresh failed with status {StatusCode}: {Response}",
                    response.StatusCode,
                    responseContent);

                return response.StatusCode == System.Net.HttpStatusCode.BadRequest
                    ? Results.BadRequest(new { Error = "Invalid or expired refresh token" })
                    : Results.Problem("Failed to refresh token", statusCode: (int)response.StatusCode);
            }

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            var tokenResponse = new TokenResponse(
                AccessToken: root.GetProperty("access_token").GetString()!,
                RefreshToken: root.GetProperty("refresh_token").GetString()!,
                TokenType: root.GetProperty("token_type").GetString() ?? "Bearer",
                ExpiresIn: root.GetProperty("expires_in").GetInt32());

            return Results.Ok(tokenResponse);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error while refreshing token");
            return Results.Problem("Unable to connect to authentication server", statusCode: 503);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Error parsing token response");
            return Results.Problem("Invalid response from authentication server", statusCode: 502);
        }
    }
}

