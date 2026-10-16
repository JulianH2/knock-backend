using Microsoft.EntityFrameworkCore;

namespace Knock.Api.Modules.Catalog.Data;

internal sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalog");
        modelBuilder.HasPostgresExtension("postgis");
    }
}
