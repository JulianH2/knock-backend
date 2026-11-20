using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Knock.Identity.Api.Modules.Panel.Services;

/// <summary>
/// Configuración de la sesión del panel (spec 033, R-13 y R-27): la llave de firma propia (<c>PanelJwt:SigningKey</c>, en Key Vault), el destinatario
/// (<c>PanelJwt:Audience</c>, <c>knock-panel</c>), el mismo emisor que la app (<c>Jwt:Issuer</c>) y el origen exacto del panel (<c>Cors:Panel:Origen</c>).
/// Sin llave configurada fuera de Development el panel queda <b>apagado</b> (<see cref="IsConfigured"/> falso): los endpoints responden 503 y ningún
/// token se acepta; así un despliegue sin las llaves nuevas no impide arrancar a la API de la app.
/// </summary>
internal sealed class PanelSettings
{
    public const string Section = "PanelJwt";
    public const string SigningKeyKey = Section + ":SigningKey";
    public const string AudienceKey = Section + ":Audience";
    public const string OriginKey = "Cors:Panel:Origen";
    public const int MinimumKeyBytes = 32;
    public const string DefaultAudience = "knock-panel";

    public PanelSettings(string issuer, string audience, SymmetricSecurityKey signingKey, string? origin, bool isConfigured)
    {
        Issuer = issuer;
        Audience = audience;
        SigningKey = signingKey;
        Origin = origin;
        IsConfigured = isConfigured;
    }

    public string Issuer { get; }

    public string Audience { get; }

    public SymmetricSecurityKey SigningKey { get; }

    /// <summary>Origen exacto del panel (esquema, host y puerto, sin barra final); <c>null</c> si no está configurado.</summary>
    public string? Origin { get; }

    /// <summary>Hay llave de firma válida (en Development siempre: se genera una al azar si falta).</summary>
    public bool IsConfigured { get; }

    /// <summary>El <c>Origin</c> de la petición es idéntico al configurado (comparación exacta, sin comodines).</summary>
    public bool OriginMatches(string? requestOrigin) =>
        Origin is not null && requestOrigin is not null && string.Equals(Origin, requestOrigin, StringComparison.Ordinal);

    public static PanelSettings Create(IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);
        var issuer = configuration["Jwt:Issuer"];
        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException("Jwt:Issuer is required.");
        }

        var audience = string.IsNullOrWhiteSpace(configuration[AudienceKey]) ? DefaultAudience : configuration[AudienceKey]!;
        var origin = NormalizeOrigin(configuration[OriginKey], environment);
        var configured = configuration[SigningKeyKey];
        if (string.IsNullOrWhiteSpace(configured))
        {
            // Development genera una llave al azar; fuera de Development sin llave el panel queda apagado (la llave es de Key Vault).
            return new PanelSettings(issuer, audience, new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(MinimumKeyBytes)), origin, environment.IsDevelopment());
        }

        var buffer = new byte[configured.Length];
        if (!Convert.TryFromBase64String(configured, buffer, out var written))
        {
            throw new InvalidOperationException($"{SigningKeyKey} must be Base64.");
        }

        if (written < MinimumKeyBytes)
        {
            throw new InvalidOperationException($"{SigningKeyKey} must decode to at least {MinimumKeyBytes} bytes.");
        }

        return new PanelSettings(issuer, audience, new SymmetricSecurityKey(buffer[..written]), origin, true);
    }

    /// <summary>Solo <c>https</c> (en Development también <c>http://localhost</c>), sin ruta, consulta, fragmento ni barra final. Un valor inválido deja el origen sin configurar.</summary>
    internal static string? NormalizeOrigin(string? value, IHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(value) || !Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var isLocal = environment.IsDevelopment() && uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback;
        var hasOnlyOrigin = uri.AbsolutePath == "/" && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment) && string.IsNullOrEmpty(uri.UserInfo);
        if (!hasOnlyOrigin || (uri.Scheme != Uri.UriSchemeHttps && !isLocal))
        {
            return null;
        }

        return uri.GetLeftPart(UriPartial.Authority);
    }
}
