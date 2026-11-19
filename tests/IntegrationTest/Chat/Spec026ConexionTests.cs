using AwesomeAssertions;
using Xunit;

namespace Knock.IntegrationTest.Chat;

/// <summary>Conexión y unión a la sala de la visita (spec 026, R-05 y R-06).</summary>
public sealed class Spec026ConexionTests
{
    [Fact]
    public async Task Se_Une_A_La_Visita_CA_026_01()
    {
        await using var scenario = await ChatScenario.StartAsync();
        var copy = await scenario.CopyAsync();
        var connection = await scenario.ConnectAsync(copy, scenario.Client);

        var result = await ChatScenario.JoinAsync(connection);

        ChatScenario.Ok(result).Should().BeTrue("el cliente de una visita confirmada entra a su sala");
        ChatScenario.Text(result, "codigo").Should().BeEmpty();
        scenario.Market.Calls.Should().Be(1, "la unión se comprueba una vez contra Mercado");
    }
}
