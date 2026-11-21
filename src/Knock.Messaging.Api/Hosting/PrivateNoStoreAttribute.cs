using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace Knock.Messaging.Api.Hosting;

/// <summary>
/// <c>Cache-Control: private, no-store</c> en toda respuesta del controller (spec 030, R-20): las conversaciones y los contadores son datos personales que cambian al escribir, y no
/// llevan <c>ETag</c>. Se fija al empezar la acción.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal sealed class PrivateNoStoreAttribute : ActionFilterAttribute
{
    public const string Value = "private, no-store";

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.HttpContext.Response.Headers[HeaderNames.CacheControl] = Value;
        base.OnActionExecuting(context);
    }
}
