using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Market.Api.Modules.Trust.Dtos;

/// <summary>Un técnico guardado como de confianza: solo datos públicos, nunca teléfono, WhatsApp ni dirección (spec 042, R-12).</summary>
internal sealed record TrustedTechnician(
    string TecnicoId,
    string Nombre,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? FotoUrl,
    string NivelVerificacion,
    DateTime GuardadoEn);

/// <summary>Una página de la lista; <c>NextCursor</c> falta en la última.</summary>
internal sealed record TrustedTechnicianPage(
    IReadOnlyList<TrustedTechnician> Items,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? NextCursor);

/// <summary>Parámetros de <c>GET /me/tecnicos-confianza</c>: <c>limite</c> de 1 a 50 (20 por omisión) y el cursor de la página anterior.</summary>
internal sealed class TrustedListQuery
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 50;

    [FromQuery(Name = "cursor")]
    [Description("Opaque cursor returned as nextCursor by the previous page.")]
    public string? Cursor { get; set; }

    [FromQuery(Name = "limite")]
    [Description("Page size, 1 to 50. Defaults to 20.")]
    public int Limite { get; set; } = DefaultLimit;
}

/// <summary>El técnico de la ruta de <c>PUT</c> y <c>DELETE /me/tecnicos-confianza/{tecnicoId}</c>.</summary>
internal sealed class TrustedTechnicianRoute
{
    [FromRoute(Name = "tecnicoId")]
    public string? TecnicoId { get; set; }
}
