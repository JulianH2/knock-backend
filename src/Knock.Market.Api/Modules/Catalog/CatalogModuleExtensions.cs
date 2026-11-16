using Knock.Contracts.Catalog;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Catalog.Data;
using Knock.Market.Api.Modules.Catalog.Services;
using Microsoft.Extensions.Options;

namespace Knock.Market.Api.Modules.Catalog;

/// <summary>Registro del módulo Catalog. Solo esta clase y <see cref="ICatalogApi"/> son públicas; el resto del módulo es internal.</summary>
public static class CatalogModuleExtensions
{
    /// <summary>Nombre del módulo y clave de su base (<c>Mongo:Databases:Catalog</c>).</summary>
    public const string ModuleKey = "Catalog";

    public static IServiceCollection AddCatalogModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddKnockMongoDatabase(ModuleKey);
        services.AddOptions<CatalogCacheOptions>().BindConfiguration(CatalogCacheOptions.SectionName);
        services.AddSingleton<IValidateOptions<CatalogCacheOptions>, CatalogCacheOptionsValidator>();
        // Sin cadena de Redis la caché queda deshabilitada y las lecturas van a MongoDB (igual que cuando Redis falla);
        // sin esto, la exportación del OpenAPI y los entornos sin Redis no podrían arrancar la API (spec 003: Market arranca sin Redis).
        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString(KnockRedisOptions.ConnectionStringName)))
        {
            services.AddSingleton<ICatalogCache, DisabledCatalogCache>();
        }
        else
        {
            services.AddKnockRedis();
            services.AddSingleton<ICatalogCache, RedisCatalogCache>();
        }
        services.AddSingleton<CatalogStore>();
        services.AddSingleton<IWaitlistStore, WaitlistStore>();
        services.AddSingleton<WaitlistService>();
        services.AddSingleton<NeighborhoodLocator>();
        services.AddSingleton<INeighborhoodLocator>(sp => sp.GetRequiredService<NeighborhoodLocator>());
        services.AddSingleton<INeighborhoodNames, NeighborhoodNames>();
        services.AddScoped<ICatalogApi, CatalogApi>();
        services.AddScoped<INeighborhoodCenterCatalog, CatalogApi>();
        services.AddScoped<CatalogSeeder>();
        services.AddScoped<OficioQueries>();
        services.AddScoped<ZonaService>();
        return services;
    }
}
