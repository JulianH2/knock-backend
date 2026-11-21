// jscpd:ignore-start -- EXC-016
namespace Knock.Gateway.Hosting;

/// <summary>CORS del gateway (spec 009, R-03): solo los orígenes de <c>Security:Cors:AllowedOrigins</c>; sin ninguno configurado no se permite ninguno.</summary>
internal static class CorsExtensions
{
    public const string PolicyName = "knock";

    public static IServiceCollection AddKnockGatewayCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Security:Cors:AllowedOrigins").Get<string[]>() ?? [];
        foreach (var origin in origins)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.AbsolutePath != "/" || origin[^1] == '/' || uri.Scheme is not ("http" or "https"))
            {
                throw new InvalidOperationException("Security:Cors:AllowedOrigins has an entry that is not an origin (scheme://host[:port]).");
            }
        }

        services.AddCors(options => options.AddPolicy(PolicyName, policy => policy
            .WithOrigins(origins)
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
            .WithHeaders("Authorization", "Content-Type", "X-Correlation-Id")
            .WithExposedHeaders("X-Correlation-Id", "Retry-After")
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10))));
        return services;
    }
}
// jscpd:ignore-end
