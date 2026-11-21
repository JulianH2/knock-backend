using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Knock.Contracts.Conversations;
using StackExchange.Redis;

namespace Knock.Messaging.Api.Modules.Conversaciones.Data;

/// <summary>
/// Caché de 60 segundos del permiso de conversación (spec 030, R-09). Un fallo de Redis nunca sale como excepción: leer devuelve «no hay» y guardar no hace nada, así la caché solo puede
/// ahorrar una consulta a Mercado, nunca decidir un permiso por sí sola ni tirar la petición.
/// </summary>
internal interface IPermisoCache
{
    Task<PermisoConversacion?> GetAsync(string conversacionId, string subject, CancellationToken cancellationToken);

    Task SetAsync(string conversacionId, string subject, PermisoConversacion permiso, CancellationToken cancellationToken);
}

/// <summary>
/// Llave <c>conv:permiso:{conversacionId}:{huella}</c> en Redis (el único punto del módulo con acceso a Redis, constitución, principio 10). La spec nombra la llave por conversación, pero el
/// permiso trae el <c>rol</c> de quien pregunta (cliente o técnico), así que se separa por persona; la huella es un hash del <c>sub</c> para que el identificador no quede en claro en Redis.
/// El valor es el permiso (identificadores, nombres públicos, servicio y colonia): nunca texto de mensajes.
/// </summary>
internal sealed class PermisoRedisCache(IConnectionMultiplexer redis) : IPermisoCache
{
    public const string KeyPrefix = "conv:permiso:";
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(60);

    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public async Task<PermisoConversacion?> GetAsync(string conversacionId, string subject, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var value = await redis.GetDatabase().StringGetAsync(Key(conversacionId, subject));
            return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<PermisoConversacion>(value.ToString(), _json);
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException or JsonException)
        {
            return null;
        }
    }

    public async Task SetAsync(string conversacionId, string subject, PermisoConversacion permiso, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(permiso);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await redis.GetDatabase().StringSetAsync(Key(conversacionId, subject), JsonSerializer.Serialize(permiso, _json), Lifetime);
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException)
        {
            // Sin caché la siguiente consulta va a Mercado: no se pierde nada salvo la resistencia a una caída de Mercado.
        }
    }

    internal static string Key(string conversacionId, string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversacionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        var fingerprint = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(subject)))[..16];
        return $"{KeyPrefix}{conversacionId}:{fingerprint}";
    }
}
