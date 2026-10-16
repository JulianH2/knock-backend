using Knock.Api.Modules.Catalog.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Knock.IntegrationTest.Support;

/// <summary>Prepara la base de pruebas del módulo Catalog: migraciones y semillas versionadas de <c>db/seeds</c>.</summary>
internal static class CatalogTestDatabase
{
    public static string SeedsDirectory { get; } = FindSeedsDirectory();

    public static async Task MigrateAndSeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
        await SeedAsync(scope.ServiceProvider);
    }

    public static Task SeedAsync(IServiceProvider scopedServices) =>
        scopedServices.GetRequiredService<CatalogSeeder>().SeedAsync(SeedsDirectory, CancellationToken.None);

    private static string FindSeedsDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Knock.slnx")))
        {
            directory = directory.Parent;
        }

        return directory is null
            ? throw new InvalidOperationException("Knock.slnx was not found above the test output directory.")
            : Path.Combine(directory.FullName, "db", "seeds");
    }
}
