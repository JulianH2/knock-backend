// jscpd:ignore-start -- EXC-021: escritura de ProblemDetails parecida a src/Knock.Market.Api/Hosting/RequestBodyLimitMiddleware.cs (KNOCK-241); se unifica en la biblioteca técnica de KNOCK-251.
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Knock.Messaging.Api.Hosting;

/// <summary>Cuerpo de error uniforme (ProblemDetails) para el 401 y el 429 que escriben el JWT y el limitador; nunca repite datos de la petición.</summary>
internal static class ProblemResponses
{
    public static IServiceCollection AddKnockProblemDetails(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
                context.ProblemDetails.Instance = context.HttpContext.GetEndpoint() is RouteEndpoint { RoutePattern.RawText: { } route }
                    ? "/" + route.TrimStart('/')
                    : context.HttpContext.Request.Path);
        return services;
    }

    public static async Task WriteAsync(HttpContext context, int statusCode, string title, string? code = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Response.StatusCode = statusCode;
        if (statusCode == StatusCodes.Status401Unauthorized)
        {
            context.Response.Headers.WWWAuthenticate = "Bearer";
        }

        var problems = context.RequestServices.GetRequiredService<IProblemDetailsService>();
        var details = new ProblemDetails { Status = statusCode, Title = title };
        if (code is not null)
        {
            details.Extensions["code"] = code;
        }

        await problems.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = details });
    }
}
// jscpd:ignore-end -- EXC-021
