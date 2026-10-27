using Serilog.Context;

namespace Knock.Api.Hosting;

/// <summary>Reglas del <c>X-Correlation-Id</c>: máximo 64 caracteres de <c>[A-Za-z0-9._-]</c>; si no, se genera un GUID sin guiones.</summary>
internal static class CorrelationId
{
    public const string HeaderName = "X-Correlation-Id";
    public const string LogProperty = "CorrelationId";
    public const string ItemKey = "Knock.CorrelationId";
    public const int MaxLength = 64;

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > MaxLength)
        {
            return false;
        }

        foreach (var c in value)
        {
            var allowed = c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '.' or '_' or '-';
            if (!allowed)
            {
                return false;
            }
        }

        return true;
    }

    public static string Create() => Guid.NewGuid().ToString("N");

    /// <summary>Id de la petición actual (lo fija <see cref="CorrelationIdMiddleware"/>); <c>null</c> si el middleware no corrió.</summary>
    public static string? Current(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Items.TryGetValue(ItemKey, out var value) ? value as string : null;
    }
}

/// <summary>
/// Asigna o propaga el <c>X-Correlation-Id</c>: lo valida, lo devuelve en la respuesta (incluso en errores), lo pone en el cuerpo de los errores
/// (vía <see cref="ProblemDetailsExtensions"/>) y en cada log de la petición con <c>LogContext</c>. Es solo para rastrear, nunca para autorizar.
/// </summary>
internal sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var incoming = context.Request.Headers[CorrelationId.HeaderName];
        var id = incoming.Count == 1 && CorrelationId.IsValid(incoming[0]) ? incoming[0]! : CorrelationId.Create();

        context.Items[CorrelationId.ItemKey] = id;
        // OnStarting y no un encabezado directo: el manejador de excepciones limpia la respuesta antes de escribir el error.
        context.Response.OnStarting(
            state =>
            {
                var (response, value) = ((HttpResponse, string))state;
                response.Headers[CorrelationId.HeaderName] = value;
                return Task.CompletedTask;
            },
            (context.Response, id));

        using (LogContext.PushProperty(CorrelationId.LogProperty, id))
        {
            await next(context);
        }
    }
}
