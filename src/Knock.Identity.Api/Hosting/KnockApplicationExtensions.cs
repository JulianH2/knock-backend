// jscpd:ignore-start -- EXC-015
using Microsoft.Extensions.Options;
using Serilog;

namespace Knock.Identity.Api.Hosting;

internal static class KnockApplicationExtensions
{
    public static WebApplicationBuilder AddKnockApi(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddKnockLogging(builder.Configuration)
            .AddControllers(options => options.Filters.Add<ValidationFilter>())
            .AddInternalControllers();
        builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, IdentityJsonSerializerContext.Default));
        builder.AddKnockSecurity();
        builder.Services
            .AddKnockProblemDetails()
            .AddKnockValidation()
            .AddKnockForwardedHeaders()
            .AddKnockRateLimiting()
            .AddKnockApiVersioning()
            .AddKnockAuthentication()
            .AddKnockOpenApi()
            .AddKnockData()
            .AddKnockModules()
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
        app.UseRateLimiter();
        app.UseMiddleware<RequestBodyLimitMiddleware>();
        app.UseMiddleware<JsonCompressionMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        // /health: vida (spec 001, igual que el gateway y Messaging); /health/ready: dependencias de datos (spec 003).
        app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "identidad" }));
        app.MapKnockReadiness();
        app.MapKnockOpenApi(app.Environment);
        app.MapControllers();
        return app;
    }
}
// jscpd:ignore-end
