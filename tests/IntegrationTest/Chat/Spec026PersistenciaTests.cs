using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit;

namespace Knock.IntegrationTest.Chat;

/// <summary>Los mensajes no se guardan en ninguna parte (spec 026, R-13).</summary>
public sealed class Spec026PersistenciaTests
{
    [Fact]
    public async Task No_Guarda_Mensajes_CA_026_12()
    {
        await using var scenario = await ChatScenario.StartAsync();
        var copy = await scenario.CopyAsync();
        var client = await scenario.ConnectAsync(copy, scenario.Client);
        var technician = await scenario.ConnectAsync(copy, scenario.Technician);
        await ChatScenario.JoinAsync(client);
        await ChatScenario.JoinAsync(technician);
        const string text = "texto-que-no-debe-quedar-guardado-12345";

        var results = new List<bool>();
        for (var i = 0; i < 3; i++)
        {
            results.Add(ChatScenario.Ok(await ChatScenario.SendAsync(client, text)));
        }

        var redis = copy.Services.GetRequiredService<IConnectionMultiplexer>();
        var database = redis.GetDatabase();
        var keys = new List<RedisKey>();
        foreach (var endpoint in redis.GetEndPoints())
        {
            await foreach (var key in redis.GetServer(endpoint).KeysAsync(pattern: "chat:*"))
            {
                keys.Add(key);
            }
        }

        results.Should().AllSatisfy(ok => ok.Should().BeTrue("los envíos funcionan aunque MongoDB no responda: no hay ninguna escritura en la base"));
        keys.Should().NotBeEmpty().And.OnlyContain(key => ((string?)key)!.StartsWith("chat:limite:", StringComparison.Ordinal) || ((string?)key)!.StartsWith("chat:presencia:", StringComparison.Ordinal));
        foreach (var key in keys)
        {
            (await database.KeyTimeToLiveAsync(key)).Should().NotBeNull("toda llave del chat vence");
            ((string?)await database.StringGetAsync(key) ?? string.Empty).Should().NotContain(text);
        }
    }
}
