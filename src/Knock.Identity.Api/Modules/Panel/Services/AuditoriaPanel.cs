using System.Security.Cryptography;
using System.Text;
using Knock.Identity.Api.Common.Errors;
using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Panel.Data;
using Knock.Identity.Api.Modules.Panel.Domain;
using Knock.Identity.Api.Modules.Sesiones.Services;

namespace Knock.Identity.Api.Modules.Panel.Services;

/// <summary>HMAC-SHA256 de la dirección IP con la llave de búsqueda de la spec 009 (la del celular): la auditoría nunca guarda la IP en claro (R-23).</summary>
internal sealed class PanelIpHasher(PhoneProtectionKeys keys)
{
    public string Hash(string ip)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ip);
        return Convert.ToHexStringLower(HMACSHA256.HashData(keys.CurrentHmacKey, Encoding.UTF8.GetBytes(ip)));
    }
}

/// <summary>
/// Auditoría del panel (spec 033, R-23 y R-24). Escribe un documento por evento en <c>panel_auditoria</c> con solo los campos permitidos y falla cerrada:
/// si no se puede escribir lanza 503 <c>audit_unavailable</c> y quien la llama no concede el acceso ni ejecuta la acción. Los datos de la petición
/// (IP como HMAC y <c>correlationId</c>) salen del contexto HTTP; la herramienta interna no tiene contexto y usa <c>cli</c>. Nunca recibe celular, códigos,
/// tokens, <c>idToken</c>, cuerpos ni datos de la persona sobre la que se actúa.
/// </summary>
internal sealed partial class AuditoriaPanel(
    IPanelAuditoriaStore store,
    PanelIpHasher ipHasher,
    IHttpContextAccessor accessor,
    TimeProvider timeProvider,
    ILogger<AuditoriaPanel> logger)
{
    public const string CliOrigin = "cli";
    public const int MaxResourceIdLength = 64;

    /// <summary>Escribe el evento. <paramref name="resourceId"/> se descarta si no parece un identificador (letras, dígitos, guion, guion bajo, punto o dos puntos).</summary>
    /// <exception cref="DomainException">503 <c>audit_unavailable</c> si la escritura falla.</exception>
    public async Task WriteAsync(
        string type,
        string eventName,
        string result,
        string? sub = null,
        string? sid = null,
        string? permission = null,
        string? route = null,
        string? resourceId = null,
        CancellationToken cancellationToken = default)
    {
        var (ipHmac, correlationId) = RequestOrigin();
        var entry = new PanelAuditEntry(
            timeProvider.GetUtcNow().UtcDateTime,
            type,
            eventName,
            result,
            ipHmac,
            correlationId,
            sub,
            sid,
            permission,
            route,
            IsSafeResourceId(resourceId) ? resourceId : null);
        try
        {
            await store.InsertAsync(entry, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogUnavailable(logger, eventName);
            throw new DomainException(DomainErrorKind.ServiceUnavailable, "The panel audit cannot be written.", PanelErrorCodes.AuditUnavailableCode);
        }
    }

    /// <summary>Un identificador de recurso es corto y de caracteres seguros: así no puede colarse un cuerpo ni un dato de la persona.</summary>
    internal static bool IsSafeResourceId(string? value) =>
        value is { Length: > 0 and <= MaxResourceIdLength } && value.All(c => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '-' or '_' or '.' or ':');

    private (string IpHmac, string CorrelationId) RequestOrigin()
    {
        if (accessor.HttpContext is not { } context)
        {
            return (CliOrigin, Guid.NewGuid().ToString("N"));
        }

        return (ipHasher.Hash(RateLimitingExtensions.ClientKey(context)), CorrelationId.Current(context) ?? context.TraceIdentifier);
    }

    // El registro técnico lleva solo el nombre del evento: ni sub, sid, IP ni datos de la petición.
    [LoggerMessage(EventId = 3301, Level = LogLevel.Error, Message = "Panel audit write failed for event {EventName}")]
    private static partial void LogUnavailable(ILogger logger, string eventName);
}
