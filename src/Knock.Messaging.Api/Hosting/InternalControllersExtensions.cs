using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Knock.Messaging.Api.Hosting;

internal static class InternalControllersExtensions
{
    // jscpd:ignore-start -- EXC-030
    /// <summary>Los controllers de los módulos son <c>internal</c>: MVC solo descubre públicos, así que se amplía el descubrimiento al ensamblado de la API.</summary>
    public static IMvcBuilder AddInternalControllers(this IMvcBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ConfigureApplicationPartManager(manager =>
        {
            foreach (var provider in manager.FeatureProviders.Where(p => p.GetType() == typeof(ControllerFeatureProvider)).ToList())
            {
                manager.FeatureProviders.Remove(provider);
            }

            manager.FeatureProviders.Add(new InternalControllerFeatureProvider());
        });
    }
}

/// <summary>Descubre los controllers públicos (como MVC) y también los <c>internal</c> del ensamblado de la API cuyo nombre termina en <c>Controller</c>.</summary>
internal sealed class InternalControllerFeatureProvider : ControllerFeatureProvider
{
    protected override bool IsController(System.Reflection.TypeInfo typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        return base.IsController(typeInfo) || IsInternalController(typeInfo);
    }

    private static bool IsInternalController(System.Reflection.TypeInfo typeInfo) =>
        typeInfo.Assembly == typeof(InternalControllerFeatureProvider).Assembly
        && typeInfo is { IsClass: true, IsAbstract: false, IsNotPublic: true, IsGenericType: false }
        && typeof(ControllerBase).IsAssignableFrom(typeInfo)
        && !typeInfo.IsDefined(typeof(NonControllerAttribute), inherit: true)
        && typeInfo.Name.EndsWith("Controller", StringComparison.Ordinal);
}
// jscpd:ignore-end
