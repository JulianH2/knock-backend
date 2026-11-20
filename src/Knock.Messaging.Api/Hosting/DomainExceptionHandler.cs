using Knock.Messaging.Api.Modules.Conversaciones.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;

namespace Knock.Messaging.Api.Hosting;

/// <summary>
/// Traduce <see cref="DomainException"/> al código de su catálogo en ProblemDetails. Solo escribe el título estándar del código: ni el mensaje ni los datos de la excepción salen
/// (el detalle de Development lo agrega <c>CustomizeProblemDetails</c>). Cualquier otra excepción sigue su camino y termina en 500.
/// </summary>
internal sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        if (exception is not DomainException domain)
        {
            return false;
        }

        var status = DomainErrors.ToStatusCode(domain.Kind);
        await ProblemResponses.WriteAsync(httpContext, status, ReasonPhrases.GetReasonPhrase(status), domain.Code);
        return true;
    }
}
