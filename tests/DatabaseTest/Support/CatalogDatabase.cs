using System.Collections.Concurrent;
using System.Security.Cryptography;
using Knock.Api.Hosting;
using Knock.Api.Modules.Catalog;
using Knock.Api.Modules.Catalog.Data;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace Knock.DatabaseTest.Support;

/// <summary>
/// Módulo Catalog registrado como en la API sobre documentdb-local (contenedor compartido), con una base propia por instancia
/// para que las pruebas no se pisen. Guarda los comandos que el driver envía a esa base: así se prueba que una entrada inválida
/// no llega al servidor y se repite la consulta real con <c>explain</c>. Solo datos ficticios.
/// </summary>
internal sealed class CatalogDatabase : IAsyncDisposable
{
    private readonly ConcurrentQueue<BsonDocument> _commands = new();
    private readonly ServiceProvider _services;

    private CatalogDatabase(string connectionString)
    {
        DatabaseName = "market_test_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:KnockMongo"] = connectionString,
                ["Mongo:MaxPoolSize"] = "5",
                ["Mongo:Databases:Catalog"] = DatabaseName,
            })
            .Build();
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddCatalogModule();
        services.Replace(ServiceDescriptor.Singleton<IMongoClient>(provider =>
        {
            var settings = MongoExtensions.CreateSettings(provider.GetRequiredService<IOptions<KnockMongoOptions>>().Value);
            settings.ClusterConfigurator = cluster => cluster.Subscribe<CommandStartedEvent>(Record);
            return new MongoClient(settings);
        }));
        _services = services.BuildServiceProvider(validateScopes: true);
    }

    public string DatabaseName { get; }

    public IServiceProvider Services => _services;

    public IMongoDatabase Database => _services.GetRequiredKeyedService<IMongoDatabase>(CatalogModuleExtensions.ModuleKey);

    public IMongoCollection<BsonDocument> Colonias => Database.GetCollection<BsonDocument>(CatalogStore.ColoniasCollection);

    public IMongoCollection<BsonDocument> Oficios => Database.GetCollection<BsonDocument>(CatalogStore.OficiosCollection);

    /// <summary>Comandos enviados a la base de esta instancia, en orden.</summary>
    public IReadOnlyList<BsonDocument> Commands => [.. _commands];

    /// <param name="seeded">Aplica <c>oficios.json</c> y <c>colonias.json</c> con el cargador del módulo.</param>
    public static async Task<CatalogDatabase> CreateAsync(bool seeded)
    {
        var container = await DocumentDbFixture.SharedAsync();
        var database = new CatalogDatabase(container.ConnectionString);
        if (seeded)
        {
            await CatalogTestDatabase.SeedAsync(database.Services);
        }

        return database;
    }

    /// <summary>
    /// Base sembrada para una clase de pruebas que solo lee: un cliente por clase (documentdb-local agota conexiones entre 64 y 96,
    /// P7). Queda viva hasta el final de la ejecución, cuando el reaper de Testcontainers borra el contenedor.
    /// </summary>
    public static Lazy<Task<CatalogDatabase>> SharedSeeded() => new(() => CreateAsync(seeded: true));

    public T Get<T>()
        where T : notnull => _services.GetRequiredService<T>();

    /// <summary>Resuelve <typeparamref name="T"/> en un scope nuevo, como un controller o un módulo vecino.</summary>
    public async Task<TResult> InScopeAsync<T, TResult>(Func<T, Task<TResult>> action)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(action);
        await using var scope = _services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<T>());
    }

    public Task InScopeAsync<T>(Func<T, Task> action)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(action);
        return InScopeAsync<T, bool>(async service =>
        {
            await action(service);
            return true;
        });
    }

    /// <summary>Comandos <c>find</c> enviados desde la última limpieza.</summary>
    public IReadOnlyList<BsonDocument> FindCommands => Commands.Where(command => command.Names.FirstOrDefault() == "find").ToList();

    public void ClearCommands() => _commands.Clear();

    public async ValueTask DisposeAsync()
    {
        await _services.GetRequiredService<IMongoClient>().DropDatabaseAsync(DatabaseName);
        await _services.DisposeAsync();
    }

    private void Record(CommandStartedEvent started)
    {
        if (started.DatabaseNamespace?.DatabaseName == DatabaseName)
        {
            _commands.Enqueue(started.Command.DeepClone().AsBsonDocument);
        }
    }
}
