using Microsoft.AspNetCore.Mvc;

namespace Knock.Market.Api.Modules.Tecnicos.Dtos;

/// <summary>Consulta de TEC-06 (spec 046, R-19): la colonia de la persona, 24 hexadecimales y del catálogo.</summary>
internal sealed class CoverageQuery
{
    [FromQuery(Name = "colonia")]
    public string? Colonia { get; set; }
}

/// <summary>Respuesta de TEC-06 (R-43).</summary>
internal sealed record CoverageResponse(bool Atiende);

/// <summary>Consulta de TEC-03 (spec 046, R-41): <c>q</c> de 2 a 40 letras, números o espacios.</summary>
internal sealed class AutocompleteQuery
{
    [FromQuery(Name = "q")]
    public string? Q { get; set; }
}

/// <summary>
/// Una sugerencia de TEC-03: <c>tipo</c> es <c>oficio</c> (con su <c>clave</c>) o <c>negocio</c> (con el <c>id</c> del técnico). Los servicios con precio
/// (spec 047) se suman cuando exista su colección.
/// </summary>
internal sealed record Suggestion(string Tipo, string Texto, string? Clave, string? Id);

/// <summary>Hasta 8 sugerencias.</summary>
internal sealed record AutocompleteResponse(IReadOnlyList<Suggestion> Sugerencias);
