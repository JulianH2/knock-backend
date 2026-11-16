using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace Knock.Market.Api.Modules.Requests.Controllers;

/// <summary>
/// <c>Cache-Control: private, no-store</c> en toda respuesta del controller (spec 020, R-18): los datos son personales y cambian al crear, y no llevan <c>ETag</c>. Se fija
/// al empezar la acción, como lo hacen los demás controllers de Mercado.
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
