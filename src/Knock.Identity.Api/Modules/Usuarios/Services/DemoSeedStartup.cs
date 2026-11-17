namespace Knock.Identity.Api.Modules.Usuarios.Services;

internal sealed class DemoSeedStartup(IServiceProvider services, IConfiguration configuration, IHostEnvironment environment) : IHostedService
{
    public static bool ShouldRun(bool enabled, string environment) =>
        enabled && !string.Equals(environment, Environments.Production, StringComparison.OrdinalIgnoreCase);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (ShouldRun(configuration.GetValue<bool>("Seed:Demo"), environment.EnvironmentName))
        {
            var seeder = services.GetRequiredService<DemoClientSeeder>();
            await seeder.SeedAsync(Path.Combine(AppContext.BaseDirectory, "db", "seeds", "demo", "identity", "usuarios.json"), cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
