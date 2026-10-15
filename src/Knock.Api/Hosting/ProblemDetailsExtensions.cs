using Microsoft.AspNetCore.Diagnostics;

namespace Knock.Api.Hosting;

internal static class ProblemDetailsExtensions
{
    /// <summary>RFC 9457 para toda respuesta de error; el detalle de la excepción solo se incluye en Development.</summary>
    public static IServiceCollection AddKnockProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
            {
                var environment = context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
                var exception = context.HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
                if (exception is not null && environment.IsDevelopment())
                {
                    context.ProblemDetails.Detail = exception.Message;
                    context.ProblemDetails.Extensions["stackTrace"] = exception.StackTrace;
                }
            });
        return services;
    }
}
