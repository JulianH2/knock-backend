using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Api.Hosting;

/// <summary>
/// Conexión a MongoDB del proceso (sección <c>Mongo</c> de appsettings, sin secretos). La cadena llega por
/// <c>ConnectionStrings:KnockMongo</c> desde variables de entorno o user-secrets; en Azure, Key Vault (TT-SEC-01).
/// </summary>
internal sealed class KnockMongoOptions
{
    public const string ConnectionStringName = "KnockMongo";
    public const string ConnectionStringKey = "ConnectionStrings:" + ConnectionStringName;
    public const string MaxPoolSizeKey = "Mongo:MaxPoolSize";
    public const string DatabasesSection = "Mongo:Databases";
    public const string ServerSelectionTimeoutKey = "Mongo:ServerSelectionTimeoutSeconds";
    public const string ConnectTimeoutKey = "Mongo:ConnectTimeoutSeconds";
    public const string SocketTimeoutKey = "Mongo:SocketTimeoutSeconds";
    public const string QueryMaxTimeKey = "Mongo:QueryMaxTimeSeconds";
    public const int MaxPoolSizeLimit = 100;
    public const int TimeoutLimitSeconds = 60;
    public const int DefaultServerSelectionTimeoutSeconds = 5;
    public const int DefaultConnectTimeoutSeconds = 5;
    public const int DefaultSocketTimeoutSeconds = 10;
    public const int DefaultQueryMaxTimeSeconds = 5;

    public string? ConnectionString { get; set; }

    /// <summary>Conexiones del pool compartido por todos los módulos; <c>null</c> si falta o no es un entero.</summary>
    public int? MaxPoolSize { get; set; }

    /// <summary>
    /// Tiempos del cliente en segundos (decisión del líder, 2026-10-28): sin ellos, el driver espera 30 s a elegir servidor y
    /// no corta sockets ni consultas. Si la clave falta se usa el valor por defecto; <c>null</c> si está pero no es un entero.
    /// </summary>
    public int? ServerSelectionTimeoutSeconds { get; set; } = DefaultServerSelectionTimeoutSeconds;

    public int? ConnectTimeoutSeconds { get; set; } = DefaultConnectTimeoutSeconds;

    public int? SocketTimeoutSeconds { get; set; } = DefaultSocketTimeoutSeconds;

    /// <summary><c>maxTimeMS</c> de cada consulta de los módulos: el servidor la corta aunque el socket siga vivo.</summary>
    public int? QueryMaxTimeSeconds { get; set; } = DefaultQueryMaxTimeSeconds;

    public TimeSpan QueryMaxTime => Seconds(QueryMaxTimeSeconds, QueryMaxTimeKey);

    internal static TimeSpan Seconds(int? value, string key) =>
        TimeSpan.FromSeconds(value ?? throw new InvalidOperationException($"{key} is not configured."));

