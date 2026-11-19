using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Identity.Api.Modules.Safety.Dtos;

/// <summary>La referencia opcional de un reporte: de qué solicitud, visita u opinión viene (spec 043, R-20).</summary>
internal sealed class ReportReferenceBody
{
    public string? Tipo { get; set; }

    public string? Id { get; set; }
}

/// <summary>Cuerpo de <c>POST /reportes</c> (spec 043, R-10). Ningún campo acepta el identificador de quien reporta: sale del token (R-24).</summary>
internal sealed class CreateReportBody
{
    public const int MaxBodyBytes = 4096;

    public string? ReportadoId { get; set; }

    public ReportReferenceBody? Referencia { get; set; }

    public string? Motivo { get; set; }

    public string? Descripcion { get; set; }

    public IReadOnlyList<string>? Evidencia { get; set; }
}

/// <summary>Cuerpo de <c>POST /reportes/{id}/apelacion</c> (spec 043, R-14): el texto de la apelación.</summary>
internal sealed class AppealBody
{
    public const int MaxBodyBytes = 2048;

    public string? Texto { get; set; }
}

/// <summary>La ruta de <c>POST /reportes/{id}/apelacion</c>.</summary>
internal sealed class ReportRoute
{
    [FromRoute(Name = "id")]
    public string? Id { get; set; }
}

/// <summary>Parámetros de <c>GET /reportes/me</c>: <c>limite</c> de 1 a 50 (20 por omisión) y el cursor de la página anterior.</summary>
internal sealed class ReportsQuery
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

/// <summary>Lo que recibe quien reporta al crear: el reporte, su estado y, si el motivo implica peligro, el aviso de llamar al 911.</summary>
internal sealed record ReportCreated(
    string ReporteId,
    string Estado,
    DateTime CreadoEn,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? AvisoEmergencia);

/// <summary>Un reporte de la persona que lo hizo. Nunca lleva el texto de la otra parte ni datos de ella (R-22).</summary>
internal sealed record ReportSummary(string ReporteId, string Motivo, string Estado, DateTime CreadoEn, bool PuedeApelar);

/// <summary>Una página de reportes propios; <c>NextCursor</c> falta en la última.</summary>
internal sealed record ReportPage(
    IReadOnlyList<ReportSummary> Items,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? NextCursor);

/// <summary>Lo que se responde al apelar: el reporte y su estado nuevo.</summary>
internal sealed record AppealResult(string ReporteId, string Estado);
