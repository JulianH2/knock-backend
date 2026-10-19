using Knock.Api.Modules.Identity.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Knock.Api.Modules.Identity;

/// <summary>Registro del módulo Identity. Solo esta clase y <see cref="IIdentityApi"/> son públicas; el resto del módulo es internal.</summary>
public static class IdentityModuleExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IIdentityApi, IdentityApi>();
        services.AddSingleton<SessionTokenIssuer>();
        services.AddSingleton<IFirebaseIdTokenVerifier, FirebaseIdTokenVerifier>();
        services.AddSingleton<FirebaseSessionService>();
        return services;
    }
}
