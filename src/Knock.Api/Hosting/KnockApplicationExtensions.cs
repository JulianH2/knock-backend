using Serilog;

namespace Knock.Api.Hosting;

internal static class KnockApplicationExtensions
{
    public static WebApplicationBuilder AddKnockApi(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddKnockLogging(builder.Configuration)
            .AddControllers()
            .AddInternalControllers();
        builder.Services
            .AddKnockProblemDetails()
            .AddKnockApiVersioning()
            .AddKnockAuthentication()
            .AddKnockOpenApi()
            .AddKnockHealth()
            .AddKnockModules();
        return builder;
    }

    public static WebApplication UseKnockApi(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseSerilogRequestLogging();
        app.UseMiddleware<RequestBodyLimitMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapKnockHealth();
        app.MapKnockOpenApi(app.Environment);
        app.MapControllers();
        return app;
    }
}
