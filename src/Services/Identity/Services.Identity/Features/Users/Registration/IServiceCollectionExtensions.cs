using Microsoft.Extensions.DependencyInjection;
using Services.Identity.Features.Users.Handlers;
using Services.Identity.Features.Users.Services;

namespace Services.Identity.Features.Users.Registration;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection UseKeycloakIdentityService(this IServiceCollection services)
    {
        // Add HTTP client factory
        services.AddHttpClient();

        services.AddMemoryCache();
        services.AddHttpClient<KeycloakTokenService>();
        services.AddTransient<KeycloakAuthHandler>();

        // Add Identity Service (Keycloak Admin API)
        services
            .AddHttpClient<IIdentityService, KeycloakIdentityService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(5);
            })
            .AddHttpMessageHandler<KeycloakAuthHandler>();

        return services;
    }
}
