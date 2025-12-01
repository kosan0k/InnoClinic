using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Services.Identity.Configurations;
using Services.Identity.Constants;

namespace Services.Identity.Api.Controllers;

/// <summary>
/// Controller for handling authentication flows with Keycloak.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthOptions _authOptions;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IOptions<AuthOptions> authOptions,
        ILogger<AuthController> logger)
    {
        _authOptions = authOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Redirects the user to the Keycloak login page.
    /// </summary>
    /// <param name="returnUrl">The URL to redirect to after successful authentication.</param>
    /// <returns>A redirect to Keycloak's authorization endpoint.</returns>
    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
        var redirectUri = !string.IsNullOrEmpty(returnUrl) 
            ? returnUrl 
            : Url.Action(nameof(LoginCallback), "Auth", null, Request.Scheme);

        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUri,
            IsPersistent = true
        };

        _logger.LogInformation("Initiating login redirect to Keycloak. RedirectUri: {RedirectUri}", redirectUri);
        
        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Callback endpoint after successful Keycloak authentication.
    /// </summary>
    [HttpGet("callback")]
    [Authorize]
    public IActionResult LoginCallback()
    {
        var userId = User.FindFirst(AuthConstants.Keycloak.Claims.Subject)?.Value;
        var username = User.FindFirst(AuthConstants.Keycloak.Claims.PreferredUsername)?.Value;
        
        _logger.LogInformation("User {Username} ({UserId}) logged in successfully", username, userId);

        return Ok(new
        {
            message = "Authentication successful",
            userId,
            username,
            isAuthenticated = true
        });
    }

    /// <summary>
    /// Logs out the user from both the application and Keycloak.
    /// </summary>
    /// <param name="returnUrl">The URL to redirect to after logout.</param>
    [HttpGet("logout")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromQuery] string? returnUrl = null)
    {
        var userId = User.FindFirst(AuthConstants.Keycloak.Claims.Subject)?.Value;
        _logger.LogInformation("User {UserId} logging out", userId);

        // Sign out from the local cookie
        await HttpContext.SignOutAsync();

        // Build the Keycloak logout URL
        var logoutUrl = BuildKeycloakLogoutUrl(returnUrl);
        
        return Redirect(logoutUrl);
    }

    /// <summary>
    /// Returns the Keycloak authorization URL for manual integration.
    /// </summary>
    /// <param name="redirectUri">The redirect URI after authentication.</param>
    /// <param name="state">Optional state parameter for CSRF protection.</param>
    /// <param name="scope">OAuth scopes (default: openid profile email).</param>
    [HttpGet("authorize-url")]
    [AllowAnonymous]
    public IActionResult GetAuthorizeUrl(
        [FromQuery] string redirectUri,
        [FromQuery] string? state = null,
        [FromQuery] string scope = "openid profile email")
    {
        if (string.IsNullOrEmpty(redirectUri))
        {
            return BadRequest(new { error = "redirect_uri is required" });
        }

        var authorizeUrl = BuildKeycloakAuthorizeUrl(redirectUri, state, scope);
        
        return Ok(new
        {
            authorizeUrl,
            clientId = _authOptions.ClientId,
            realm = _authOptions.Realm
        });
    }

    /// <summary>
    /// Exchanges an authorization code for tokens.
    /// </summary>
    /// <param name="code">The authorization code from Keycloak.</param>
    /// <param name="redirectUri">The redirect URI used in the authorization request.</param>
    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<IActionResult> ExchangeToken(
        [FromForm] string code,
        [FromForm] string redirectUri)
    {
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(redirectUri))
        {
            return BadRequest(new { error = "code and redirect_uri are required" });
        }

        try
        {
            var tokenEndpoint = $"{_authOptions.KeycloakBaseUrl}/realms/{_authOptions.Realm}/protocol/openid-connect/token";

            using var httpClient = new HttpClient();
            var tokenRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = _authOptions.ClientId,
                ["client_secret"] = _authOptions.ClientSecret,
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            };

            var response = await httpClient.PostAsync(
                tokenEndpoint, 
                new FormUrlEncodedContent(tokenRequest));

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Token exchange failed: {StatusCode} - {Content}", 
                    response.StatusCode, content);
                return StatusCode((int)response.StatusCode, content);
            }

            return Content(content, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging authorization code for tokens");
            return StatusCode(500, new { error = "token_exchange_failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromForm] string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(new { error = "refresh_token is required" });
        }

        try
        {
            var tokenEndpoint = $"{_authOptions.KeycloakBaseUrl}/realms/{_authOptions.Realm}/protocol/openid-connect/token";

            using var httpClient = new HttpClient();
            var tokenRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = _authOptions.ClientId,
                ["client_secret"] = _authOptions.ClientSecret,
                ["refresh_token"] = refreshToken
            };

            var response = await httpClient.PostAsync(
                tokenEndpoint,
                new FormUrlEncodedContent(tokenRequest));

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Token refresh failed: {StatusCode} - {Content}",
                    response.StatusCode, content);
                return StatusCode((int)response.StatusCode, content);
            }

            return Content(content, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return StatusCode(500, new { error = "token_refresh_failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Returns information about the current authentication status.
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult GetAuthStatus()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Ok(new
            {
                isAuthenticated = false,
                loginUrl = Url.Action(nameof(Login), "Auth", null, Request.Scheme)
            });
        }

        return Ok(new
        {
            isAuthenticated = true,
            userId = User.FindFirst(AuthConstants.Keycloak.Claims.Subject)?.Value,
            username = User.FindFirst(AuthConstants.Keycloak.Claims.PreferredUsername)?.Value,
            email = User.FindFirst(AuthConstants.Keycloak.Claims.Email)?.Value,
            logoutUrl = Url.Action(nameof(Logout), "Auth", null, Request.Scheme)
        });
    }

    private string BuildKeycloakAuthorizeUrl(string redirectUri, string? state, string scope)
    {
        var baseUrl = $"{_authOptions.KeycloakBaseUrl}/realms/{_authOptions.Realm}/protocol/openid-connect/auth";
        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = _authOptions.ClientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = scope
        };

        if (!string.IsNullOrEmpty(state))
        {
            queryParams["state"] = state;
        }

        var queryString = string.Join("&", queryParams.Select(kv => 
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        return $"{baseUrl}?{queryString}";
    }

    private string BuildKeycloakLogoutUrl(string? postLogoutRedirectUri)
    {
        var logoutUrl = $"{_authOptions.KeycloakBaseUrl}/realms/{_authOptions.Realm}/protocol/openid-connect/logout";
        
        if (!string.IsNullOrEmpty(postLogoutRedirectUri))
        {
            logoutUrl += $"?post_logout_redirect_uri={Uri.EscapeDataString(postLogoutRedirectUri)}&client_id={_authOptions.ClientId}";
        }

        return logoutUrl;
    }
}



