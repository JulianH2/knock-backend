using Scalar.AspNetCore;

namespace Knock.Api.Hosting;

internal static class OpenApiExtensions
{
    public static IServiceCollection AddKnockOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi("v1");
        return services;
    }

    /// <summary>OpenAPI (<c>/openapi/v1.json</c>) y Scalar (<c>/scalar</c>) solo en Development.</summary>
    public static IEndpointRouteBuilder MapKnockOpenApi(this IEndpointRouteBuilder endpoints, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        if (environment.IsDevelopment())
        {
            endpoints.MapOpenApi();
            endpoints.MapScalarApiReference();
        }

        return endpoints;
    }
}
