using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using Testcontainers.MongoDb;
using Xunit;
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

    /// <summary>Origen de la medición de latencia (P3): ciudad y red de quien ejecuta.</summary>
    public const string OriginVariable = "SPIKE_ORIGEN";

    /// <summary>
    /// P3 y P7 comparten colección de xUnit para no correr a la vez (la carga de P7 inflaría la latencia de P3).
    /// </summary>
    public const string MeasurementCollection = "Mediciones P3 y P7";

    public static bool AzureConfigured => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(AzureVariable));

    public static string Origin =>
        Environment.GetEnvironmentVariable(OriginVariable) is { Length: > 0 } origin ? origin : "Tula, Hgo. — red doméstica";

    /// <summary>Entero positivo de una variable de entorno, o el valor por defecto si no existe o no es válido.</summary>
    public static int PositiveIntFromEnvironment(string variable, int fallback) =>
        int.TryParse(Environment.GetEnvironmentVariable(variable), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
        && value > 0 ? value : fallback;

    /// <summary>Destinos que corren en esta ejecución: los locales siempre; Azure si está definido y responde.</summary>
    public static IReadOnlyList<string> Available =>
        AzureConfigured && AzureConnection.Failure is null ? [Mongo, DocumentDbLocal, Azure] : [Mongo, DocumentDbLocal];
}

/// <summary>
/// Cliente de Azure con un chequeo único al primer uso: ping con <c>serverSelectionTimeout</c> de 10 s.
/// Si no responde, las pruebas de Azure se omiten con el motivo y solo <c>AzureConnectivityTests</c> falla.
/// </summary>
internal static class AzureConnection
{
    public static readonly TimeSpan ServerSelectionTimeout = TimeSpan.FromSeconds(10);

    private static readonly Lazy<(MongoClient? Client, string? Failure)> _probe = new(Probe);

    /// <summary>Motivo del fallo de conexión, o <c>null</c> si el clúster respondió.</summary>
    public static string? Failure => _probe.Value.Failure;

    public static MongoClient Client => _probe.Value.Client ?? throw new InvalidOperationException(_probe.Value.Failure);

    private static (MongoClient? Client, string? Failure) Probe()
    {
        MongoClientSettings settings;
        try
        {
            settings = MongoClientSettings.FromConnectionString(Environment.GetEnvironmentVariable(SpikeTargets.AzureVariable));
        }
        catch (Exception ex) when (ex is MongoConfigurationException or ArgumentException)
        {
            // No se propaga el mensaje original: podría contener partes de la cadena de conexión.
            return (null, $"{SpikeTargets.AzureVariable} no es una cadena de conexión válida.");
        }

        settings.ServerSelectionTimeout = ServerSelectionTimeout;
        var client = new MongoClient(settings);
        try
        {
            client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            return (client, null);
        }
        catch (Exception ex) when (ex is TimeoutException or MongoException)
        {
            client.Dispose();
            return (null, $"Sin conexión de red al clúster: no respondió en {ServerSelectionTimeout.TotalSeconds:0} s ({ex.GetType().Name}). "
                + "Revisa la red y la regla de firewall de tu IP.");
        }
    }
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

/// <summary>
/// Azure DocumentDB; se omite (Skip) si <c>KNOCK_SPIKE_MONGO</c> no existe (spec §3) o si el clúster no respondió
/// al chequeo único de conexión.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class AzureTargetAttribute : DataAttribute
{
    public AzureTargetAttribute()
    {
        Skip = AzureSkipReason.Value;
    }

    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        yield return [SpikeTargets.Azure];
    }

    internal static Lazy<string?> AzureSkipReason { get; } = new(SkipReason);

    private static string? SkipReason()
    {
        if (!SpikeTargets.AzureConfigured)
        {
            return $"{SpikeTargets.AzureVariable} no definida: destino Azure omitido.";
        }

        return AzureConnection.Failure is { } failure ? $"{failure} Ver AzureConnectivityTests." : null;
    }
}

/// <summary>Prueba de Azure que solo se omite si la variable no existe (no si falla la conexión).</summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class AzureFactAttribute : FactAttribute
{
    public AzureFactAttribute()
    {
        Skip = SpikeTargets.AzureConfigured ? null : $"{SpikeTargets.AzureVariable} no definida: destino Azure omitido.";
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

    public static async Task<SpikeDatabase> CreateDatabaseAsync(string target) =>
        new(await ClientAsync(target), "knock_spike_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(4)));

    public static async Task<MongoClient> ClientAsync(string target) => target switch
    {
        SpikeTargets.Mongo => await _mongo.Value,
        SpikeTargets.DocumentDbLocal => await _documentDb.Value,
        SpikeTargets.Azure => AzureConnection.Client,
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Destino desconocido."),
    };

    /// <summary>
    /// Cliente nuevo con su propio pool (P7). El nombre de aplicación distinto evita que el driver comparta el clúster
    /// entre clientes con la misma configuración.
    /// </summary>
    public static async Task<MongoClient> CreateIndependentClientAsync(
        string target, string applicationName, Action<ClusterBuilder>? configure = null)
    {
        var settings = (await ClientAsync(target)).Settings.Clone();
        settings.ApplicationName = applicationName;
        if (configure is not null)
        {
            settings.ClusterConfigurator = configure;
        }

        return new MongoClient(settings);
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

    private bool _dropped;

    /// <summary>Borra la base al terminar; si ya se intentó con <see cref="TryDropAsync"/>, no hace nada.</summary>
    public async ValueTask DisposeAsync()
    {
        if (!_dropped)
        {
            await DropAsync();
        }
    }

    /// <summary>Intenta borrar la base y devuelve el tipo de error en vez de lanzarlo (P7, tras agotar conexiones).</summary>
    public async Task<string?> TryDropAsync()
    {
        _dropped = true;
        try
        {
            await DropAsync();
            return null;
        }
        catch (MongoException ex)
        {
            return ex.GetType().Name;
        }
    }

    /// <summary>
    /// Reintenta hasta 30 s: tras la rampa de P7 el servidor puede tardar en liberar conexiones
    /// (documentdb-local responde «remaining connection slots are reserved»).
    /// </summary>
    private async Task DropAsync()
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (true)
        {
            try
            {
                await Client.DropDatabaseAsync(Database.DatabaseNamespace.DatabaseName);
                return;
            }
            catch (MongoException) when (DateTime.UtcNow < deadline)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}
