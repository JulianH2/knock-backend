using Microsoft.AspNetCore.Http.Extensions;

namespace Knock.Gateway.Hosting;

/// <summary>
/// El cliente WebSocket o de eventos del servidor no puede enviar el encabezado <c>Authorization</c> en el saludo, así que la app manda el token en la consulta (<c>access_token</c>). Solo para
/// el hub de chat (<c>/hubs/chat</c>, spec 026, R-02 y R-18): se pasa a <c>Authorization: Bearer</c> antes de autenticar y el parámetro se quita de la consulta, de modo que ni el servicio
/// ni los registros lo vean en la URL. En cualquier otra ruta el parámetro no tiene efecto (un token en la URL de una ruta normal sigue siendo 401).
/// </summary>
internal sealed class HubQueryTokenMiddleware(RequestDelegate next)
{
    public const string HubPath = "/hubs/chat";

    public const string QueryParameter = "access_token";

    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Request.Path.StartsWithSegments(HubPath, StringComparison.OrdinalIgnoreCase) && context.Request.Query.TryGetValue(QueryParameter, out var tokens))
        {
            // Un encabezado propio manda sobre la consulta, y varios valores no se adivinan: sin encabezado, la petición sigue sin token y responde 401.
            if (tokens.Count == 1 && tokens[0] is { Length: > 0 } token && string.IsNullOrEmpty(context.Request.Headers.Authorization))
            {
                context.Request.Headers.Authorization = "Bearer " + token;
            }

            var rest = new QueryBuilder(context.Request.Query
                .Where(pair => !pair.Key.Equals(QueryParameter, StringComparison.OrdinalIgnoreCase))
                .SelectMany(pair => pair.Value.Select(value => new KeyValuePair<string, string>(pair.Key, value ?? string.Empty))));
            context.Request.QueryString = rest.ToQueryString();
        }

        return next(context);
    }
}
