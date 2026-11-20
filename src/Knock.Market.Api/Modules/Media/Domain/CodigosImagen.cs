namespace Knock.Market.Api.Modules.Media.Domain;

/// <summary>Valores de <c>code</c> de ProblemDetails de las imágenes públicas (spec 032, §5), en inglés y en <c>snake_case</c>.</summary>
internal static class CodigosImagen
{
    /// <summary>Los bytes no son JPEG, PNG ni WebP, sin importar el tipo declarado (R-07).</summary>
    public const string FormatNotSupportedCode = "format_not_supported";

    /// <summary>La imagen no se decodifica, excede los límites contra imágenes bomba o tarda más de lo permitido (R-08).</summary>
    public const string ImageInvalidCode = "image_invalid";
}
