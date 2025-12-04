using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Services.Identity.Shared.Configurations;

namespace Services.Identity.Features.Auth;

public static class Behaviors
{
    public static Task<IResult> LoginAsync(string? returnUrl)
        => Task.FromResult(
            Results.Challenge(
                new AuthenticationProperties { RedirectUri = returnUrl ?? "/" },
                [OpenIdConnectDefaults.AuthenticationScheme]));

    public async static Task<IResult> LogoutAsync(
        HttpContext context,
        IOptions<AuthOptions> authOptions,
        string? returnUrl)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        var keycloakBaseUrl = authOptions.Value.KeycloakBaseUrl;
        var realm = authOptions.Value.Realm;
        var clientId = authOptions.Value.ClientId;

        var keycloakLogoutUrl = $"{keycloakBaseUrl}/realms/{realm}/protocol/openid-connect/logout";

        if (!string.IsNullOrEmpty(returnUrl))
        {
            keycloakLogoutUrl += $"?post_logout_redirect_uri={Uri.EscapeDataString(returnUrl)}&client_id={clientId}";
        }

        return Results.Redirect(keycloakLogoutUrl);
    }    
}
