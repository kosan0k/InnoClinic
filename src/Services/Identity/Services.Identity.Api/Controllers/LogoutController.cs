using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Services.Identity.Configurations;
using Services.Identity.Constants;
using Services.Identity.Models;
using Services.Identity.Services;

namespace Services.Identity.Api.Controllers;

/// <summary>
/// Controller for handling OIDC Back-Channel Logout.
/// </summary>
[ApiController]
public class LogoutController : ControllerBase
{
    private readonly ISessionRevocationService _sessionRevocationService;
    private readonly AuthOptions _authOptions;
    private readonly RedisOptions _redisOptions;
    private readonly ILogger<LogoutController> _logger;
    private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;

    public LogoutController(
        ISessionRevocationService sessionRevocationService,
        IOptions<AuthOptions> authOptions,
        IOptions<RedisOptions> redisOptions,
        ILogger<LogoutController> logger,
        IConfigurationManager<OpenIdConnectConfiguration> configurationManager)
    {
        _sessionRevocationService = sessionRevocationService;
        _authOptions = authOptions.Value;
        _redisOptions = redisOptions.Value;
        _logger = logger;
        _configurationManager = configurationManager;
    }

    /// <summary>
    /// OIDC Back-Channel Logout endpoint.
    /// Receives a logout token from Keycloak and revokes the session.
    /// </summary>
    /// <remarks>
    /// This endpoint is called by Keycloak when a user logs out.
    /// It validates the logout token and stores the session ID in Redis
    /// to be checked by the SessionRevocationMiddleware.
    /// </remarks>
    [HttpPost(AuthConstants.Routes.BackChannelLogout)]
    [AllowAnonymous]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> BackChannelLogout(
        [FromForm] BackChannelLogoutRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.LogoutToken))
        {
            _logger.LogWarning("Back-channel logout request received without logout_token");
            return BadRequest(new { error = "invalid_request", error_description = "logout_token is required" });
        }

        try
        {
            // Validate the logout token
            var validationResult = await ValidateLogoutTokenAsync(request.LogoutToken, cancellationToken);
            
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Invalid logout token: {Error}", validationResult.Error);
                return BadRequest(new { error = "invalid_token", error_description = validationResult.Error });
            }

            var sessionId = validationResult.SessionId;
            var subject = validationResult.Subject;

            if (string.IsNullOrEmpty(sessionId))
            {
                _logger.LogWarning("Logout token missing session ID (sid) claim");
                return BadRequest(new { error = "invalid_token", error_description = "sid claim is required" });
            }

            // Revoke the session in Redis
            var ttl = TimeSpan.FromMinutes(_redisOptions.SessionRevocationTtlMinutes);
            var revoked = await _sessionRevocationService.RevokeSessionAsync(sessionId, ttl, cancellationToken);

            if (!revoked)
            {
                _logger.LogError("Failed to revoke session {SessionId}", sessionId);
                return StatusCode(500, new { error = "server_error", error_description = "Failed to revoke session" });
            }

            _logger.LogInformation(
                "Successfully processed back-channel logout for session {SessionId}, subject {Subject}",
                sessionId,
                subject ?? "unknown");

            // Return 200 OK with Cache-Control: no-store as per spec
            Response.Headers.CacheControl = "no-store";
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing back-channel logout");
            return StatusCode(500, new { error = "server_error", error_description = "Internal error processing logout" });
        }
    }

    /// <summary>
    /// Validates the logout token JWT.
    /// </summary>
    private async Task<LogoutTokenValidationResult> ValidateLogoutTokenAsync(
        string token,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get OpenID Connect configuration (includes signing keys)
            var config = await _configurationManager.GetConfigurationAsync(cancellationToken);

            var validationParameters = new TokenValidationParameters
            {
                ValidIssuer = _authOptions.Authority,
                ValidAudience = _authOptions.ClientId,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false, // Logout tokens don't have exp claim
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = config.SigningKeys,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                return LogoutTokenValidationResult.Failure("Invalid token format");
            }

            // Validate the events claim
            var eventsClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "events");
            if (eventsClaim == null)
            {
                return LogoutTokenValidationResult.Failure("Missing events claim");
            }

            // The events claim should contain the back-channel logout event
            if (!eventsClaim.Value.Contains("backchannel-logout", StringComparison.OrdinalIgnoreCase))
            {
                return LogoutTokenValidationResult.Failure("Invalid events claim - not a back-channel logout token");
            }

            // Extract session ID and subject
            var sessionId = jwtToken.Claims.FirstOrDefault(c => c.Type == AuthConstants.Keycloak.Claims.SessionId)?.Value;
            var subject = jwtToken.Claims.FirstOrDefault(c => c.Type == AuthConstants.Keycloak.Claims.Subject)?.Value;

            // Per spec, either sid or sub must be present
            if (string.IsNullOrEmpty(sessionId) && string.IsNullOrEmpty(subject))
            {
                return LogoutTokenValidationResult.Failure("Logout token must contain either sid or sub claim");
            }

            // Validate nonce is NOT present (logout tokens must not have nonce)
            var nonce = jwtToken.Claims.FirstOrDefault(c => c.Type == "nonce");
            if (nonce != null)
            {
                return LogoutTokenValidationResult.Failure("Logout token must not contain nonce claim");
            }

            return LogoutTokenValidationResult.Success(sessionId, subject);
        }
        catch (SecurityTokenValidationException ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return LogoutTokenValidationResult.Failure($"Token validation failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return LogoutTokenValidationResult.Failure("Unexpected error during validation");
        }
    }
}

/// <summary>
/// Result of logout token validation.
/// </summary>
internal class LogoutTokenValidationResult
{
    public bool IsValid { get; private init; }
    public string? SessionId { get; private init; }
    public string? Subject { get; private init; }
    public string? Error { get; private init; }

    public static LogoutTokenValidationResult Success(string? sessionId, string? subject) =>
        new() { IsValid = true, SessionId = sessionId, Subject = subject };

    public static LogoutTokenValidationResult Failure(string error) =>
        new() { IsValid = false, Error = error };
}

