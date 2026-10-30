using Knock.Api.Modules.Catalog.Data;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.IntegrationTest.Support;

/// <summary>Aplica a la base del módulo Catalog las semillas versionadas de <c>db/seeds/catalog</c> (oficios.json y colonias.json).</summary>
internal static class CatalogTestDatabase
{
    public static string SeedsDirectory { get; } = FindSeedsDirectory();

    public static async Task SeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CatalogSeeder>().SeedAsync(SeedsDirectory, CancellationToken.None);
    }

    /// <summary>Colecciones de la base y documentos de cada una: una consulta no debe crear ni cambiar nada.</summary>
    public static async Task<string> ReadStorageSnapshotAsync(IMongoDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        var names = (await (await database.ListCollectionNamesAsync()).ToListAsync()).Order(StringComparer.Ordinal).ToList();
        var counts = new List<string>();
        foreach (var name in names)
        {
            var count = await database.GetCollection<BsonDocument>(name).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
            counts.Add($"{name}={count}");
        }

        return string.Join(',', counts);
    }

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
