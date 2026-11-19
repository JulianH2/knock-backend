using AwesomeAssertions;
using Xunit;

namespace Knock.IntegrationTest.Chat;

/// <summary>El límite de mensajes por minuto por persona, compartido entre copias por Redis (spec 026, R-14).</summary>
public sealed class Spec026LimiteTests
{
    [Fact]
    public async Task Corta_Al_Mensaje_Sesenta_Y_Uno_CA_026_09()
    {
        await using var scenario = await ChatScenario.StartAsync();
        var copy = await scenario.CopyAsync();
        var client = await scenario.ConnectAsync(copy, scenario.Client);
        var technician = await scenario.ConnectAsync(copy, scenario.Technician);
        await ChatScenario.JoinAsync(client);
        await ChatScenario.JoinAsync(technician);
        var inbox = ChatScenario.Listen(technician);

        var accepted = 0;
        for (var i = 0; i < 30; i++)
        {
            accepted += ChatScenario.Ok(await ChatScenario.SendAsync(client, $"mensaje {i}")) ? 1 : 0;
        }

        var limited = await ChatScenario.SendAsync(client, "el que sobra");

        accepted.Should().Be(30, "caben 30 mensajes por minuto");
        ChatScenario.Ok(limited).Should().BeFalse();
        ChatScenario.Text(limited, "codigo").Should().Be("message_limit");
        limited.GetProperty("reintentarEnSegundos").GetInt32().Should().BeInRange(1, 60, "dice cuántos segundos faltan");
        var delivered = 0;
        while (!await ChatScenario.NothingArrivesAsync(inbox, TimeSpan.FromMilliseconds(500)))
        {
            delivered++;
        }

        delivered.Should().Be(30, "el mensaje que pasó el límite no se retransmite");
    }

    [Fact]
    public async Task Limite_Compartido_Entre_Copias_CA_026_10()
    {
        await using var scenario = await ChatScenario.StartAsync();
        var overrides = new Dictionary<string, string?> { ["Chat:MensajesPorMinuto"] = "6" };
        var copyA = await scenario.CopyAsync(overrides);
        var copyB = await scenario.CopyAsync(overrides);
        var onA = await scenario.ConnectAsync(copyA, scenario.Client);
        var onB = await scenario.ConnectAsync(copyB, scenario.Client);
        await ChatScenario.JoinAsync(onA);
        await ChatScenario.JoinAsync(onB);

        var results = new List<bool>();
        for (var i = 0; i < 3; i++)
        {
            results.Add(ChatScenario.Ok(await ChatScenario.SendAsync(onA, $"desde A {i}")));
            results.Add(ChatScenario.Ok(await ChatScenario.SendAsync(onB, $"desde B {i}")));
        }

        var extra = await ChatScenario.SendAsync(onA, "uno de más");

        results.Should().AllSatisfy(ok => ok.Should().BeTrue());
        ChatScenario.Text(extra, "codigo").Should().Be("message_limit", "el total sigue siendo el del límite aunque se reparta entre dos copias");
    }
}
