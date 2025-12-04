using Microsoft.Extensions.DependencyInjection;
using Services.Identity.Features.Users.Services;

namespace Services.Identity.Features.Users.Registration;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection UseKeycloakIdentityService(this IServiceCollection services)
    {
        // Add HTTP client factory
        services.AddHttpClient();

        // Add Identity Service (Keycloak Admin API)
        services.AddHttpClient<IIdentityService, IdentityService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
