using System.Net;
using System.Security.Cryptography;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Knock.IntegrationTest.Support;

/// <summary>Host de pruebas de la API: entorno configurable, controller de prueba y, opcionalmente, una comprobación de readiness que falla.</summary>
internal sealed class KnockApiFactory(
    string environment = "Development",
    bool failingReadinessCheck = false,
    string? connectionString = null,
    IReadOnlyDictionary<string, string?>? settings = null,
    bool withSigningKey = true,
    Action<IServiceCollection>? configureServices = null)
    : WebApplicationFactory<Program>
{
    public const string TestIssuer = "knock-api";
    public const string TestAudience = "knock-app";

    /// <summary>Encabezado que solo entiende el host de pruebas: fija <c>Connection.RemoteIpAddress</c> (TestServer no trae IP).</summary>
    public const string RemoteIpHeader = "X-Test-Remote-Ip";

    /// <summary>Límite alto por defecto: las suites que no prueban el limitador no deben recibir 429 (el limitador sigue activo).</summary>
    private const string _highLimit = "1000000";

    /// <summary>Clave de firma sintética de las pruebas (32 bytes aleatorios por proceso; nunca se escribe en disco).</summary>
    public static readonly byte[] TestSigningKey = RandomNumberGenerator.GetBytes(32);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseEnvironment(environment);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var values = new Dictionary<string, string?>
            {
                ["Firebase:ProjectId"] = FirebaseEmulator.ProjectId,
                ["Jwt:Issuer"] = TestIssuer,
                ["Jwt:Audience"] = TestAudience,
                ["RateLimiting:Global:PermitLimit"] = _highLimit,
                ["RateLimiting:Auth:PermitLimit"] = _highLimit,
            };
            if (withSigningKey)
            {
                values["Jwt:SigningKey"] = Convert.ToBase64String(TestSigningKey);
            }

            if (!string.Equals(environment, "Development", StringComparison.Ordinal))
            {
                // La variable del emulador es del proceso; fuera de Development la API no arranca con ella (TC-TT-API-02-14).
                // Las pruebas que no son del guardia la anulan; la prueba del guardia la vuelve a poner con `settings`.
                values[FirebaseEmulator.HostVariable] = string.Empty;
            }

            if (connectionString is not null)
            {
                values["ConnectionStrings:KnockDatabase"] = connectionString;
            }

            foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
            {
                values[key] = value;
            }

            configuration.AddInMemoryCollection(values);
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(TestController).Assembly);
            services.AddValidatorsFromAssemblyContaining<ValidatedInputValidator>();
            services.AddSingleton<IStartupFilter>(new RemoteIpStartupFilter());
            configureServices?.Invoke(services);

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

/// <summary>Pone la IP de conexión desde <see cref="KnockApiFactory.RemoteIpHeader"/> antes del resto del pipeline de la API.</summary>
internal sealed class RemoteIpStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        return app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(KnockApiFactory.RemoteIpHeader, out var value)
                    && IPAddress.TryParse(value.ToString(), out var address))
                {
                    context.Connection.RemoteIpAddress = address;
                }

                return nextMiddleware(context);
            });
            next(app);
        };
    }
}
