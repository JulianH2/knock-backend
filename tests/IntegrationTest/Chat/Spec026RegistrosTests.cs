using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.Chat;

/// <summary>Los registros del chat solo llevan conteos y códigos (spec 026, R-19).</summary>
public sealed class Spec026RegistrosTests
{
    [Fact]
    public async Task Sin_Datos_Sensibles_CA_026_13()
    {
        var sub = Guid.NewGuid().ToString("D");
        var sid = Guid.NewGuid().ToString("N")[..24];
        const string text = "mensaje-privado-que-no-debe-salir-en-los-registros";
        string token = string.Empty;

        var output = await ConsoleCapture.RunAsync(async () =>
        {
            await using var scenario = await ChatScenario.StartAsync();
            var copy = await scenario.CopyAsync();
            scenario.Market.Answer = (_, _) => Task.FromResult(new VisitAnswer(200, "cliente", "confirmada"));
            token = ChatScenario.Token(sub, sid);
            var connection = copy.Connect(token);
            await connection.StartAsync();
            await ChatScenario.JoinAsync(connection);
            await ChatScenario.SendAsync(connection, text);
            await connection.DisposeAsync();
        });

        output.Should().NotBeEmpty("el servicio sí registra lo que hace, solo conteos y códigos");
        output.Should().NotContain(text, "nunca se registra el texto de un mensaje");
        output.Should().NotContain(token, "ni el token");
        output.Should().NotContain(sub, "ni el sub");
        output.Should().NotContain(sid, "ni el sid");
        output.Should().NotContain(ChatFactory.Visit, "ni el identificador de la visita");
        output.Should().NotContain("access_token", "ni la consulta con el token");
    }
}
