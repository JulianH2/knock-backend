namespace Knock.Api.Hosting;

/// <summary>
/// <c>Cache-Control: no-store</c> en toda respuesta de <c>/api/v{n}/auth/*</c> (tokens de sesión, spec HU-001): también en 4xx, 413 y 429.
/// Se escribe al iniciar la respuesta para que el manejador de excepciones no lo reemplace.
/// </summary>
internal sealed class AuthNoStoreMiddleware(RequestDelegate next)
{
    private const string _apiPrefix = "/api/v";
    private const string _authSegment = "/auth";

    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (IsAuthPath(context.Request.Path.Value))
        {
            context.Response.OnStarting(
                static state =>
                {
                    ((HttpContext)state).Response.Headers.CacheControl = "no-store";
                    return Task.CompletedTask;
                },
                context);
        }

        return next(context);
    }

    private static bool IsAuthPath(string? path)
    {
        if (path is null || !path.StartsWith(_apiPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var slash = path.IndexOf('/', _apiPrefix.Length);
        if (slash < 0 || !path.AsSpan(slash).StartsWith(_authSegment, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var end = slash + _authSegment.Length;
        return path.Length == end || path[end] == '/';
    }
}
