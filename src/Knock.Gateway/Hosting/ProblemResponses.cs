// jscpd:ignore-start -- EXC-016
using Knock.Contracts.Correlation;
using Microsoft.AspNetCore.Routing;

namespace Knock.Gateway.Hosting;

/// <summary>Respuestas ProblemDetails (RFC 9457) del gateway: mismo cuerpo para 401, 429 y 503, sin motivos internos, con <c>correlationId</c>.</summary>
internal static class ProblemResponses
{
    public static async Task WriteAsync(HttpContext context, int statusCode, string title)
    {
        context.Response.StatusCode = statusCode;
        if (statusCode == StatusCodes.Status401Unauthorized)
        {
            context.Response.Headers.WWWAuthenticate = "Bearer";
        }

        var problems = context.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problems.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = { Status = statusCode, Title = title },
        });
    }

    public static IServiceCollection AddKnockGatewayProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Type ??= "about:blank";
                context.ProblemDetails.Instance = context.HttpContext.GetEndpoint() is RouteEndpoint { RoutePattern.RawText: { } route }
                    ? "/" + route.TrimStart('/')
                    : context.HttpContext.Request.Path;
                if (CorrelationId.Current(context.HttpContext) is { } correlationId)
                {
                    context.ProblemDetails.Extensions["correlationId"] = correlationId;
                }

                context.ProblemDetails.Extensions["traceId"] = System.Diagnostics.Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;
            });
        return services;
    }
}
// jscpd:ignore-end
