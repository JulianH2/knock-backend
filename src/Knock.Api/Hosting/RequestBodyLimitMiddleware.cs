using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;

namespace Knock.Api.Hosting;

/// <summary>Respuestas ProblemDetails (RFC 9457) escritas desde middleware o eventos de autenticación.</summary>
internal static class ProblemResponses
{
    public static async Task WriteAsync(HttpContext context, int statusCode, string title)
    {
        context.Response.StatusCode = statusCode;
        if (statusCode == StatusCodes.Status401Unauthorized)
        {
            context.Response.Headers.WWWAuthenticate = "Bearer";
        }

        var problems = context.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problems.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = { Status = statusCode, Title = title },
        });
    }
}

/// <summary>
/// Aplica el límite de cuerpo declarado con <c>[RequestSizeLimit]</c> y responde 413 en ProblemDetails.
/// Con <c>Content-Length</c> se rechaza antes de leer; sin él, el servidor corta la lectura y se traduce a 413.
/// </summary>
internal sealed class RequestBodyLimitMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var limit = context.GetEndpoint()?.Metadata.GetMetadata<IRequestSizeLimitMetadata>()?.MaxRequestBodySize;
        if (limit is null)
        {
            await next(context);
            return;
        }

        if (context.Request.ContentLength > limit)
        {
            await ProblemResponses.WriteAsync(context, StatusCodes.Status413PayloadTooLarge, "Request body too large");
            return;
        }

        var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (feature is { IsReadOnly: false })
        {
            feature.MaxRequestBodySize = limit;
        }

        try
        {
            await next(context);
        }
        catch (BadHttpRequestException ex) when (ex.StatusCode == StatusCodes.Status413PayloadTooLarge && !context.Response.HasStarted)
        {
            await ProblemResponses.WriteAsync(context, StatusCodes.Status413PayloadTooLarge, "Request body too large");
        }
    }
}
