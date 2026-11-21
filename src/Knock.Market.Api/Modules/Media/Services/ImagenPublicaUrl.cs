using Knock.Market.Api.Modules.Media.Domain;
using Knock.Market.Api.Modules.Media.Options;
using Microsoft.Extensions.Options;

namespace Knock.Market.Api.Modules.Media.Services;

/// <summary>
/// Formato y validación de la dirección pública de una imagen (spec 032, R-13):
/// <c>{BaseUrlPublica}/{proposito}/{64 hexadecimales en minúsculas}.jpg</c>, https, sin consulta, fragmento, puerto ni credenciales.
/// Es una instancia (y no estática) porque la regla depende de <c>Imagenes:BaseUrlPublica</c>.
/// </summary>
internal sealed class ImagenPublicaUrl
{
    private const int _longitudHuella = 64;
    private const string _extension = ".jpg";

    private readonly Uri _baseUri;

    public ImagenPublicaUrl(IOptions<ImagenesOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!ImagenesOptionsValidator.TryParseBase(options.Value.BaseUrlPublica, out var baseUri))
        {
            throw new InvalidOperationException("Imagenes:BaseUrlPublica is not valid.");
        }

        _baseUri = baseUri;
    }

    /// <summary>Arma la dirección de una imagen publicada.</summary>
    public ImagenPublica Build(PropositoImagen proposito, string huella)
    {
        ArgumentNullException.ThrowIfNull(huella);
        return IsHash(huella)
            ? new ImagenPublica(proposito, huella, new Uri($"{_baseUri.GetLeftPart(UriPartial.Path)}/{proposito.ToContractValue()}/{huella}{_extension}"))
            : throw new ArgumentException("The hash must be 64 lowercase hexadecimal characters.", nameof(huella));
    }

    /// <summary>Interpreta una dirección; cualquier desviación de R-13 la rechaza.</summary>
    public bool TryParse(string? value, out ImagenPublica? imagen)
    {
        imagen = null;
        var prefix = _baseUri.GetLeftPart(UriPartial.Path) + "/";
        if (string.IsNullOrEmpty(value)
            || value.Length > prefix.Length + 64 + 16
            || !value.StartsWith(prefix, StringComparison.Ordinal)
            || value.Contains('?', StringComparison.Ordinal)
            || value.Contains('#', StringComparison.Ordinal))
        {
            return false;
        }

        var parts = value[prefix.Length..].Split('/');
        if (parts.Length != 2 || PropositoImagenExtensions.Parse(parts[0]) is not { } proposito || !parts[1].EndsWith(_extension, StringComparison.Ordinal))
        {
            return false;
        }

        var huella = parts[1][..^_extension.Length];
        if (!IsHash(huella))
        {
            return false;
        }

        imagen = Build(proposito, huella);
        return true;
    }

    private static bool IsHash(string value) =>
        value.Length == _longitudHuella && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
