using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.SecurityTest.Identity;

public class FirebaseEmulatorGuardTests
{
    private static readonly Dictionary<string, string?> _emulatorConfigured = new()
    {
        [FirebaseEmulator.HostVariable] = "127.0.0.1:9099",
    };

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void NonDevelopment_EmulatorTokensRejected_TC_TT_API_02_14(string environment)
    {
        // Fuera de Development, con la variable del emulador presente, la API no arranca: nunca acepta tokens sin firma.
        using var factory = new KnockApiFactory(environment, settings: _emulatorConfigured);

        var act = () => factory.CreateClient();

        act.Should().Throw<InvalidOperationException>().WithMessage("*emulator*");
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task NonDevelopmentWithoutEmulator_Starts_TC_TT_API_02_14(string environment)
    {
        // Control: sin la variable, el mismo entorno arranca; así el fallo de arriba se debe al guardia.
        using var factory = new KnockApiFactory(environment);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Development_WithEmulator_Starts_TC_TT_API_02_14()
    {
        using var factory = new KnockApiFactory("Development", settings: _emulatorConfigured);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
