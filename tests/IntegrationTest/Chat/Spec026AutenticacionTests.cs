using AwesomeAssertions;
using Knock.Contracts.Sessions;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit;

namespace Knock.IntegrationTest.Chat;

/// <summary>El saludo del hub y la vida de la conexión según el token (spec 026, R-02 a R-04).</summary>
public sealed class Spec026AutenticacionTests
{
    [Fact]
    public async Task Rechaza_Sin_Token_O_Cancelado_CA_026_02()
    {
        await using var scenario = await ChatScenario.StartAsync();
        var copy = await scenario.CopyAsync();
        var sid = Guid.NewGuid().ToString("N")[..24];
        await copy.Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase()
            .StringSetAsync(SessionRevocationKey.Prefix + sid, "1", TimeSpan.FromMinutes(15));

        var withoutToken = await copy.NegotiateAsync(null);
        var expired = await copy.NegotiateAsync(ChatScenario.Token(scenario.Client, lifetime: TimeSpan.FromMinutes(-5)));
        var cancelled = await copy.NegotiateAsync(ChatScenario.Token(scenario.Client, sid));
        var valid = await copy.NegotiateAsync(ChatScenario.Token(scenario.Client));

        withoutToken.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        expired.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        cancelled.Should().Be(System.Net.HttpStatusCode.Unauthorized, "la sesión cerrada con logout no abre canal");
        valid.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task Cierra_Al_Vencer_El_Token_CA_026_03()
    {
        await using var scenario = await ChatScenario.StartAsync();
        var copy = await scenario.CopyAsync();
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var connection = await scenario.ConnectAsync(copy, scenario.Client, lifetime: TimeSpan.FromSeconds(3));
        connection.Closed += _ =>
        {
            closed.TrySetResult();
            return Task.CompletedTask;
        };
        var joined = await ChatScenario.JoinAsync(connection);

        var closedInTime = true;
        try
        {
            await closed.Task.WaitAsync(TimeSpan.FromSeconds(20));
        }
        catch (TimeoutException)
        {
            closedInTime = false;
        }

        ChatScenario.Ok(joined).Should().BeTrue("antes de vencer el token la conexión sirve");
        closedInTime.Should().BeTrue("el servicio cierra la conexión cuando vence el token");
    }
}
