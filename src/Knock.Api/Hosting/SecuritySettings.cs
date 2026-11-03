using Microsoft.Extensions.Options;

namespace Knock.Api.Hosting;

/// <summary>HSTS (<c>Security:Hsts</c>): <c>max-age</c> e <c>includeSubDomains</c>. Nunca <c>preload</c>.</summary>
internal sealed class HstsSettings
{
    public TimeSpan MaxAge { get; set; }

    public bool IncludeSubDomains { get; set; }
}

/// <summary>Redirección a HTTPS (<c>Security:Https</c>): sin <c>RedirectPort</c> no se redirige nunca (el caso de Container Apps).</summary>
internal sealed class HttpsSettings
{
    public int? RedirectPort { get; set; }
}

/// <summary>CORS (<c>Security:Cors</c>): lista de orígenes explícitos; vacía significa CORS apagado.</summary>
internal sealed class CorsSettings
{
    public string[] AllowedOrigins { get; set; } = [];

    public TimeSpan PreflightMaxAge { get; set; }
}

/// <summary>Valores del pipeline de seguridad; sin secretos. No hay valores por defecto en código: una configuración inválida impide el arranque.</summary>
internal sealed class SecuritySettings
{
    public const string SectionName = "Security";

    public HstsSettings Hsts { get; set; } = new();

    public HttpsSettings Https { get; set; } = new();

    public CorsSettings Cors { get; set; } = new();
}

/// <summary>Valida <c>Security:*</c> de forma estricta; cada mensaje nombra la clave inválida y nunca repite el valor.</summary>
internal sealed class SecuritySettingsValidator : IValidateOptions<SecuritySettings>
{
    private const int _maxPort = 65535;

    private static readonly string[] _loopbackHosts = ["localhost", "127.0.0.1", "[::1]"];

    public ValidateOptionsResult Validate(string? name, SecuritySettings options) =>
        Errors(options) is { Count: > 0 } errors ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;

    public static List<string> Errors(SecuritySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var errors = new List<string>();
        if (settings.Hsts.MaxAge <= TimeSpan.Zero)
        {
            errors.Add("Security:Hsts:MaxAge must be greater than zero.");
        }

        if (settings.Https.RedirectPort is { } port && port is < 1 or > _maxPort)
        {
            errors.Add("Security:Https:RedirectPort must be between 1 and 65535.");
        }

        for (var i = 0; i < settings.Cors.AllowedOrigins.Length; i++)
        {
            if (!IsValidOrigin(settings.Cors.AllowedOrigins[i]))
            {
                errors.Add($"Security:Cors:AllowedOrigins:{i} must be an exact origin (https://host[:port], or http only for loopback), without wildcard, path, query, fragment, credentials or trailing slash.");
            }
        }

        if (settings.Cors.AllowedOrigins.Length > 0 && settings.Cors.PreflightMaxAge <= TimeSpan.Zero)
        {
            errors.Add("Security:Cors:PreflightMaxAge must be greater than zero when Security:Cors:AllowedOrigins is not empty.");
        }

        return errors;
    }

    /// <summary>
    /// Origen exacto como lo envía un navegador: esquema y host en minúsculas, puerto solo si no es el del esquema, sin ruta, consulta,
    /// fragmento ni credenciales. <c>http</c> solo para hosts loopback. La comparación se hace contra la forma canónica del <see cref="Uri"/>.
    /// </summary>
    public static bool IsValidOrigin(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains('*', StringComparison.Ordinal) || !Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var secure = uri.Scheme == Uri.UriSchemeHttps;
        var loopbackHttp = uri.Scheme == Uri.UriSchemeHttp && _loopbackHosts.Contains(uri.Host, StringComparer.Ordinal);
        return (secure || loopbackHttp)
            && uri.UserInfo.Length == 0
            && string.Equals(value, uri.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal);
    }
}
