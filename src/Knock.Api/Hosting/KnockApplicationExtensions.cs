using Serilog;

namespace Knock.Api.Hosting;

internal static class KnockApplicationExtensions
{
    public static WebApplicationBuilder AddKnockApi(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddKnockLogging(builder.Configuration)
            .AddControllers(options => options.Filters.Add<ValidationFilter>())
            .AddInternalControllers();
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
            .AddKnockModules();
        return builder;
    }

    public static WebApplication UseKnockApi(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler();
        app.UseSerilogRequestLogging(LoggingExtensions.ConfigureRequestLogging);
        app.UseRateLimiter();
        app.UseMiddleware<RequestBodyLimitMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapKnockHealth();
        app.MapKnockOpenApi(app.Environment);
        app.MapControllers();
        return app;
    }
}
