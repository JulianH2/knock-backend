using Knock.Api.Modules.Catalog.Data;
using Knock.Api.Modules.Catalog.Services;
using Microsoft.EntityFrameworkCore;

namespace Knock.Api.Modules.Catalog;

/// <summary>Registro del módulo Catalog. Solo esta clase y <see cref="ICatalogApi"/> son públicas; el resto del módulo es internal.</summary>
public static class CatalogModuleExtensions
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<ICatalogApi, CatalogApi>();
        services.AddDbContext<CatalogDbContext>((provider, options) =>
        {
            var connectionString = provider.GetRequiredService<IConfiguration>()
                .GetConnectionString("KnockDatabase");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:KnockDatabase is not configured.");
            }

            options
                .UseNpgsql(connectionString, postgres => postgres.UseNetTopologySuite())
                .UseSnakeCaseNamingConvention();
        });
        return services;
    }
}
