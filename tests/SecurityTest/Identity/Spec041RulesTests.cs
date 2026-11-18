using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Knock.SecurityTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.SecurityTest.Identity;

/// <summary>Spec 041, T-07 y T-08: límite por persona, propiedad, `sub` del token, registros sin datos personales, 429, 413 y creación repetida. Solo datos ficticios.</summary>
public class Spec041RulesTests
{
    private static object Body(string name) => new { tipo = "otro", nombre = name };

    [Fact]
    public async Task The21stItem_Returns409ItemLimitReached_AndSavesNothing_CA_041_25()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();
        for (var i = 0; i < 20; i++)
        {
            using var ok = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Post, HomeAndCarHttp.Route, TokenForge.Valid(sub), Body("Elemento " + i));
            ok.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        using var response = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Post, HomeAndCarHttp.Route, TokenForge.Valid(sub), Body("Elemento 21"));

        var raw = await SecureHttp.AssertProblemAsync(response, HttpStatusCode.Conflict);
        System.Text.Json.JsonElement.Parse(raw).GetProperty("code").GetString().Should().Be("item_limit_reached");
        (await HomeAndCarHttp.CountAsync(hosts, sub)).Should().Be(20);
        using var other = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Post, HomeAndCarHttp.Route, TokenForge.Valid(), Body("Elemento ajeno"));
        other.StatusCode.Should().Be(HttpStatusCode.Created, "el tope es por persona");
    }

    [Fact]
    public async Task ConcurrentCreations_NeverExceed20_CA_041_33()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();

        var responses = await Task.WhenAll(Enumerable.Range(0, 30).Select(async i =>
        {
            using var response = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Post, HomeAndCarHttp.Route, TokenForge.Valid(sub), Body("Repetido " + i));
            return response.StatusCode;
        }));

        var created = responses.Count(s => s == HttpStatusCode.Created);
        created.Should().BeLessThanOrEqualTo(20);
        responses.Count(s => s == HttpStatusCode.Conflict).Should().Be(30 - created);
        (await HomeAndCarHttp.CountAsync(hosts, sub)).Should().Be(created, "lo aceptado es lo guardado y nunca pasa de 20");
    }

    [Fact]
    public async Task AnotherPersonsItem_IsHiddenInEveryRoute_CA_041_26()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var id = await HomeAndCarHttp.SeedAsync(hosts, TokenForge.Sub());
        var token = TokenForge.Valid();

        foreach (var (method, path, body) in new (HttpMethod, string, object?)[]
        {
            (HttpMethod.Put, $"{HomeAndCarHttp.Route}/{id}", new { nombre = "Cambio ajeno" }),
            (HttpMethod.Delete, $"{HomeAndCarHttp.Route}/{id}", null),
            (HttpMethod.Get, $"{HomeAndCarHttp.Route}/{id}/historial", null),
        })
        {
            using var response = await HomeAndCarHttp.SendAsync(hosts, method, path, token, body);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound, $"{method} {path}");
        }

        using var list = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Get, HomeAndCarHttp.Route, token);
        (await list.Content.ReadAsStringAsync()).Should().NotContain(id.ToString());
        (await HomeAndCarHttp.Items(hosts).CountDocumentsAsync(new BsonDocument("_id", id))).Should().Be(1);
    }

    [Fact]
    public async Task AForeignOwnerIdInTheBody_IsIgnored_TheTokenSubWins_CA_041_30()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();
        var victim = TokenForge.Sub();

        using var created = await HomeAndCarHttp.SendRawAsync(hosts, HttpMethod.Post, HomeAndCarHttp.Route, TokenForge.Valid(sub),
            $$"""{"tipo":"otro","nombre":"Nombre válido","usuarioId":"{{victim}}","sub":"{{victim}}"}""");

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        (await HomeAndCarHttp.CountAsync(hosts, sub)).Should().Be(1);
        (await HomeAndCarHttp.CountAsync(hosts, victim)).Should().Be(0);
        var id = (await SecureHttp.ReadJsonAsync(created)).GetProperty("id").GetString()!;
        using var edited = await HomeAndCarHttp.SendRawAsync(hosts, HttpMethod.Put, $"{HomeAndCarHttp.Route}/{id}", TokenForge.Valid(sub), $$"""{"nombre":"Otro nombre","usuarioId":"{{victim}}"}""");
        edited.StatusCode.Should().Be(HttpStatusCode.OK);
        (await HomeAndCarHttp.Items(hosts).Find(new BsonDocument("_id", ObjectId.Parse(id))).SingleAsync())["usuarioId"].AsString.Should().Be(sub);
    }

    [Fact]
    public async Task ABodyOver4KiB_Returns413_AndSavesNothing_CA_041_32()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();

        using var response = await HomeAndCarHttp.SendRawAsync(hosts, HttpMethod.Post, HomeAndCarHttp.Route, TokenForge.Valid(sub),
            "{\"tipo\":\"otro\",\"nombre\":\"" + new string('a', 5000) + "\"}");

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.RequestEntityTooLarge);
        (await HomeAndCarHttp.CountAsync(hosts, sub)).Should().Be(0);
    }

    [Fact]
    public async Task GlobalLimit_Returns429WithRetryAfter_CA_041_32()
    {
        using var factory = await SecurityIdentityFactory.CreateAsync(new Dictionary<string, string?>
        {
            ["RateLimiting:Global:PermitLimit"] = "3",
            ["RateLimiting:Global:Window"] = "00:01:00",
        });
        using var client = factory.CreateClient();
        var token = TokenForge.Valid();
        HttpStatusCode last = default;
        string? retry = null;
        for (var i = 0; i < 4; i++)
        {
            using var response = await SecureHttp.SendAsync(client, HttpMethod.Get, HomeAndCarHttp.Route, bearer: token, remoteIp: "203.0.113.90");
            last = response.StatusCode;
            retry = response.Headers.TryGetValues("Retry-After", out var values) ? values.Single() : null;
        }

        last.Should().Be(HttpStatusCode.TooManyRequests);
        int.Parse(retry!, System.Globalization.CultureInfo.InvariantCulture).Should().BeInRange(1, 60);
    }

    [Fact]
    public async Task Logs_NeverContainTheItemNameOrTheSub_CA_041_31()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();
        var secretName = "Nombre-Privado-" + Guid.NewGuid().ToString("N");

        var output = await ConsoleCapture.RunAsync(async () =>
        {
            using var created = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Post, HomeAndCarHttp.Route, TokenForge.Valid(sub), Body(secretName));
            using var bad = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Post, HomeAndCarHttp.Route, TokenForge.Valid(sub), new { tipo = "x", nombre = secretName });
            using var list = await HomeAndCarHttp.SendAsync(hosts, HttpMethod.Get, HomeAndCarHttp.Route, TokenForge.Valid(sub));
            (await bad.Content.ReadAsStringAsync()).Should().NotContain(secretName);
        });

        output.Should().NotContain(secretName).And.NotContain(sub);
    }

    [Theory]
    [InlineData("{\"tipo\":\"moto\",\"nombre\":\"Nombre válido\"}")]
    [InlineData("{\"tipo\":\"otro\",\"nombre\":\"X\"}")]
    [InlineData("{\"tipo\":\"auto\",\"nombre\":\"Nombre válido\",\"anio\":1900}")]
    [InlineData("{\"tipo\":\"instalacion\",\"nombre\":\"Nombre válido\",\"anio\":2020}")]
    public async Task InvalidTypeNameOrYear_Returns422AndIsNotSaved_CA_041_24(string json)
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();

        using var response = await HomeAndCarHttp.SendRawAsync(hosts, HttpMethod.Post, HomeAndCarHttp.Route, TokenForge.Valid(sub), json);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await HomeAndCarHttp.CountAsync(hosts, sub)).Should().Be(0);
    }
}