    /// <summary>Base de cada módulo (<c>Mongo:Databases:&lt;Módulo&gt;</c>).</summary>
    public Dictionary<string, string?> Databases { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Módulos que pidieron su base con <see cref="MongoExtensions.AddKnockMongoDatabase"/>.</summary>
    public HashSet<string> Modules { get; } = new(StringComparer.Ordinal);
}

/// <summary>Valida la conexión al arrancar. Los mensajes nombran la clave, nunca el valor (la cadena trae usuario y contraseña).</summary>
internal sealed partial class KnockMongoOptionsValidator : IValidateOptions<KnockMongoOptions>
{
    [GeneratedRegex("^[A-Za-z0-9_-]{1,63}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex DatabaseName();

    public ValidateOptionsResult Validate(string? name, KnockMongoOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            failures.Add($"{KnockMongoOptions.ConnectionStringKey} is not configured.");
        }
        else if (!IsValidConnectionString(options.ConnectionString))
        {
            failures.Add($"{KnockMongoOptions.ConnectionStringKey} is not a valid MongoDB connection string.");
        }

        if (options.MaxPoolSize is not (>= 1 and <= KnockMongoOptions.MaxPoolSizeLimit))
        {
            failures.Add($"{KnockMongoOptions.MaxPoolSizeKey} must be an integer from 1 to {KnockMongoOptions.MaxPoolSizeLimit}.");
        }

        ValidateTimeout(failures, options.ServerSelectionTimeoutSeconds, KnockMongoOptions.ServerSelectionTimeoutKey);
        ValidateTimeout(failures, options.ConnectTimeoutSeconds, KnockMongoOptions.ConnectTimeoutKey);
        ValidateTimeout(failures, options.SocketTimeoutSeconds, KnockMongoOptions.SocketTimeoutKey);
        ValidateTimeout(failures, options.QueryMaxTimeSeconds, KnockMongoOptions.QueryMaxTimeKey);

        foreach (var module in options.Modules.Order(StringComparer.Ordinal))
        {
            if (!options.Databases.TryGetValue(module, out var database) || database is null || !DatabaseName().IsMatch(database))
            {
                failures.Add($"{KnockMongoOptions.DatabasesSection}:{module} must name a database (letters, digits, '_' or '-').");
            }
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateTimeout(List<string> failures, int? seconds, string key)
    {
        if (seconds is not (>= 1 and <= KnockMongoOptions.TimeoutLimitSeconds))
        {
            failures.Add($"{key} must be an integer from 1 to {KnockMongoOptions.TimeoutLimitSeconds} (seconds).");
        }
    }

    private static bool IsValidConnectionString(string connectionString)
    {
        try
        {
            _ = MongoUrl.Create(connectionString);
            return true;
        }
        catch (Exception ex) when (ex is MongoConfigurationException or ArgumentException or FormatException)
        {
            // Sin el mensaje: el del driver repite la cadena.
            return false;
        }
    }
}

internal static class MongoExtensions
{
    public const string HealthCheckName = "mongodb";

    /// <summary>Tiempo máximo del ping de readiness; sin él, la selección de servidor del driver espera 30 s.</summary>
    public static readonly TimeSpan PingTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Un solo <see cref="IMongoClient"/> por proceso (un pool de <c>Mongo:MaxPoolSize</c> conexiones para todos los módulos),
    /// validado al arrancar, y una comprobación de readiness. Llamarlo más de una vez no registra nada nuevo.
    /// </summary>
    public static IServiceCollection AddKnockMongo(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (services.Any(descriptor => descriptor.ServiceType == typeof(IMongoClient)))
        {
            return services;
        }

        services.AddOptions<KnockMongoOptions>()
            .Configure<IConfiguration>(Bind)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<KnockMongoOptions>, KnockMongoOptionsValidator>();
        services.AddSingleton<IMongoClient>(provider =>
            new MongoClient(CreateSettings(provider.GetRequiredService<IOptions<KnockMongoOptions>>().Value)));
        services.AddHealthChecks().AddCheck<MongoReadinessHealthCheck>(HealthCheckName, tags: [HealthExtensions.ReadyTag]);
        return services;
    }

    /// <summary>
    /// Base del módulo por nombre (<c>Mongo:Databases:&lt;module&gt;</c>), del cliente compartido. Se resuelve con
    /// <c>[FromKeyedServices("&lt;module&gt;")] IMongoDatabase</c>. Sin la clave, la API no arranca.
    /// </summary>
    public static IServiceCollection AddKnockMongoDatabase(this IServiceCollection services, string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        services.AddKnockMongo();
        services.Configure<KnockMongoOptions>(options => options.Modules.Add(module));
        services.AddKeyedSingleton<IMongoDatabase>(module, (provider, _) =>
        {
            var options = provider.GetRequiredService<IOptions<KnockMongoOptions>>().Value;
            return provider.GetRequiredService<IMongoClient>().GetDatabase(options.Databases[module]);
        });
        return services;
    }

    /// <summary>Ajustes del cliente: la cadena da el destino y las credenciales; el tamaño del pool y los tiempos los fija la configuración.</summary>
    internal static MongoClientSettings CreateSettings(KnockMongoOptions options)
    {
        var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);
        settings.MaxConnectionPoolSize = options.MaxPoolSize
            ?? throw new InvalidOperationException($"{KnockMongoOptions.MaxPoolSizeKey} is not configured.");
        settings.ServerSelectionTimeout = KnockMongoOptions.Seconds(options.ServerSelectionTimeoutSeconds, KnockMongoOptions.ServerSelectionTimeoutKey);
        settings.ConnectTimeout = KnockMongoOptions.Seconds(options.ConnectTimeoutSeconds, KnockMongoOptions.ConnectTimeoutKey);
        settings.SocketTimeout = KnockMongoOptions.Seconds(options.SocketTimeoutSeconds, KnockMongoOptions.SocketTimeoutKey);
        return settings;
    }

    private static void Bind(KnockMongoOptions options, IConfiguration configuration)
    {
        options.ConnectionString = configuration.GetConnectionString(KnockMongoOptions.ConnectionStringName);
        options.MaxPoolSize = ParseInteger(configuration[KnockMongoOptions.MaxPoolSizeKey]);
        options.ServerSelectionTimeoutSeconds = ParseSeconds(
            configuration, KnockMongoOptions.ServerSelectionTimeoutKey, KnockMongoOptions.DefaultServerSelectionTimeoutSeconds);
        options.ConnectTimeoutSeconds = ParseSeconds(
            configuration, KnockMongoOptions.ConnectTimeoutKey, KnockMongoOptions.DefaultConnectTimeoutSeconds);
        options.SocketTimeoutSeconds = ParseSeconds(
            configuration, KnockMongoOptions.SocketTimeoutKey, KnockMongoOptions.DefaultSocketTimeoutSeconds);
        options.QueryMaxTimeSeconds = ParseSeconds(
            configuration, KnockMongoOptions.QueryMaxTimeKey, KnockMongoOptions.DefaultQueryMaxTimeSeconds);
        foreach (var database in configuration.GetSection(KnockMongoOptions.DatabasesSection).GetChildren())
        {
            options.Databases[database.Key] = database.Value;
        }
    }

    private static int? ParseInteger(string? value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : null;

    /// <summary>Clave ausente: el valor por defecto. Presente pero vacía o no entera: <c>null</c>, y el validador la rechaza.</summary>
    private static int? ParseSeconds(IConfiguration configuration, string key, int defaultSeconds)
    {
        var value = configuration[key];
        return value is null ? defaultSeconds : ParseInteger(value);
    }
}

/// <summary>Ping a MongoDB con la etiqueta <c>ready</c>; una sola por proceso, aunque varios módulos tengan base.</summary>
internal sealed class MongoReadinessHealthCheck(IMongoClient client) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(MongoExtensions.PingTimeout);
        try
        {
            await client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: timeout.Token);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // Cualquier error, no solo los del driver: si la excepción saliera, HealthCheckService registraría su mensaje,
            // que puede traer el host o el usuario de la cadena. Solo el tipo, sin la excepción (TC-TT-BD-06-14).
            // La cancelación que pide quien llama sí se propaga.
            return HealthCheckResult.Unhealthy($"MongoDB did not respond ({ex.GetType().Name}).");
        }
    }
}
