using Microsoft.Extensions.Options;
using Serilog;

namespace Knock.Market.Api.Hosting;

internal static class KnockApplicationExtensions
{
    public static WebApplicationBuilder AddKnockApi(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddKnockLogging(builder.Configuration)
            .AddControllers(options => options.Filters.Add<ValidationFilter>())
            .AddInternalControllers();
        builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, MarketJsonSerializerContext.Default));
        builder.AddKnockSecurity();
        builder.Services
            .AddKnockProblemDetails()
            .AddKnockValidation()
            .AddKnockForwardedHeaders()
            .AddKnockRateLimiting()
            .AddKnockApiVersioning()
            .AddKnockAuthentication()
            .AddKnockOpenApi()
            .AddKnockHealth()
            .AddKnockMongo()
            .AddKnockModules(builder.Configuration, builder.Environment.IsDevelopment())
            .AddKnockObservability(builder.Configuration, builder.Environment);
        return builder;
    }

    public static WebApplication UseKnockApi(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseExceptionHandler();
        // Una respuesta de error sin cuerpo (404 de una ruta inexistente, 405…) sale como ProblemDetails igual que las excepciones (spec 002, R-03).
        app.UseStatusCodePages();
        app.UseSerilogRequestLogging(LoggingExtensions.ConfigureRequestLogging);
        var securedTransport = !app.Environment.IsDevelopment();
        if (securedTransport)
        {
            app.UseHsts();
        }

        if (securedTransport && app.Services.GetRequiredService<IOptions<SecuritySettings>>().Value.Https.RedirectPort is not null)
        {
            app.UseHttpsRedirection();
        }

        app.UseMiddleware<CorsMiddleware>();
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.Equals("/api/v1/zonas/lista-espera", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Headers.CacheControl = "private, no-store";
            }

            await next(context);
        });
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseMiddleware<RequestBodyLimitMiddleware>();
        app.UseMiddleware<JsonCompressionMiddleware>();
        app.UseAuthorization();
        app.MapKnockHealth();
        app.MapKnockOpenApi(app.Environment);
        app.MapControllers();
        return app;
    }
}
