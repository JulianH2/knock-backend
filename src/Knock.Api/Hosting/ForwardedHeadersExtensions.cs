using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Knock.Api.Hosting;

internal static class ForwardedHeadersExtensions
{
    public const string SectionName = "ForwardedHeaders";

    /// <summary>
    /// IP real detrás del proxy: solo se acepta <c>X-Forwarded-For</c> de los proxies y redes de <c>ForwardedHeaders:KnownProxies</c> y
    /// <c>ForwardedHeaders:KnownNetworks</c> (CIDR). Sin ninguno configurado NO se confía en nadie (ni siquiera en loopback): se usa la IP de la conexión.
    /// El repositorio no trae valores de producción; una persona los define al desplegar.
    /// </summary>
    public static IServiceCollection AddKnockForwardedHeaders(this IServiceCollection services)
    {
        services.AddOptions<ForwardedHeadersOptions>().Configure<IConfiguration>((options, configuration) =>
        {
            options.ForwardLimit = 1;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();

            foreach (var proxy in configuration.GetSection($"{SectionName}:KnownProxies").Get<string[]>() ?? [])
            {
                options.KnownProxies.Add(IPAddress.TryParse(proxy, out var address)
                    ? address
                    : throw new InvalidOperationException($"{SectionName}:KnownProxies has an entry that is not an IP address."));
            }

            foreach (var network in configuration.GetSection($"{SectionName}:KnownNetworks").Get<string[]>() ?? [])
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.TryParse(network, out var parsed)
                    ? parsed
                    : throw new InvalidOperationException($"{SectionName}:KnownNetworks has an entry that is not a CIDR network."));
            }

            // El middleware confía en TODOS los remitentes cuando no hay proxies ni redes: sin configuración se deshabilita el reenvío.
            options.ForwardedHeaders = options.KnownProxies.Count > 0 || options.KnownIPNetworks.Count > 0
                ? ForwardedHeaders.XForwardedFor
                : ForwardedHeaders.None;
        });
        return services;
    }
}
