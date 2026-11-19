// jscpd:ignore-start -- EXC-015
using Knock.Identity.Api.Common.Errors;
using Knock.Identity.Api.Modules.Panel.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;

namespace Knock.Identity.Api.Hosting;

/// <summary>
/// Traduce <see cref="DomainException"/> al código de su catálogo en ProblemDetails (mismo formato que el resto de errores).
/// Solo escribe el título estándar del código: ni el mensaje ni los datos de la excepción salen (el detalle de Development lo agrega <c>CustomizeProblemDetails</c>).
/// Cualquier otra excepción sigue su camino y termina en 500.
/// </summary>
internal sealed class DomainExceptionHandler : IExceptionHandler
{
    private const string _retryAfterSeconds = "30";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        if (exception is not DomainException domain)
        {
            return false;
        }

        var status = DomainErrors.ToStatusCode(domain.Kind);
        if (domain.Code is PanelErrorCodes.UnavailableCode or PanelErrorCodes.AuditUnavailableCode)
        {
            // Spec 033, §5: los 503 del panel llevan Retry-After.
            httpContext.Response.Headers.RetryAfter = _retryAfterSeconds;
        }

        await ProblemResponses.WriteAsync(httpContext, status, ReasonPhrases.GetReasonPhrase(status), domain.Code);
        return true;
    }
}
// jscpd:ignore-end
