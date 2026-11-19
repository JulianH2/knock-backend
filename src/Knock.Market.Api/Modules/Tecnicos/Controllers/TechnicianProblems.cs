using Knock.Market.Api.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Knock.Market.Api.Modules.Tecnicos.Controllers;

/// <summary>Reglas de negocio de los controladores de técnicos en ProblemDetails, con <c>code</c> y, si se nombra un campo, <c>errors</c> sin repetir el valor.</summary>
internal static class TechnicianProblems
{
    public const string ProblemJson = "application/problem+json";

    public static ObjectResult Rule(ControllerBase controller, int status, string? code, string? field = null)
    {
        var problem = controller.ProblemDetailsFactory.CreateProblemDetails(controller.HttpContext, status, ReasonPhrases.GetReasonPhrase(status));
        if (code is not null)
        {
            problem.Extensions["code"] = code;
        }

        if (field is not null)
        {
            problem.Extensions["errors"] = new Dictionary<string, string[]> { [field] = [ValidationFilter.GenericReason] };
        }

        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { ProblemJson } };
    }
}
