using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace Knock.Identity.Api.Modules.Account.Controllers.V1;

/// <summary>
/// Base de los controladores de la spec 039. La persona es siempre la del <c>sub</c> del token (R-27): ninguna ruta, consulta ni cuerpo trae un
/// identificador de persona, y el <c>sid</c> es la sesión desde la que se llama.
/// </summary>
internal abstract class AccountControllerBase : ControllerBase
{
    /// <summary>El <c>sub</c> y el <c>sid</c> del token; falso si alguno falta o no tiene la forma que emite Identidad.</summary>
    protected bool TryGetCaller(out string sub, out string sessionId)
    {
        sub = User.FindFirst("sub")?.Value ?? string.Empty;
        sessionId = User.FindFirst("sid")?.Value ?? string.Empty;
        var wellFormed = Guid.TryParse(sub, out _);
        return wellFormed && ObjectId.TryParse(sessionId, out _);
    }

    protected ObjectResult UnauthorizedProblem() => Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");
}
