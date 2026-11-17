using System.Text.Json;
using Knock.Market.Api.Modules.Catalog.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using StackExchange.Redis;

namespace Knock.Market.Api.Modules.Catalog.Data;

/// <summary>
/// Caché del catálogo (spec 010, R-01, R-02 y R-04): existe para probar <see cref="CatalogStore"/> con dobles sin Redis.
/// Solo guarda lo que la política declara cacheable (oficios, nombres por CP y existencia por <c>_id</c> y CP); la colonia por
/// punto nunca entra, porque la clave llevaría coordenadas.
/// </summary>
internal interface ICatalogCache
{
    Task<IReadOnlyList<OficioListing>?> GetOficiosAsync(CancellationToken cancellationToken);

    /// <summary>Borra la clave. Si falla, el error sube: una escritura no puede responder dejando datos viejos en la caché.</summary>
    Task RemoveOficiosAsync(CancellationToken cancellationToken);

    /// <summary>Generación de la clave de oficios: 0 si aún nadie la invalidó; -1 si Redis no responde. Las lecturas la capturan antes de ir a MongoDB.</summary>
    Task<long> GetOficiosGenerationAsync(CancellationToken cancellationToken);

    /// <summary>Guarda solo si la generación sigue siendo la capturada (la escritura subió la generación al invalidar). Devuelve false si cambió o Redis no responde.</summary>
    Task<bool> SetOficiosIfGenerationAsync(IReadOnlyList<OficioListing> oficios, long expectedGeneration, CancellationToken cancellationToken);

    Task<IReadOnlyList<Colonia>?> GetNeighborhoodNamesAsync(string postalCode, CancellationToken cancellationToken);

    /// <summary>Borra la clave del CP. Si falla, el error sube: una escritura no puede responder dejando datos viejos en la caché.</summary>
    Task RemoveNeighborhoodNamesAsync(string postalCode, CancellationToken cancellationToken);

    /// <summary>Generación de la clave de nombres del CP; mismas reglas que la de oficios.</summary>
    Task<long> GetNeighborhoodNamesGenerationAsync(string postalCode, CancellationToken cancellationToken);

    /// <summary>Guarda solo si la generación sigue siendo la capturada; mismas reglas que la de oficios.</summary>
    Task<bool> SetNeighborhoodNamesIfGenerationAsync(string postalCode, IReadOnlyList<Colonia> names, long expectedGeneration, CancellationToken cancellationToken);

    Task<bool?> GetColoniaExistsAsync(ObjectId id, string postalCode, CancellationToken cancellationToken);

    /// <summary>Borra la clave del par <c>_id</c> y CP. Si falla, el error sube: una escritura no puede responder dejando datos viejos en la caché.</summary>
    Task RemoveColoniaExistsAsync(ObjectId id, string postalCode, CancellationToken cancellationToken);

    /// <summary>Generación de la clave de existencia del par; mismas reglas que la de oficios.</summary>
    Task<long> GetColoniaExistsGenerationAsync(ObjectId id, string postalCode, CancellationToken cancellationToken);

    /// <summary>Guarda solo si la generación sigue siendo la capturada; mismas reglas que la de oficios.</summary>
    Task<bool> SetColoniaExistsIfGenerationAsync(ObjectId id, string postalCode, bool exists, long expectedGeneration, CancellationToken cancellationToken);
}

/// <summary>
/// Claves <c>catalog:oficios</c>, <c>catalog:colonias:cp:{cp}</c> y <c>catalog:colonias:existe:{id}:{cp}</c> en Redis con la vida de
/// <see cref="CatalogCacheOptions"/> (spec 010, R-04: vigencia inicial de los catálogos, a ajustar con pruebas de carga). Leer y guardar
/// fallan en silencio (se va a MongoDB); borrar no.
/// </summary>
internal sealed partial class RedisCatalogCache(IConnectionMultiplexer redis, IOptions<CatalogCacheOptions> cacheOptions, ILogger<RedisCatalogCache> logger) : ICatalogCache
{
    public const string OficiosKey = "catalog:oficios";
    public const string NeighborhoodNamesKeyPrefix = "catalog:colonias:cp:";
    public const string ColoniaExistsKeyPrefix = "catalog:colonias:existe:";

    // Generaciones por clave (KNOCK-205, carrera lectura/escritura): la escritura sube la generación y borra la clave; la lectura solo
    // guarda si la generación no cambió desde que empezó a leer en MongoDB. Así un lector lento no resucita datos viejos tras la invalidación.
    public const string OficiosGenerationKey = "catalog:gen:oficios";
    public const string NeighborhoodNamesGenerationKeyPrefix = "catalog:gen:colonias:cp:";
    public const string ColoniaExistsGenerationKeyPrefix = "catalog:gen:colonias:existe:";

