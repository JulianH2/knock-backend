using Microsoft.Extensions.Options;

namespace Knock.Market.Api.Modules.Media.Options;

/// <summary>
/// Configuración de las imágenes públicas (<c>Imagenes</c>, spec 032 R-03). Sin valor por omisión en código: el despliegue
/// llena <c>Imagenes__BaseUrlPublica</c> y, si falta o está mal formada, Mercado no arranca.
/// </summary>
internal sealed class ImagenesOptions
{
    public const string SectionName = "Imagenes";

    /// <summary>Contenedor público; la dirección base termina en <c>/imagenes-publicas</c>.</summary>
    public const string ContenedorPublico = "imagenes-publicas";

    /// <summary>Dirección base de las imágenes públicas: <c>https://host/imagenes-publicas</c>.</summary>
    public string? BaseUrlPublica { get; set; }
}

/// <summary>Valida <c>Imagenes:*</c> al arrancar; cada mensaje nombra la llave y nunca repite su valor (spec 032, CA-24).</summary>
internal sealed class ImagenesOptionsValidator : IValidateOptions<ImagenesOptions>
{
    private const string _llave = "Imagenes:BaseUrlPublica";

    public ValidateOptionsResult Validate(string? name, ImagenesOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return TryParseBase(options.BaseUrlPublica, out _)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                $"{_llave} must be an absolute https URL with the host and the path /{ImagenesOptions.ContenedorPublico}, without query, fragment, port or credentials.");
    }

    /// <summary>Interpreta la dirección base con la regla de R-03; devuelve el URI sin barra final.</summary>
    internal static bool TryParseBase(string? value, out Uri baseUri)
    {
        baseUri = null!;
        if (string.IsNullOrWhiteSpace(value) || value != value.Trim() || !HasNoPortOrCredentials(value))
        {
            return false;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || string.IsNullOrEmpty(uri.Host)
            || uri.Query.Length > 0
            || uri.Fragment.Length > 0
            || value.Contains('?', StringComparison.Ordinal)
            || value.Contains('#', StringComparison.Ordinal)
            || uri.AbsolutePath != "/" + ImagenesOptions.ContenedorPublico)
        {
            return false;
        }

        baseUri = uri;
        return true;
    }

    /// <summary>El <see cref="Uri"/> normaliza <c>:443</c> y descarta nada; se revisa el texto de la autoridad tal cual llegó.</summary>
    private static bool HasNoPortOrCredentials(string value)
    {
        const string prefix = "https://";
        if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var rest = value[prefix.Length..];
        var end = rest.IndexOf('/', StringComparison.Ordinal);
        var authority = end < 0 ? rest : rest[..end];
        return authority.Length > 0 && !authority.Contains(':', StringComparison.Ordinal) && !authority.Contains('@', StringComparison.Ordinal);
    }
}
