using AwesomeAssertions;
using Xunit;

namespace Knock.IntegrationTest.Chat;

/// <summary>El puente de Redis entre copias del servicio (spec 026, R-15).</summary>
public sealed class Spec026PuenteTests
{
    [Fact]
    public async Task Entrega_Entre_Dos_Copias_CA_026_07()
    {
        await using var scenario = await ChatScenario.StartAsync();
        var copyA = await scenario.CopyAsync();
        var copyB = await scenario.CopyAsync();
        var client = await scenario.ConnectAsync(copyA, scenario.Client);
        var technician = await scenario.ConnectAsync(copyB, scenario.Technician);
        await ChatScenario.JoinAsync(client);
        await ChatScenario.JoinAsync(technician);
        var inbox = ChatScenario.Listen(technician);

        var sent = await ChatScenario.SendAsync(client, "Mensaje que cruza de una copia a otra");
        var received = await ChatScenario.NextAsync(inbox);

        ChatScenario.Ok(sent).Should().BeTrue();
        ChatScenario.Text(received, "texto").Should().Be("Mensaje que cruza de una copia a otra", "sin el puente de Redis el mensaje se perdería en silencio");
    }
}
