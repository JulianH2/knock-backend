using Microsoft.AspNetCore.Mvc.Filters;

namespace Knock.Identity.Api.Modules.Addresses.Controllers;

/// <summary>
/// Las respuestas con direcciones (y sus errores de forma) llevan <c>Cache-Control: private, no-store</c> (spec 040, R-13). Es un filtro de recurso para que también
/// salga en el 400 y el 422 que se responden antes de ejecutar la acción.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal sealed class PrivateNoStoreAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.HttpContext.Response.Headers.CacheControl = "private, no-store";
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
