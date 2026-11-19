using System.Net;
using AwesomeAssertions;
using Xunit;

namespace Knock.IntegrationTest.Chat;

/// <summary>El interruptor <c>Chat:Habilitado</c> (spec 026, R-17).</summary>
public sealed class Spec026ApagadoTests
{
    [Fact]
    public async Task Chat_Apagado_CA_026_17()
    {
        await using var scenario = await ChatScenario.StartAsync();
        var copy = await scenario.CopyAsync(new Dictionary<string, string?> { ["Chat:Habilitado"] = "false" });

        var negotiate = await copy.NegotiateAsync(ChatScenario.Token(scenario.Client));

        negotiate.Should().Be(HttpStatusCode.ServiceUnavailable, "con el chat apagado el saludo responde 503");
    }
}
