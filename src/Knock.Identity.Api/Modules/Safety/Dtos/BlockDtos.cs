using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Identity.Api.Modules.Safety.Dtos;

/// <summary>Una persona bloqueada: su identificador público, el nombre público y cuándo se bloqueó. Nunca teléfono ni otros datos (spec 043, R-19).</summary>
internal sealed record BlockedPerson(string UsuarioId, string Nombre, DateTime BloqueadoEn);

/// <summary>Una página de bloqueos; <c>NextCursor</c> falta en la última.</summary>
internal sealed record BlockedPage(
    IReadOnlyList<BlockedPerson> Items,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? NextCursor);

/// <summary>Parámetros de <c>GET /me/bloqueos</c>: <c>limite</c> de 1 a 50 (20 por omisión) y el cursor de la página anterior.</summary>
internal sealed class BlocksQuery
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

/// <summary>La persona de la ruta de <c>POST</c> y <c>DELETE /usuarios/{id}/bloqueo</c>: su identificador público.</summary>
internal sealed class BlockRoute
{
    [FromRoute(Name = "id")]
    public string? Id { get; set; }
}
