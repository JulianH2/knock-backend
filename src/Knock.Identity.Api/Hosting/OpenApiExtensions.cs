// jscpd:ignore-start -- EXC-015
using Knock.Identity.Api.Modules.Panel;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Knock.Identity.Api.Hosting;

internal static class OpenApiExtensions
{
    public static IServiceCollection AddKnockOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            options.AddOperationTransformer((operation, context, _) =>
            {
                var description = context.Description;
                PanelOpenApi.Describe(operation, description?.RelativePath);
                if (description?.HttpMethod == "GET"
                    && description.RelativePath == "api/v{version}/me/sesiones"
                    && operation.Responses is { } responses
                    && responses.TryGetValue("200", out var response)
                    && response is OpenApiResponse concreteResponse)
                {
                    concreteResponse.Headers ??= new Dictionary<string, IOpenApiHeader>(StringComparer.OrdinalIgnoreCase);
                    concreteResponse.Headers["Cache-Control"] = new OpenApiHeader
                    {
                        Description = "Cache policy for the personal sessions response.",
                        Schema = new OpenApiSchema
                        {
                            Type = JsonSchemaType.String,
                            Const = "private, no-store",
                        },
                    };
                }

                return Task.CompletedTask;
            });
        });
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
// jscpd:ignore-end