    public static string NeighborhoodNamesGenerationKey(string postalCode) => NeighborhoodNamesGenerationKeyPrefix + postalCode;

    public static string ColoniaExistsGenerationKey(ObjectId id, string postalCode) => ColoniaExistsGenerationKeyPrefix + id + ":" + postalCode;

    private readonly TimeSpan _lifetime = cacheOptions.Value.Lifetime;

    // Las claves de generación no llevan TTL a propósito: expirar una y reiniciar su contador a 0 podría hacer coincidir la generación
    // de un lector lento con la de una clave nueva y resucitar datos obsoletos. Su crecimiento está acotado por el número de CP y
    // pares _id/CP distintos que se consultan, no por el número de lecturas.

    // Incrementa la generación y borra la clave en un solo script atómico: no queda ventana entre el INCR y el DEL en la que un
    // lector con la generación nueva pueda publicar datos viejos, ni en la que se reciba una entrada antigua tras la invalidación.
    private const string _invalidateScript =
        """
        redis.call('INCR', KEYS[2])
        redis.call('DEL', KEYS[1])
        return 1
        """;

    // Compara la generación actual con la esperada invocada por el lector y solo entonces guarda; evita que la escritura entre medias quede tapada.
    private const string _setIfGenerationScript =
        """
        local generation = redis.call('GET', KEYS[2])
        if generation == false then generation = '0' end
        if generation == ARGV[2] then return redis.call('SET', KEYS[1], ARGV[1], 'EX', ARGV[3]) else return 0 end
        """;

    public static string NeighborhoodNamesKey(string postalCode) => NeighborhoodNamesKeyPrefix + postalCode;

    public static string ColoniaExistsKey(ObjectId id, string postalCode) => ColoniaExistsKeyPrefix + id + ":" + postalCode;

    public async Task<IReadOnlyList<OficioListing>?> GetOficiosAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var cached = await redis.GetDatabase().StringGetAsync(OficiosKey);
            return cached.IsNullOrEmpty ? null : JsonSerializer.Deserialize<List<OficioListing>>(cached.ToString());
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException or JsonException)
        {
            LogCacheUnavailable(logger);
            return null;
        }
    }

    public async Task RemoveOficiosAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await InvalidateAsync(OficiosKey, OficiosGenerationKey, cancellationToken);
    }

    public Task<long> GetOficiosGenerationAsync(CancellationToken cancellationToken) =>
        GetGenerationAsync(OficiosGenerationKey, cancellationToken);

    public async Task<bool> SetOficiosIfGenerationAsync(IReadOnlyList<OficioListing> oficios, long expectedGeneration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(oficios);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var result = await redis.GetDatabase().ScriptEvaluateAsync(
                _setIfGenerationScript,
                [OficiosKey, OficiosGenerationKey],
                [JsonSerializer.Serialize(oficios), expectedGeneration.ToString(System.Globalization.CultureInfo.InvariantCulture), ((long)_lifetime.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture)]);
            return result.ToString() == "OK";
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException)
        {
            LogCacheUnavailable(logger);
            return false;
        }
    }

    private async Task<long> GetGenerationAsync(string generationKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var value = await redis.GetDatabase().StringGetAsync(generationKey);
            return value.IsNullOrEmpty
                ? 0
                : long.Parse(value.ToString(), System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException or FormatException)
        {
            LogCacheUnavailable(logger);
            return -1;
        }
    }

    public async Task<IReadOnlyList<Colonia>?> GetNeighborhoodNamesAsync(string postalCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var cached = await redis.GetDatabase().StringGetAsync(NeighborhoodNamesKey(postalCode));
            return cached.IsNullOrEmpty ? null : JsonSerializer.Deserialize<List<Colonia>>(cached.ToString());
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException or JsonException)
        {
            LogCacheUnavailable(logger);
            return null;
        }
    }

    public async Task RemoveNeighborhoodNamesAsync(string postalCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);
        cancellationToken.ThrowIfCancellationRequested();
        await RemoveWithGenerationBumpAsync(NeighborhoodNamesKey(postalCode), NeighborhoodNamesGenerationKey(postalCode));
    }

    public Task<long> GetNeighborhoodNamesGenerationAsync(string postalCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);
        return GetGenerationAsync(NeighborhoodNamesGenerationKey(postalCode), cancellationToken);
    }

    public async Task<bool> SetNeighborhoodNamesIfGenerationAsync(string postalCode, IReadOnlyList<Colonia> names, long expectedGeneration, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);
        ArgumentNullException.ThrowIfNull(names);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var result = await redis.GetDatabase().ScriptEvaluateAsync(
                _setIfGenerationScript,
                [NeighborhoodNamesKey(postalCode), NeighborhoodNamesGenerationKey(postalCode)],
                [JsonSerializer.Serialize(names), expectedGeneration.ToString(System.Globalization.CultureInfo.InvariantCulture), (names.Count == 0 ? 300L : (long)_lifetime.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture)]);
            return result.ToString() == "OK";
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException)
        {
            LogCacheUnavailable(logger);
            return false;
        }
    }

    public async Task<bool?> GetColoniaExistsAsync(ObjectId id, string postalCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var cached = await redis.GetDatabase().StringGetAsync(ColoniaExistsKey(id, postalCode));
            return cached.IsNullOrEmpty ? null : JsonSerializer.Deserialize<bool>(cached.ToString());
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException or JsonException)
        {
            LogCacheUnavailable(logger);
            return null;
        }
    }

    public async Task RemoveColoniaExistsAsync(ObjectId id, string postalCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);
        cancellationToken.ThrowIfCancellationRequested();
        await RemoveWithGenerationBumpAsync(ColoniaExistsKey(id, postalCode), ColoniaExistsGenerationKey(id, postalCode));
    }

    public Task<long> GetColoniaExistsGenerationAsync(ObjectId id, string postalCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);
        return GetGenerationAsync(ColoniaExistsGenerationKey(id, postalCode), cancellationToken);
    }

    public async Task<bool> SetColoniaExistsIfGenerationAsync(ObjectId id, string postalCode, bool exists, long expectedGeneration, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var result = await redis.GetDatabase().ScriptEvaluateAsync(
                _setIfGenerationScript,
                [ColoniaExistsKey(id, postalCode), ColoniaExistsGenerationKey(id, postalCode)],
                [JsonSerializer.Serialize(exists), expectedGeneration.ToString(System.Globalization.CultureInfo.InvariantCulture), ((long)_lifetime.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture)]);
            return result.ToString() == "OK";
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException)
        {
            LogCacheUnavailable(logger);
            return false;
        }
    }

    private async Task RemoveWithGenerationBumpAsync(string key, string generationKey) =>
        await InvalidateAsync(key, generationKey, default);

    private async Task InvalidateAsync(string key, string generationKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await redis.GetDatabase().ScriptEvaluateAsync(_invalidateScript, [key, generationKey]);
    }

    [LoggerMessage(EventId = 2204, Level = LogLevel.Warning, Message = "Catalog cache unavailable: reading from the database")]
    private static partial void LogCacheUnavailable(ILogger logger);
}

