namespace Knock.Market.Api.Modules.Requests.Services;

internal sealed class DemoTechnicianStartup(IServiceProvider services, IConfiguration configuration, IHostEnvironment environment) : IHostedService
{
    public static bool ShouldRun(bool enabled, string environment) => enabled && !string.Equals(environment, Environments.Production, StringComparison.OrdinalIgnoreCase);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (ShouldRun(configuration.GetValue<bool>("Seed:Demo"), environment.EnvironmentName))
        {
            using var scope = services.CreateScope();
            var market = Path.Combine(AppContext.BaseDirectory, "db", "seeds", "demo", "market");
            await scope.ServiceProvider.GetRequiredService<DemoTechnicianSeeder>().SeedAsync(Path.Combine(market, "tecnicos.json"), cancellationToken);
            await scope.ServiceProvider.GetRequiredService<DemoRequestSeeder>().SeedAsync(Path.Combine(market, "solicitudes.json"), Path.Combine(market, "cotizaciones.json"), cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
