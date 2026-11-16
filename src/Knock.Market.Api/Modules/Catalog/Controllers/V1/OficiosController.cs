using System.Security.Cryptography;
using System.Text;
using Asp.Versioning;
using Knock.Market.Api.Modules.Catalog.Dtos;
using Knock.Market.Api.Modules.Catalog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Market.Api.Modules.Catalog.Controllers.V1;

/// <summary>Catálogo público de oficios (spec TT-PRU-01): la app lo necesita antes o durante el registro.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/oficios")]
internal sealed class OficiosController(OficioQueries oficios) : ControllerBase
{
    /// <summary>Los clientes y proxies guardan la respuesta pero siempre revalidan con <c>If-None-Match</c> (304); la vigencia de 24 h es solo del servidor, en Redis.</summary>
    private const string _cacheControl = "public, max-age=0, must-revalidate";

    private static readonly System.Text.Json.JsonSerializerOptions _payloadOptions = new() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };

    /// <summary>
    /// Oficios activos como arreglo sin paginar: excepción documentada a la constitución §11 (catálogo acotado, D9).
    /// <c>[AllowAnonymous]</c> explícito para que siga público aunque se agregue una política de autorización global (D3).
    /// Responde con <c>ETag</c> débil, <c>Cache-Control: public, max-age=0, must-revalidate</c> y <c>Vary: Accept-Encoding</c> (la respuesta comprimida es otra variante);
    /// con <c>If-None-Match</c> vigente responde 304 sin cuerpo.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<OficioResponse>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<IActionResult> List([FromHeader(Name = "If-None-Match")] string? ifNoneMatch, CancellationToken cancellationToken)
    {
        var items = await oficios.ListActiveAsync(cancellationToken);
        var etag = ETag(items);
        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = _cacheControl;
        return Matches(ifNoneMatch ?? string.Empty, etag) ? StatusCode(StatusCodes.Status304NotModified) : Ok(items);
    }

    /// <summary>ETag débil a partir del contenido serializado: determinista para el mismo catálogo y distinto ante cualquier cambio.</summary>
    internal static string ETag(IReadOnlyList<OficioResponse> items) => ETagPayload(items);

    internal static string ETagPayload<T>(T items)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(items, _payloadOptions);
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        return $"W/\"{hash}\"";
    }

    // jscpd:ignore-start -- EXC-018
    internal static bool Matches(string ifNoneMatch, string etag)
    {
        if (string.IsNullOrWhiteSpace(ifNoneMatch))
        {
            return false;
        }

        var wanted = Strip(etag);
        return ifNoneMatch.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(candidate => candidate == "*" || Strip(candidate) == wanted);
    }
    // jscpd:ignore-end -- EXC-018

    private static string Strip(string tag) => tag.StartsWith("W/", StringComparison.Ordinal) ? tag[2..] : tag;
}
