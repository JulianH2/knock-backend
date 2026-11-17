using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Knock.Market.Api.Modules.Requests.Controllers;

/// <summary>
/// Base de los controllers de Requests que responden reglas de negocio con ProblemDetails (mismo formato que el resto de errores: <c>traceId</c> y <c>correlationId</c>) y su
/// <c>code</c> estable. Nunca repite datos de la persona ni de la solicitud.
/// </summary>
internal abstract class BusinessRuleController : ControllerBase
{
    protected const string ProblemJson = "application/problem+json";

    /// <summary>Texto con el <c>sub</c> del token; vacío si no lo trae (el limitador y la autenticación ya lo exigen antes).</summary>
    protected string Subject() => User.FindFirst("sub")?.Value ?? string.Empty;

    /// <summary>Regla de negocio con su <c>code</c> y, si hace falta, una extensión con un dato no personal (por ejemplo <c>intentosRestantes</c>).</summary>
    protected ObjectResult Rule(int status, string code, string? extensionName = null, object? extensionValue = null)
    {
        var problem = ProblemDetailsFactory.CreateProblemDetails(HttpContext, status, ReasonPhrases.GetReasonPhrase(status));
        problem.Extensions["code"] = code;
        if (extensionName is not null)
        {
            problem.Extensions[extensionName] = extensionValue;
        }

        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { ProblemJson } };
    }
}
