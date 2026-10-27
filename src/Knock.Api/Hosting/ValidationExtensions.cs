using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Knock.Api.Hosting;

internal static class ValidationExtensions
{
    /// <summary>Registra los validadores de FluentValidation del ensamblado (incluidos los internal); el filtro que responde 422 se agrega a MVC.</summary>
    public static IServiceCollection AddKnockValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ValidationFilter>(includeInternalTypes: true);
        return services;
    }
}

/// <summary>
/// Filtro propio (sin FluentValidation.AspNetCore): valida cada argumento de la acción que tenga un <see cref="IValidator{T}"/> registrado
/// y, si falla, responde 422 ProblemDetails con <c>errors</c> (campo en camelCase a motivos) sin ejecutar el controller.
/// Nunca devuelve el valor enviado: si un mensaje lo repite, se sustituye por un motivo genérico.
/// </summary>
internal sealed class ValidationFilter : IAsyncActionFilter
{
    public const string GenericReason = "The value is not valid.";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null
                || context.HttpContext.RequestServices.GetService(typeof(IValidator<>).MakeGenericType(argument.GetType())) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
            Collect(result, errors);
        }

        if (errors.Count == 0)
        {
            await next();
            return;
        }

        var problems = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        context.HttpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "One or more validation errors occurred.",
        };
        problem.Extensions["errors"] = errors.ToDictionary(e => e.Key, e => e.Value.ToArray(), StringComparer.Ordinal);
        await problems.WriteAsync(new ProblemDetailsContext { HttpContext = context.HttpContext, ProblemDetails = problem });
        context.Result = new EmptyResult();
    }

    internal static string ToCamelCasePath(string propertyName) =>
        string.Join('.', propertyName.Split('.').Select(segment =>
            segment.Length == 0 ? segment : string.Concat(char.ToLowerInvariant(segment[0]).ToString(), segment.AsSpan(1))));

    private static void Collect(ValidationResult result, Dictionary<string, List<string>> errors)
    {
        foreach (var failure in result.Errors)
        {
            var field = ToCamelCasePath(failure.PropertyName);
            var attempted = failure.AttemptedValue?.ToString();
            var reason = !string.IsNullOrEmpty(attempted) && failure.ErrorMessage.Contains(attempted, StringComparison.OrdinalIgnoreCase)
                ? GenericReason
                : failure.ErrorMessage;
            if (!errors.TryGetValue(field, out var reasons))
            {
                errors[field] = reasons = [];
            }

            if (!reasons.Contains(reason, StringComparer.Ordinal))
            {
                reasons.Add(reason);
            }
        }
    }
}
