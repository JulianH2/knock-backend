using System.Globalization;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Knock.Api.Hosting;

/// <summary>
/// CORS por configuración (TT-API-06): orígenes explícitos de <c>Security:Cors:AllowedOrigins</c> con comparación exacta, sin comodín ni credenciales.
/// Lista vacía = CORS apagado. Está escrito a mano y no con <c>UseCors</c> por dos razones: el preflight se responde 204 aquí mismo, sin enrutar
/// (una ruta que solo acepta POST no da 405 ni exige sesión), y los encabezados de la respuesta se fijan en <c>OnStarting</c> para que sobrevivan
/// al manejador de excepciones (el panel debe poder leer también el 500). Va antes del limitador: el 429 lleva CORS y el preflight no cuenta.
/// </summary>
internal sealed class CorsMiddleware(RequestDelegate next, IOptions<SecuritySettings> options)
{
    public static readonly string[] AllowedMethods = ["GET", "POST", "PUT", "PATCH", "DELETE"];

    public static readonly string[] AllowedHeaders = ["Authorization", "Content-Type", "X-Correlation-Id", "Idempotency-Key", "If-Match"];

    public static readonly string[] ExposedHeaders = ["X-Correlation-Id", "Retry-After", "ETag"];

    private readonly HashSet<string> _origins = new(options.Value.Cors.AllowedOrigins, StringComparer.Ordinal);
    private readonly string _maxAgeSeconds = ((int)options.Value.Cors.PreflightMaxAge.TotalSeconds).ToString(CultureInfo.InvariantCulture);

    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_origins.Count == 0)
        {
            return next(context);
        }

        var request = context.Request;
        var origin = request.Headers.Origin;
        if (origin.Count != 1 || origin[0] is not { } value || !_origins.Contains(value))
        {
            // El origen no está en la lista (o no hay): ningún encabezado Access-Control-Allow-*; solo Vary para que las cachés no mezclen respuestas.
            context.Response.OnStarting(state => VaryOnOrigin((HttpContext)state), context);
            return next(context);
        }

        if (HttpMethods.IsOptions(request.Method) && request.Headers.ContainsKey(HeaderNames.AccessControlRequestMethod))
        {
            return AnswerPreflightAsync(context, value);
        }

        context.Response.OnStarting(state => ApplyToResponse(((HttpContext, string))state), (context, value));
        return next(context);
    }

    private Task AnswerPreflightAsync(HttpContext context, string origin)
    {
        var request = context.Request;
        var response = context.Response;
        response.StatusCode = StatusCodes.Status204NoContent;
        AddVaryOrigin(context);
        if (IsGranted(request))
        {
            response.Headers.AccessControlAllowOrigin = origin;
            response.Headers.AccessControlAllowMethods = string.Join(", ", AllowedMethods);
            response.Headers.AccessControlAllowHeaders = string.Join(", ", AllowedHeaders);
            response.Headers.AccessControlMaxAge = _maxAgeSeconds;
        }

        return Task.CompletedTask;
    }

    /// <summary>El método pedido y cada encabezado pedido deben estar en las listas; si no, no se concede nada.</summary>
    private static bool IsGranted(HttpRequest request)
    {
        var method = request.Headers.AccessControlRequestMethod.ToString();
        if (!AllowedMethods.Contains(method, StringComparer.Ordinal))
        {
            return false;
        }

        var requested = request.Headers.AccessControlRequestHeaders.ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return requested.All(name => AllowedHeaders.Contains(name, StringComparer.OrdinalIgnoreCase));
    }

    private static Task ApplyToResponse((HttpContext Context, string Origin) state)
    {
        var headers = state.Context.Response.Headers;
        headers.AccessControlAllowOrigin = state.Origin;
        headers.AccessControlExposeHeaders = string.Join(", ", ExposedHeaders);
        return VaryOnOrigin(state.Context);
    }

    private static Task VaryOnOrigin(HttpContext context)
    {
        AddVaryOrigin(context);
        return Task.CompletedTask;
    }

    private static void AddVaryOrigin(HttpContext context)
    {
        var headers = context.Response.Headers;
        if (!headers.Vary.Any(v => v is not null && v.Contains(HeaderNames.Origin, StringComparison.OrdinalIgnoreCase)))
        {
            headers.Append(HeaderNames.Vary, HeaderNames.Origin);
        }
    }
}
