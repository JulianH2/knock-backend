using System.Net;
using AwesomeAssertions;
using Xunit;

namespace Knock.IntegrationTest.Chat;

/// <summary>Falla cerrada cuando Redis no responde (spec 026, R-16).</summary>
public sealed class Spec026SaludTests
{
    private const string _deadRedis = "127.0.0.1:6390,password=sin-redis-en-este-puerto,abortConnect=false,connectTimeout=300,syncTimeout=300";

    [Fact]
    public async Task Redis_Caido_Falla_Cerrado_CA_026_14()
    {
        await using var scenario = await ChatScenario.StartAsync();
        var copy = await scenario.CopyAsync(redisConnectionString: _deadRedis);
        using var client = copy.CreateClient();

        using var ready = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));
        var negotiate = await copy.NegotiateAsync(ChatScenario.Token(scenario.Client));

        ready.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        negotiate.Should().Be(HttpStatusCode.ServiceUnavailable, "sin Redis no hay puente ni límite: no se acepta la conexión");
    }
}
