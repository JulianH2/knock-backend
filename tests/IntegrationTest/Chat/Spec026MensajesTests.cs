using AwesomeAssertions;
using Xunit;

namespace Knock.IntegrationTest.Chat;

/// <summary>Retransmisión de mensajes a la sala de la visita (spec 026, R-10 a R-12 y R-14).</summary>
public sealed class Spec026MensajesTests
{
    private const string _bell = "\u0007";

    [Fact]
    public async Task Entrega_Al_Otro_Participante_CA_026_04()
    {
        await using var scenario = await ChatScenario.StartAsync();
        var copy = await scenario.CopyAsync();
        var client = await scenario.ConnectAsync(copy, scenario.Client);
        var technician = await scenario.ConnectAsync(copy, scenario.Technician);
        await ChatScenario.JoinAsync(client);
        await ChatScenario.JoinAsync(technician);
        var inbox = ChatScenario.Listen(technician);

        var sent = await ChatScenario.SendAsync(client, "  Buenas, llego a las diez  ");
        var received = await ChatScenario.NextAsync(inbox);

        ChatScenario.Ok(sent).Should().BeTrue();
        ChatScenario.Text(received, "visitaId").Should().Be(ChatFactory.Visit);
        ChatScenario.Text(received, "texto").Should().Be("Buenas, llego a las diez", "el texto se recorta");
        Guid.TryParse(ChatScenario.Text(received, "mensajeId"), out var id).Should().BeTrue();
        id.ToString("N")[12].Should().Be('7', "el identificador es un UUID v7 generado en el servidor");
        received.TryGetProperty("enviadoEn", out _).Should().BeTrue();
        received.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(["visitaId", "mensajeId", "texto", "enviadoEn"], "no lleva sub, nombre ni rol de quien escribe");
    }

    [Theory]
    [InlineData(1001)]
    [InlineData(5000)]
    public async Task Rechaza_Texto_Invalido_CA_026_08(int length)
    {
        await using var scenario = await ChatScenario.StartAsync();
        var copy = await scenario.CopyAsync();
        var client = await scenario.ConnectAsync(copy, scenario.Client);
        var technician = await scenario.ConnectAsync(copy, scenario.Technician);
        await ChatScenario.JoinAsync(client);
        await ChatScenario.JoinAsync(technician);
        var inbox = ChatScenario.Listen(technician);

        var tooLong = await ChatScenario.SendAsync(client, new string('a', length));
        var empty = await ChatScenario.SendAsync(client, "   ");
        var control = await ChatScenario.SendAsync(client, "hola" + _bell + "mundo");

        foreach (var result in new[] { tooLong, empty, control })
        {
            ChatScenario.Ok(result).Should().BeFalse();
            ChatScenario.Text(result, "codigo").Should().Be("invalid_message");
        }

        (await ChatScenario.NothingArrivesAsync(inbox, TimeSpan.FromSeconds(1))).Should().BeTrue("nadie recibe un mensaje inválido");
    }

    [Fact]
    public async Task Dos_Dispositivos_CA_026_16()
    {
        await using var scenario = await ChatScenario.StartAsync();
        var copy = await scenario.CopyAsync();
        var phone = await scenario.ConnectAsync(copy, scenario.Client);
        var tablet = await scenario.ConnectAsync(copy, scenario.Client);
        var technician = await scenario.ConnectAsync(copy, scenario.Technician);
        await ChatScenario.JoinAsync(phone);
        await ChatScenario.JoinAsync(tablet);
        await ChatScenario.JoinAsync(technician);
        var phoneInbox = ChatScenario.Listen(phone);
        var tabletInbox = ChatScenario.Listen(tablet);

        await ChatScenario.SendAsync(technician, "Ya voy para allá");

        ChatScenario.Text(await ChatScenario.NextAsync(phoneInbox), "texto").Should().Be("Ya voy para allá");
        ChatScenario.Text(await ChatScenario.NextAsync(tabletInbox), "texto").Should().Be("Ya voy para allá");
    }
}
