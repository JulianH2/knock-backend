using Microsoft.Extensions.Options;
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
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapKnockHealth();
        app.MapKnockOpenApi(app.Environment);
        app.MapControllers();
        return app;
    }
}
