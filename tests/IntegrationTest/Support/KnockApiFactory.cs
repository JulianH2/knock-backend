using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Knock.IntegrationTest.Support;

/// <summary>Host de pruebas de la API: entorno configurable, controller de prueba y, opcionalmente, una comprobación de readiness que falla.</summary>
internal sealed class KnockApiFactory(string environment = "Development", bool failingReadinessCheck = false)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseEnvironment(environment);
        builder.ConfigureTestServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(TestController).Assembly);

            if (failingReadinessCheck)
            {
                services.AddHealthChecks().AddCheck(
                    "always-failing",
                    () => HealthCheckResult.Unhealthy("synthetic failure"),
                    tags: ["ready"]);
            }
        });
    }
}
