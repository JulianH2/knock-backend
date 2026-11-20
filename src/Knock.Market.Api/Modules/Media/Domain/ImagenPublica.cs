namespace Knock.Market.Api.Modules.Media.Domain;

/// <summary>Propósito de una imagen pública (spec 032, R-04): las fotos de una solicitud son privadas y no entran aquí.</summary>
internal enum PropositoImagen { Perfil, Trabajo }

internal static class PropositoImagenExtensions
{
    public static PropositoImagen? Parse(string? value) => value switch
    {
        "perfil" => PropositoImagen.Perfil,
        "trabajo" => PropositoImagen.Trabajo,
        _ => null,
    };

    public static string ToContractValue(this PropositoImagen proposito) => proposito switch
    {
        PropositoImagen.Perfil => "perfil",
        _ => "trabajo",
    };
}

/// <summary>Imagen pública ya interpretada: su propósito y la huella SHA-256 (64 hexadecimales en minúsculas) de los bytes recodificados.</summary>
internal sealed record ImagenPublica(PropositoImagen Proposito, string Huella, Uri Url);
