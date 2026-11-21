using Knock.Market.Api.Modules.Tecnicos.Services;
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

/// <summary>Consulta de TEC-02 (spec 046, R-04 y R-05): filtros estructurados, orden y cursor por página.</summary>
internal sealed class TechnicianListQuery
{
    public const int DefaultLimit = 20;

    public const int MaxLimit = 50;

    [FromQuery(Name = "oficio")] public string? Oficio { get; set; }

    [FromQuery(Name = "colonia")] public string? Colonia { get; set; }

    [FromQuery(Name = "lat")] public string? Lat { get; set; }

    [FromQuery(Name = "lng")] public string? Lng { get; set; }

    [FromQuery(Name = "radioKm")] public string? RadioKm { get; set; }

    [FromQuery(Name = "disponibleHoy")] public string? DisponibleHoy { get; set; }

    [FromQuery(Name = "soloVerificados")] public string? SoloVerificados { get; set; }

    [FromQuery(Name = "orden")] public string? Orden { get; set; }

    [FromQuery(Name = "cursor")] public string? Cursor { get; set; }

    [FromQuery(Name = "limite")] public string? Limite { get; set; }
}

/// <summary>Respuesta de TEC-02: técnicos visibles para la página y el cursor de la siguiente (nula si no hay más).</summary>
internal sealed record TechnicianListResponse(IReadOnlyList<TechnicianProfileResponse> Tecnicos, string? NextCursor);

/// <summary>Página interna de TEC-02: técnicos visibles con el cursor de la página siguiente (nula si no hay más).</summary>
internal sealed record TechnicianListPage(IReadOnlyList<TechnicianProfileRecord> Tecnicos, string? NextCursor);

/// <summary>Estado de la búsqueda de TEC-02: «ok», o por qué no hay listado.</summary>
internal enum TechnicianListStatus
{
    Ok,
    OficioInvalid,
    ColoniaNotFound,
    MissingPosition,
}

/// <summary>Resultado de la búsqueda de TEC-02: respuesta o estado del por qué no hay.</summary>
internal sealed record TechnicianListOutcome(TechnicianListStatus Status, TechnicianListResponse? Response);
