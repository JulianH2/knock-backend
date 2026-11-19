using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Trust.Data;
using Knock.Market.Api.Modules.Trust.Services;

namespace Knock.Market.Api.Modules.Trust;

/// <summary>Módulo de técnicos de confianza (spec 042): la lista de técnicos que una persona guardó para volver a contratarlos.</summary>
internal static class TrustModuleExtensions
{
    /// <summary>Nombre del módulo y clave de su base (<c>Mongo:Databases:Trust</c>).</summary>
    public const string ModuleKey = "Trust";

    public static IServiceCollection AddTrustModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddKnockMongoDatabase(ModuleKey);
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<TrustStore>();
        services.AddScoped<TrustService>();
        return services;
    }
}
