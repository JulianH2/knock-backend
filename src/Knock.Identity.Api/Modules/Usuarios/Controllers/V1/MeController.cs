using System.Security.Cryptography;
using System.Text;
using Asp.Versioning;
using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Usuarios.Domain;
using Knock.Identity.Api.Modules.Usuarios.Dtos;
using Knock.Identity.Api.Modules.Usuarios.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Knock.Identity.Api.Modules.Usuarios.Controllers.V1;

/// <summary>
/// Perfil propio (spec 017). La ruta no lleva identificador: la persona es siempre la del <c>sub</c> del token, así nadie puede pedir el de otra (R-09).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me")]
[Authorize]
internal sealed class MeController(ProfileService profiles, ProfileRules rules) : ControllerBase
{
    /// <summary>Límite del cuerpo del <c>PUT</c>: 4 KiB (cuatro campos cortos).</summary>
    public const int MaxBodyBytes = 4096;

    [HttpGet]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (Sub() is not { } sub)
        {
            return Unauthorized();
        }

        var profile = await profiles.GetAsync(sub, cancellationToken);
        var etag = ETag(profile);
        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "private, no-cache";
        return Matches(Request.Headers.IfNoneMatch.ToString(), etag) ? StatusCode(StatusCodes.Status304NotModified) : Ok(ToResponse(profile));
    }

    /// <summary>Actualiza el perfil. Los campos inválidos responden 422 con errores por campo; una colonia inexistente responde 422 con su código.</summary>
    [HttpPut]
    [RequestSizeLimit(MaxBodyBytes)]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Put([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (Sub() is not { } sub)
        {
            return Unauthorized();
        }

        var (update, errors) = rules.Validate(request);
        if (update is null)
        {
            return await ValidationFilter.WriteErrorsAsync(HttpContext, errors);
        }

        var saved = await profiles.UpdateAsync(sub, update, cancellationToken);
        return Ok(ToResponse(saved));
    }

    /// <summary>ETag débil: cambia con cada escritura (<c>version</c>) y es distinto entre personas, para que un cliente que cambia de cuenta nunca reciba un 304 ajeno.</summary>
    internal static string ETag(UserProfile profile)
    {
        var owner = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(profile.Id)))[..12];
        return $"W/\"{owner}-{profile.Version}\"";
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

    private static ProfileResponse ToResponse(UserProfile profile) => new(
        profile.Id,
        profile.Name,
        profile.PhotoUrl,
        profile.Colonia,
        profile.PostalCode,
        profile.IsTechnician,
        profile.State,
        profile.ProfileComplete,
        profile.CreatedAt);

    private string? Sub() => User.FindFirst("sub")?.Value is { Length: > 0 } sub ? sub : null;
}
