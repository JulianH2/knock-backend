using AwesomeAssertions;
using Xunit;

namespace Knock.IntegrationTest.Chat;

/// <summary>Quién puede entrar a la sala y enviar (spec 026, R-06, R-07 y R-10).</summary>
public sealed class Spec026AutorizacionTests
{
    [Fact]
    public async Task Ajeno_No_Entra_CA_026_05()
    {
        await using var scenario = await ChatScenario.StartAsync();
        var copy = await scenario.CopyAsync();
        var participant = await scenario.ConnectAsync(copy, scenario.Client);
        var outsider = await scenario.ConnectAsync(copy, Guid.NewGuid().ToString("D"));
        await ChatScenario.JoinAsync(participant);
        var inbox = ChatScenario.Listen(outsider);

        var joined = await ChatScenario.JoinAsync(outsider);
        await ChatScenario.SendAsync(participant, "hola");

        ChatScenario.Ok(joined).Should().BeFalse();
        ChatScenario.Text(joined, "codigo").Should().Be("unauthorized");
        (await ChatScenario.NothingArrivesAsync(inbox, TimeSpan.FromSeconds(1))).Should().BeTrue("quien no es de la visita no recibe nada de la sala");
    }

    [Theory]
    [InlineData("cancelada")]
    [InlineData("terminada")]
    public async Task Visita_Cerrada_No_Entra_CA_026_06(string state)
    {
        await using var scenario = await ChatScenario.StartAsync();
        scenario.State = state;
        var copy = await scenario.CopyAsync();
        var connection = await scenario.ConnectAsync(copy, scenario.Technician);

        var result = await ChatScenario.JoinAsync(connection);

        ChatScenario.Ok(result).Should().BeFalse();
        ChatScenario.Text(result, "codigo").Should().Be("conversation_unavailable");
    }

    [Fact]
    public async Task Enviar_Sin_Unirse_CA_026_11()
    {
        await using var scenario = await ChatScenario.StartAsync();
        var copy = await scenario.CopyAsync();
        var sender = await scenario.ConnectAsync(copy, scenario.Client);
        var listener = await scenario.ConnectAsync(copy, scenario.Technician);
        await ChatScenario.JoinAsync(listener);
        var inbox = ChatScenario.Listen(listener);

        var result = await ChatScenario.SendAsync(sender, "hola sin unirme");

        ChatScenario.Ok(result).Should().BeFalse();
        ChatScenario.Text(result, "codigo").Should().Be("not_joined");
        (await ChatScenario.NothingArrivesAsync(inbox, TimeSpan.FromSeconds(1))).Should().BeTrue();
        scenario.Market.Calls.Should().Be(1, "enviar no llama a Mercado: solo la unión del técnico lo llamó (R-10)");
    }

    [Theory]
    [InlineData(503)]
    [InlineData(500)]
    public async Task Mercado_No_Responde_CA_026_15(int status)
    {
        await using var scenario = await ChatScenario.StartAsync();
        scenario.Market.Answer = (_, _) => Task.FromResult(new VisitAnswer(status));
        var copy = await scenario.CopyAsync();
        var connection = await scenario.ConnectAsync(copy, scenario.Client);

        var result = await ChatScenario.JoinAsync(connection);

        ChatScenario.Ok(result).Should().BeFalse();
        ChatScenario.Text(result, "codigo").Should().Be("unavailable", "si no se pudo comprobar, nunca se asume pertenencia");
    }

    [Fact]
    public async Task Mercado_Que_Tarda_Demasiado_No_Da_Pertenencia_CA_026_15()
    {
        await using var scenario = await ChatScenario.StartAsync();
        scenario.Market.Answer = async (_, _) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
            return new VisitAnswer(200, "cliente", "confirmada");
        };
        var copy = await scenario.CopyAsync(new Dictionary<string, string?> { ["Services:Market:TiempoLimiteSegundos"] = "1" });
        var connection = await scenario.ConnectAsync(copy, scenario.Client);

        var result = await ChatScenario.JoinAsync(connection);

        ChatScenario.Text(result, "codigo").Should().Be("unavailable");
    }
}
