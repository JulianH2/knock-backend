using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Requests;

public sealed class Spec048MyQuotesTests
{
    private const string _path = "/api/v1/tecnicos/me/cotizaciones";

    [Fact]
    public async Task Technician_ListsOnlyOwnQuotes_CA_048_12()
    {
        await using var host = await Spec020Host.StartAsync();
        await host.SeedTechnicianAsync(host.ClientSub, "activo", false, [Spec020Host.Plumbing], host.ColoniaId);
        await SeedAsync(host, host.ClientSub, 3);
        await SeedAsync(host, Guid.NewGuid().ToString("D"), 2);
        using var response = await host.SendAsync(HttpMethod.Get, _path, host.ClientSub);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Spec020Host.ReadAsync(response)).GetProperty("items").GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task MissingOrExpiredToken_IsUnauthorized_CA_048_13()
    {
        await using var host = await Spec020Host.StartAsync();
        using var anonymous = await host.SendAsync(HttpMethod.Get, _path);
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_path, UriKind.Relative));
        var expired = GatewayTestSupport.IssueToken(host.ClientSub, key: Convert.FromBase64String(MarketApiFactory.SigningKeyBase64), lifetime: TimeSpan.FromMinutes(-5));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", expired);
        using var invalid = await host.Client.SendAsync(request);
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        invalid.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task StateFilter_ReturnsOnlyOwnInDescendingOrder_CA_048_12()
    {
        await using var host = await Spec020Host.StartAsync();
        await host.SeedTechnicianAsync(host.ClientSub, "activo", false, [Spec020Host.Plumbing], host.ColoniaId);
        await SeedAsync(host, host.ClientSub, 4);
        await host.Database.GetCollection<BsonDocument>("cotizaciones").UpdateOneAsync(
            Builders<BsonDocument>.Filter.Eq("tecnicoUsuarioId", host.ClientSub),
            Builders<BsonDocument>.Update.Set("estado", "aceptada"));
        using var response = await host.SendAsync(HttpMethod.Get, _path + "?estado=enviada", host.ClientSub);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = (await Spec020Host.ReadAsync(response)).GetProperty("items").EnumerateArray().ToArray();
        items.Should().HaveCount(3);
        items.Select(x => x.GetProperty("estado").GetString()).Should().OnlyContain(x => x == "enviada");
        items.Select(x => x.GetProperty("creadoEn").GetDateTime()).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task NonTechnician_IsForbidden_CA_048_14()
    {
        await using var host = await Spec020Host.StartAsync();
        using var response = await host.SendAsync(HttpMethod.Get, _path, host.ClientSub);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("?estado=invalid", "estado")]
    [InlineData("?limite=51", "limite")]
    [InlineData("?cursor=invalid", "cursor")]
    public async Task InvalidQuery_ReturnsSanitized422_CA_048_15(string query, string field)
    {
        await using var host = await Spec020Host.StartAsync();
        await host.SeedTechnicianAsync(host.ClientSub, "activo", false, [Spec020Host.Plumbing], host.ColoniaId);
        using var response = await host.SendAsync(HttpMethod.Get, _path + query, host.ClientSub);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await Spec020Host.ReadAsync(response);
        body.GetProperty("errors").GetProperty(field).GetArrayLength().Should().BeGreaterThan(0);
        body.ToString().Should().NotContain("invalid");
    }

    [Fact]
    public async Task DefaultPage_IsTwenty_WithNextCursor_CA_048_16()
    {
        await using var host = await Spec020Host.StartAsync();
        await host.SeedTechnicianAsync(host.ClientSub, "activo", false, [Spec020Host.Plumbing], host.ColoniaId);
        await SeedAsync(host, host.ClientSub, 25);
        using var response = await host.SendAsync(HttpMethod.Get, _path, host.ClientSub);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await Spec020Host.ReadAsync(response);
        page.GetProperty("items").GetArrayLength().Should().Be(20);
        var cursor = page.GetProperty("nextCursor").GetString();
        cursor.Should().NotBeNullOrEmpty();
        using var next = await host.SendAsync(HttpMethod.Get, _path + "?cursor=" + Uri.EscapeDataString(cursor), host.ClientSub);
        next.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Spec020Host.ReadAsync(next)).GetProperty("items").GetArrayLength().Should().Be(5);
        using var tooLarge = await host.SendAsync(HttpMethod.Get, _path + "?limite=51", host.ClientSub);
        tooLarge.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private static async Task SeedAsync(Spec020Host host, string sub, int count)
    {
        var now = DateTime.UtcNow;
        var documents = Enumerable.Range(0, count).Select(i => new BsonDocument
        {
            ["_id"] = ObjectId.GenerateNewId(),
            ["solicitudId"] = ObjectId.GenerateNewId(),
            ["tecnicoId"] = ObjectId.GenerateNewId(),
            ["tecnicoUsuarioId"] = sub,
            ["estado"] = "enviada",
            ["tipoPrecio"] = "fijo",
            ["precio"] = 100,
            ["tope"] = 110,
            ["requiereAprobacion"] = false,
            ["dia"] = "2026-12-01",
            ["hora"] = "12:00",
            ["creadoEn"] = now.AddMinutes(-i),
        });
        await host.Database.GetCollection<BsonDocument>("cotizaciones").InsertManyAsync(documents);
    }
}
