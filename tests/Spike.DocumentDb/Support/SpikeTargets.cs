using System.Reflection;
using System.Security.Cryptography;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MongoDB.Bson;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit.Sdk;

namespace Knock.Spike.DocumentDb.Support;

/// <summary>Destinos del spike. Las etiquetas de imagen van fijas (spec §3).</summary>
internal static class SpikeTargets
{
    public const string Mongo = "mongo-8.0";
    public const string DocumentDbLocal = "documentdb-local";
    public const string Azure = "azure";

    public const string MongoImage = "mongo:8.0.34";

    // Último build de ghcr.io/microsoft/documentdb/documentdb-local (etiqueta `latest`, creado el 2025-06-17).
    // El proyecto publica las imágenes nuevas en ghcr.io/documentdb/documentdb/documentdb-local; se registra en la evidencia.
    public const string DocumentDbLocalImage =
        "ghcr.io/microsoft/documentdb/documentdb-local@sha256:0fcf634531c1917ad0855ff9f4354aca0a5c5d8e435f08250568deb37eeb0ad5";

    public const ushort DocumentDbPort = 10260;

    /// <summary>Variable con la cadena de Azure. Nunca se imprime ni se registra.</summary>
    public const string AzureVariable = "KNOCK_SPIKE_MONGO";

    public static bool AzureConfigured => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(AzureVariable));
}

/// <summary>Destinos locales (Docker): mongo:8.0 como réplica de un nodo y documentdb-local.</summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class LocalTargetsAttribute : DataAttribute
{
    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        yield return [SpikeTargets.Mongo];
        yield return [SpikeTargets.DocumentDbLocal];
    }
}

/// <summary>Azure DocumentDB; se omite (Skip) si <c>KNOCK_SPIKE_MONGO</c> no existe (spec §3).</summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class AzureTargetAttribute : DataAttribute
{
    public AzureTargetAttribute()
    {
        Skip = SpikeTargets.AzureConfigured ? null : $"{SpikeTargets.AzureVariable} no definida: destino Azure omitido.";
    }

    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        yield return [SpikeTargets.Azure];
    }
}

/// <summary>
/// Arranca cada contenedor una sola vez por ejecución, al primer uso. El reaper de Testcontainers (Ryuk)
/// los elimina al terminar el proceso de pruebas.
/// </summary>
internal static class SpikePool
{
    private const string _localUser = "spike";

    // Contraseña de un solo uso para el contenedor local; no es un secreto del proyecto.
    private static readonly string _localPassword = "Sp1ke" + Convert.ToHexString(RandomNumberGenerator.GetBytes(12));

    private static readonly Lazy<Task<MongoClient>> _mongo = new(StartMongoAsync);
    private static readonly Lazy<Task<MongoClient>> _documentDb = new(StartDocumentDbAsync);
    private static readonly Lazy<MongoClient> _azure = new(CreateAzureClient);

    public static async Task<SpikeDatabase> CreateDatabaseAsync(string target)
    {
        MongoClient client = target switch
        {
            SpikeTargets.Mongo => await _mongo.Value,
            SpikeTargets.DocumentDbLocal => await _documentDb.Value,
            SpikeTargets.Azure => _azure.Value,
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Destino desconocido."),
        };
        return new SpikeDatabase(client, "knock_spike_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(4)));
    }

    private static async Task<MongoClient> StartMongoAsync()
    {
        var container = new MongoDbBuilder(SpikeTargets.MongoImage).WithReplicaSet().Build();
        await container.StartAsync();
        var client = new MongoClient(container.GetConnectionString());
        await WaitUntilPingAsync(client);
        return client;
    }

    private static async Task<MongoClient> StartDocumentDbAsync()
    {
        // Documentación de la imagen: puerto 10260, usuario y contraseña con --username/--password,
        // TLS con certificado autofirmado generado al arrancar.
        IContainer container = new ContainerBuilder(SpikeTargets.DocumentDbLocalImage)
            .WithPortBinding(SpikeTargets.DocumentDbPort, assignRandomHostPort: true)
            .WithCommand("--username", _localUser, "--password", _localPassword)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(SpikeTargets.DocumentDbPort))
            .Build();
        await container.StartAsync();
        var url = new MongoUrlBuilder
        {
            Server = new MongoServerAddress(container.Hostname, container.GetMappedPublicPort(SpikeTargets.DocumentDbPort)),
            Username = _localUser,
            Password = _localPassword,
            UseTls = true,
            AllowInsecureTls = true, // solo el destino local: certificado autofirmado del contenedor
            DirectConnection = true,
        }.ToMongoUrl();
        var client = new MongoClient(url);
        await WaitUntilPingAsync(client);
        return client;
    }

    private static MongoClient CreateAzureClient()
    {
        try
        {
            return new MongoClient(Environment.GetEnvironmentVariable(SpikeTargets.AzureVariable));
        }
        catch (MongoConfigurationException)
        {
            // No se propaga el mensaje original: podría contener partes de la cadena de conexión.
            throw new InvalidOperationException($"{SpikeTargets.AzureVariable} no es una cadena de conexión válida.");
        }
    }

    private static async Task WaitUntilPingAsync(MongoClient client)
    {
        var admin = client.GetDatabase("admin");
        var deadline = DateTime.UtcNow.AddSeconds(90);
        while (true)
        {
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
                return;
            }
            catch (Exception ex) when (ex is MongoException or TimeoutException && DateTime.UtcNow < deadline)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}

/// <summary>Base efímera por prueba (<c>knock_spike_*</c>); se borra al terminar.</summary>
internal sealed class SpikeDatabase(MongoClient client, string name) : IAsyncDisposable
{
    public IMongoDatabase Database { get; } = client.GetDatabase(name);

    public MongoClient Client { get; } = client;

    public async ValueTask DisposeAsync() => await Client.DropDatabaseAsync(Database.DatabaseNamespace.DatabaseName);
}
