using Knock.Contracts.Correlation;
using Knock.Gateway.Hosting;
using Knock.Hosting.ForwardedHeaders;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// BUG-003: Kestrel agrega «Server: Kestrel» al final de la respuesta, después de cualquier middleware; solo se apaga aquí.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// La tabla de rutas y las direcciones de los servicios viven en la configuración (sección ReverseProxy),
// para cambiarlas por entorno sin recompilar (spec 001, R-02). Cada ruta declara su política: `default` exige sesión
// y `anonymous` deja pasar (specs 009 y 015).
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services
    .AddKnockGatewayProblemDetails()
    .AddKnockGatewayHsts()
    .AddKnockForwardedHeaders()
    .AddKnockGatewayCors(builder.Configuration)
    .AddKnockData()
    .AddKnockGatewayRateLimiting()
    .AddKnockGatewayAuthentication()
    .AddKnockGatewayPanel();
// Spec 011: Serilog estructurado y OpenTelemetry con redacción; Azure Monitor solo si hay APPLICATIONINSIGHTS_CONNECTION_STRING.
builder.Services.AddKnockLogging(builder.Configuration);
builder.Services.AddKnockObservability(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseKnockCorrelationId();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseExceptionHandler();
app.UseSerilogRequestLogging(LoggingExtensions.ConfigureRequestLogging);
app.UseRouting();
app.UseCors(CorsExtensions.PolicyName);
app.UseMiddleware<IpRateLimitMiddleware>();
app.UseMiddleware<HubQueryTokenMiddleware>();
app.UseAuthentication();
app.UseMiddleware<PanelTokenMiddleware>();
app.UseMiddleware<UserRateLimitMiddleware>();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "gateway" })).AllowAnonymous();
app.MapHealthChecks(HealthExtensions.ReadyPath, new HealthCheckOptions { Predicate = check => check.Tags.Contains(HealthExtensions.ReadyTag) }).AllowAnonymous();
app.MapReverseProxy();

await app.RunAsync();

/// <summary>Punto de entrada accesible para las pruebas de integración.</summary>
internal partial class Program
{
    protected Program()
    {
    }
}
