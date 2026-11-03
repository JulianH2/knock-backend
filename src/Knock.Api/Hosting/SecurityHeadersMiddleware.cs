using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Knock.Api.Hosting;

/// <summary>
/// Encabezados de seguridad de respuesta (TT-API-06). Se fijan en <c>Response.OnStarting</c> y por asignación: el manejador de excepciones limpia los
/// encabezados antes de escribir el error y el limitador responde antes que el resto, así que ninguna respuesta (200, 4xx, 429, 500) queda sin ellos
/// y ninguno se duplica. Va antes del manejador de excepciones.
/// </summary>
internal sealed class SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment environment)
{
    public const string ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";

    private static readonly string[] _leakyHeaders = ["Server", "X-Powered-By", "X-AspNet-Version"];

    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Response.OnStarting(state => Apply((HttpContext)state), context);
        return next(context);
    }

    private Task Apply(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        if (SkipsContentSecurityPolicy(context.Request.Path))
        {
            headers.Remove(HeaderNames.ContentSecurityPolicy);
        }
        else
        {
            headers.ContentSecurityPolicy = ContentSecurityPolicy;
        }

        // `no-store` solo si el endpoint no declaró el suyo: un endpoint con ETag podrá fijar su propio Cache-Control.
        if (StringValues.IsNullOrEmpty(headers.CacheControl))
        {
            headers.CacheControl = "no-store";
        }

        foreach (var leaky in _leakyHeaders)
        {
            headers.Remove(leaky);
        }

        return Task.CompletedTask;
    }

    /// <summary>Scalar sirve HTML y scripts: en Development, y solo para su ruta y la del documento OpenAPI, la CSP estricta no se aplica (el resto sí).</summary>
    private bool SkipsContentSecurityPolicy(PathString path) =>
        environment.IsDevelopment()
        && (path.StartsWithSegments("/scalar", StringComparison.OrdinalIgnoreCase) || path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase));
}
