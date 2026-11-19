using Microsoft.OpenApi;

namespace Knock.Identity.Api.Modules.Panel;

/// <summary>
/// Documenta en el contrato lo que MVC no puede inferir (spec 033, §5): la cookie de renovación en <c>Set-Cookie</c> de las respuestas 200 de
/// <c>totp</c> y <c>renovar</c>, el borrado de la cookie en <c>cerrar</c> y el encabezado obligatorio <c>X-Knock-Panel: 1</c> de <c>renovar</c> y <c>cerrar</c>.
/// </summary>
internal static class PanelOpenApi
{
    public const string CookieName = "__Secure-knock-panel-rt";
    public const string MarkerHeader = "X-Knock-Panel";

    private const string _cookieHeaderDescription =
        CookieName + "=<token>; HttpOnly; Secure; SameSite=None; Partitioned; Path=/api/v1/panel/sesion; Max-Age=1200. The refresh token never appears in a body.";

    private const string _clearCookieDescription =
        CookieName + "=; HttpOnly; Secure; SameSite=None; Partitioned; Path=/api/v1/panel/sesion; Max-Age=0 (clears the refresh cookie).";

    public static void Describe(OpenApiOperation operation, string? relativePath)
    {
        ArgumentNullException.ThrowIfNull(operation);
        switch (relativePath)
        {
            case "api/v{version}/panel/sesion/totp":
            case "api/v{version}/panel/sesion/renovar":
                AddSetCookie(operation, "200", _cookieHeaderDescription);
                break;
            case "api/v{version}/panel/sesion/cerrar":
                AddSetCookie(operation, "204", _clearCookieDescription);
                break;
            default:
                break;
        }

        if (relativePath is "api/v{version}/panel/sesion/renovar" or "api/v{version}/panel/sesion/cerrar")
        {
            operation.Parameters ??= [];
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = MarkerHeader,
                In = ParameterLocation.Header,
                Required = true,
                Description = "Must be 1: it forces a CORS preflight that only the panel origin passes.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String, Const = "1" },
            });
        }
    }

    private static void AddSetCookie(OpenApiOperation operation, string status, string description)
    {
        if (operation.Responses is { } responses && responses.TryGetValue(status, out var response) && response is OpenApiResponse concrete)
        {
            concrete.Headers ??= new Dictionary<string, IOpenApiHeader>(StringComparer.OrdinalIgnoreCase);
            concrete.Headers["Set-Cookie"] = new OpenApiHeader
            {
                Description = description,
                Schema = new OpenApiSchema { Type = JsonSchemaType.String },
            };
        }
    }
}
