using Microsoft.AspNetCore.Diagnostics;

namespace Knock.Api.Hosting;

internal static class ProblemDetailsExtensions
{
    /// <summary>RFC 9457 para toda respuesta de error, con <c>correlationId</c>; el detalle de la excepción solo se incluye en Development. Registra el manejador de excepciones de dominio.</summary>
    public static IServiceCollection AddKnockProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
            {
                // RFC 9457: sin «type» propio, «about:blank». Así todos los errores (429, 413…) tienen la misma forma.
                context.ProblemDetails.Type ??= "about:blank";
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
}
