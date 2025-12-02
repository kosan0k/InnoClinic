using Microsoft.Extensions.DependencyInjection;
using Services.Identity.Features.Session.Service;

namespace Services.Identity.Features.Session.Registration;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection UseSessionRevocation(this IServiceCollection services)
        => services.AddScoped<ISessionRevocationService, SessionRevocationService>();
}
