using Services.Identity.Features.Users.Services;
using System.Net.Http.Headers;

namespace Services.Identity.Features.Users.Handlers;

public class KeycloakAuthHandler(KeycloakTokenService tokenService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // 1. Get the token
        var tokenResult = await tokenService.GetTokenAsync(cancellationToken);

        if (tokenResult.IsFailure)
        {
            // Return 401 immediately if we can't get a technical token
            return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
            {
                Content = new StringContent($"Failed to acquire admin token: {tokenResult.Error}")
            };
        }

        // 2. Attach Header
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Value);

        // 3. Continue the request chain
        return await base.SendAsync(request, cancellationToken);
    }
}
