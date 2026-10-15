using Knock.Api.Modules.Catalog.Services;

namespace Knock.Api.Modules.Catalog;

/// <summary>Registro del módulo Catalog. Solo esta clase y <see cref="ICatalogApi"/> son públicas; el resto del módulo es internal.</summary>
public static class CatalogModuleExtensions
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<ICatalogApi, CatalogApi>();
        return services;
    }
}
