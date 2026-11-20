// jscpd:ignore-start -- EXC-021: escritura de ProblemDetails parecida a src/Knock.Market.Api/Hosting/RequestBodyLimitMiddleware.cs (KNOCK-241); se unifica en la biblioteca técnica de KNOCK-251.
using Knock.Contracts.Correlation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Knock.Messaging.Api.Hosting;

/// <summary>Cuerpo de error uniforme (ProblemDetails) para el 401 y el 429 que escriben el JWT y el limitador; nunca repite datos de la petición.</summary>
internal static class ProblemResponses
{
    /// <summary>
    /// RFC 9457 para toda respuesta de error, con <c>traceId</c> y <c>correlationId</c>; el detalle de la excepción solo se incluye en Development. Registra el manejador de excepciones
    /// de dominio, que escribe el <c>code</c> de su catálogo.
    /// </summary>
    public static IServiceCollection AddKnockProblemDetails(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
            {
                // Sin «type» propio, «about:blank»: todos los errores (401, 429, 422…) tienen la misma forma.
                context.ProblemDetails.Type ??= "about:blank";
                context.ProblemDetails.Extensions["traceId"] = System.Diagnostics.Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;
                if (CorrelationId.Current(context.HttpContext) is { } correlationId)
                {
                    context.ProblemDetails.Extensions["correlationId"] = correlationId;
                }

                var environment = context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
                var exception = context.HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
                if (exception is not null && environment.IsDevelopment())
                {
                    context.ProblemDetails.Detail = exception.Message;
                    context.ProblemDetails.Extensions["stackTrace"] = exception.StackTrace;
                }
            });
        services.AddExceptionHandler<DomainExceptionHandler>();
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
