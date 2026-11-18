namespace Knock.Market.Api.Modules.Requests.Dtos;

/// <summary>Cuerpo de <c>POST /solicitudes</c> (spec 020, R-01). El 422 lo da <c>CreateRequestBodyValidator</c>; nunca repite el valor recibido.</summary>
/// <param name="OficioClave">Clave de un oficio activo del catálogo. La modalidad (domicilio o taller) la dicta el oficio.</param>
/// <param name="Descripcion">De 8 a 500 caracteres; pasa por moderación antes de guardarse.</param>
/// <param name="Fotos">De 1 a 5 identificadores de archivo (24 hexadecimales), sin repetidos, de archivos ya confirmados del mismo usuario.</param>
/// <param name="Dia">Fecha <c>YYYY-MM-DD</c> en la zona America/Mexico_City, de hoy a hoy + 5 días.</param>
/// <param name="Franja">Una de <c>manana</c>, <c>tarde</c> o <c>noche</c>.</param>
/// <param name="ColoniaId">Identificador (24 hexadecimales) de una colonia del catálogo.</param>
/// <param name="Direccion">De 8 a 200 caracteres; obligatoria en modalidad domicilio y no admitida en taller. Se guarda cifrada.</param>
/// <param name="ElementoId">Opcional (spec 041, R-19, ampliación aditiva de la spec 020): identificador (24 hexadecimales) de un elemento de «Mi casa y mi auto» al que se liga el trabajo. Solo se valida su formato; nunca se devuelve.</param>
/// <remarks>El cuerpo completo no puede pasar de <see cref="MaxBodyBytes"/> (R-21); uno mayor responde 413 antes de llegar al controller.</remarks>
internal sealed record CreateRequestBody(
    string? OficioClave,
    string? Descripcion,
    IReadOnlyList<string>? Fotos,
    string? Dia,
    string? Franja,
    string? ColoniaId,
    string? Direccion,
    string? ElementoId = null)
{
    /// <summary>Tope del cuerpo JSON de las peticiones de esta spec: 8 KB (R-21).</summary>
    public const int MaxBodyBytes = 8 * 1024;
}
