using System.Globalization;
using Knock.Identity.Api.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Knock.Identity.Api.Modules.Panel.Controllers;

/// <summary>Ayudas HTTP del panel: ProblemDetails con <c>code</c>, <c>Retry-After</c> y la cookie de renovación (spec 033, R-16 y §5).</summary>
internal static class PanelHttp
{
    public const string CookieName = "__Secure-knock-panel-rt";
    public const string CookiePath = "/api/v1/panel/sesion";
    public const string MarkerHeader = "X-Knock-Panel";
    public const string MarkerValue = "1";

    private const int _cookieMaxAgeSeconds = 20 * 60;
    private const string _cookieAttributes = "; Path=" + CookiePath + "; Secure; HttpOnly; SameSite=None; Partitioned";

    /// <summary>IP de la conexión normalizada igual que el limitador (IPv6 por /64): es la que cuenta para los fallos por IP.</summary>
    public static string ClientIp(HttpContext context) => RateLimitingExtensions.ClientKey(context);

    /// <summary>Escribe el error con su <c>code</c> y, si aplica, <c>Retry-After</c>; devuelve un resultado vacío porque la respuesta ya está escrita.</summary>
    public static async Task<IActionResult> ProblemAsync(HttpContext context, int status, string code, TimeSpan? retryAfter = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (retryAfter is { } wait)
        {
            context.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(wait.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        await ProblemResponses.WriteAsync(context, status, ReasonPhrases.GetReasonPhrase(status), code);
        return new EmptyResult();
    }

    /// <summary>
    /// <c>__Secure-knock-panel-rt=&lt;token&gt;; HttpOnly; Secure; SameSite=None; Partitioned; Path=/api/v1/panel/sesion; Max-Age=1200</c>. Se escribe a mano porque
    /// <c>CookieOptions</c> no tiene el atributo <c>Partitioned</c>. El token nunca aparece en un cuerpo.
    /// </summary>
    public static void SetRefreshCookie(HttpResponse response, string token)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        response.Headers.Append("Set-Cookie", $"{CookieName}={token}{_cookieAttributes}; Max-Age={_cookieMaxAgeSeconds.ToString(CultureInfo.InvariantCulture)}");
    }

    /// <summary>Borra la cookie: misma ruta y atributos con <c>Max-Age=0</c>.</summary>
    public static void ClearRefreshCookie(HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        response.Headers.Append("Set-Cookie", $"{CookieName}={_cookieAttributes}; Max-Age=0; Expires=Thu, 01 Jan 1970 00:00:00 GMT");
    }
}
