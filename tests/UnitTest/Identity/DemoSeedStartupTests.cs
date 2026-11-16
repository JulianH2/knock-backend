using Knock.Identity.Api.Modules.Usuarios.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Knock.UnitTest.Identity;

public sealed class DemoSeedStartupTests
{
    [Theory]
    [InlineData(true, "Production", false)]
    [InlineData(false, "Development", false)]
    [InlineData(true, "Development", true)]
    public void EnvironmentGuard_CA_038_07(bool enabled, string environment, bool expected)
    {
        Assert.Equal(expected, DemoSeedStartup.ShouldRun(enabled, environment));
    }

    [Fact]
    public async Task ProductionWithFlagDoesNotResolveSeeder_CA_038_07()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Seed:Demo"] = "true" }).Build();
        var host = new FakeEnvironment { EnvironmentName = Environments.Production };
        var startup = new DemoSeedStartup(services, config, host);
        await startup.StartAsync(CancellationToken.None);
        Assert.Null(services.GetService<DemoClientSeeder>());
    }

    private sealed class FakeEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