/// <summary>
/// Caché deshabilitada: se usa cuando no hay cadena de Redis configurada (<see cref="CatalogModuleExtensions"/>). Lee siempre de
/// MongoDB, igual que cuando Redis no responde (las lecturas de <see cref="RedisCatalogCache"/> fallan abiertas y las generaciones
/// quedan en -1, así que nada se guarda); invalidar no hace nada porque no hay nada que invalidar.
/// </summary>
internal sealed class DisabledCatalogCache : ICatalogCache
{
    public Task<IReadOnlyList<OficioListing>?> GetOficiosAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OficioListing>?>(null);

    public Task RemoveOficiosAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<long> GetOficiosGenerationAsync(CancellationToken cancellationToken) => Task.FromResult(-1L);

    public Task<bool> SetOficiosIfGenerationAsync(IReadOnlyList<OficioListing> oficios, long expectedGeneration, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public Task<IReadOnlyList<Colonia>?> GetNeighborhoodNamesAsync(string postalCode, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Colonia>?>(null);

    public Task RemoveNeighborhoodNamesAsync(string postalCode, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<long> GetNeighborhoodNamesGenerationAsync(string postalCode, CancellationToken cancellationToken) => Task.FromResult(-1L);

    public Task<bool> SetNeighborhoodNamesIfGenerationAsync(string postalCode, IReadOnlyList<Colonia> names, long expectedGeneration, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public Task<bool?> GetColoniaExistsAsync(ObjectId id, string postalCode, CancellationToken cancellationToken) =>
        Task.FromResult<bool?>(null);

    public Task RemoveColoniaExistsAsync(ObjectId id, string postalCode, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<long> GetColoniaExistsGenerationAsync(ObjectId id, string postalCode, CancellationToken cancellationToken) => Task.FromResult(-1L);

    public Task<bool> SetColoniaExistsIfGenerationAsync(ObjectId id, string postalCode, bool exists, long expectedGeneration, CancellationToken cancellationToken) =>
        Task.FromResult(false);
}
