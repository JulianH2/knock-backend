using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Requests;

internal static class WaitlistHttp
{
    public static readonly Uri Route = new("/api/v1/zonas/lista-espera", UriKind.Relative);

    public static async Task<(MarketApiFactory Factory, IMongoCollection<BsonDocument> Collection)> CreateAsync()
    {
        var factory = await MarketApiFactory.CreateAsync();
        var collection = factory.Database.GetCollection<BsonDocument>("listas_espera");
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("usuarioId").Ascending("cp"), new CreateIndexOptions { Unique = true }));
        return (factory, collection);
    }

    public static HttpClient SignedIn(MarketApiFactory factory, string? subject = null)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GatewayTestSupport.IssueToken(
            sub: subject ?? Guid.NewGuid().ToString("D"), key: Convert.FromBase64String(MarketApiFactory.SigningKeyBase64)));
        return client;
    }
}

public sealed class Spec023ListaEsperaTests
{
    [Fact]
    public async Task Registra_Lista_De_Espera_CA_023_05()
    {
        var (factory, collection) = await WaitlistHttp.CreateAsync();
        using var host = factory;
        var subject = Guid.NewGuid().ToString("D");
        using var client = WaitlistHttp.SignedIn(factory, subject);
        using var response = await client.PostAsJsonAsync(WaitlistHttp.Route, new { cp = "99999", municipio = "Tula" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.CacheControl!.Private.Should().BeTrue();
        response.Headers.CacheControl.NoStore.Should().BeTrue();
        var document = await collection.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        document["usuarioId"].AsString.Should().Be(subject);
        document["cp"].AsString.Should().Be("99999");
        document["municipio"].AsString.Should().Be("Tula");
        document.Names.Should().BeEquivalentTo("_id", "usuarioId", "cp", "municipio", "creadoEn");
    }

    [Fact]
    public async Task Idempotente_Por_Persona_Y_Codigo_CA_023_06()
    {
        var (factory, collection) = await WaitlistHttp.CreateAsync();
        using var host = factory;
        using var client = WaitlistHttp.SignedIn(factory);
        using var first = await client.PostAsJsonAsync(WaitlistHttp.Route, new { cp = "91000", municipio = "Tula" });
        using var second = await client.PostAsJsonAsync(WaitlistHttp.Route, new { cp = "91000", municipio = "Otro" });
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        second.Headers.CacheControl!.Private.Should().BeTrue();
        second.Headers.CacheControl.NoStore.Should().BeTrue();
        var a = await first.Content.ReadFromJsonAsync<JsonElement>();
        var b = await second.Content.ReadFromJsonAsync<JsonElement>();
        b.GetProperty("yaRegistrado").GetBoolean().Should().BeTrue();
        b.GetProperty("id").GetString().Should().Be(a.GetProperty("id").GetString());
        var stored = await collection.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        stored["municipio"].AsString.Should().Be("Tula");
    }

    [Fact]
    public async Task Dos_Personas_O_Dos_Codigos_CA_023_07()
    {
        var (factory, collection) = await WaitlistHttp.CreateAsync();
        using var host = factory;
        using var first = WaitlistHttp.SignedIn(factory);
        using var second = WaitlistHttp.SignedIn(factory);
        using var a = await first.PostAsJsonAsync(WaitlistHttp.Route, new { cp = "91000" });
        using var b = await first.PostAsJsonAsync(WaitlistHttp.Route, new { cp = "91001" });
        using var c = await second.PostAsJsonAsync(WaitlistHttp.Route, new { cp = "91000" });
        foreach (var response in new[] { a, b, c }) response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(3);
    }
}

public sealed class Spec023ValidacionTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Tula1")]
    [InlineData("Tula<")]
    [InlineData("Tula>")]
    [InlineData("Tula$")]
    [InlineData("Tula{")]
    [InlineData("Tula}")]
    [InlineData("Tula;")]
    [InlineData("Tula\"")]
    [InlineData("Tula\r\n")]
    [InlineData("Tula\u0001")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task Municipio_Invalido_CA_023_08(string municipio)
    {
        var (factory, collection) = await WaitlistHttp.CreateAsync();
        using var host = factory;
        using var client = WaitlistHttp.SignedIn(factory);
        using var response = await client.PostAsJsonAsync(WaitlistHttp.Route, new { cp = "91000", municipio });
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").TryGetProperty("municipio", out _).Should().BeTrue();
        if (!string.IsNullOrWhiteSpace(municipio)) body.ToString().Should().NotContain(municipio);
        (await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(0);
    }

    [Theory]
    [InlineData("Tula de Allende")]
    [InlineData("San Agustín Tlaxiaca")]
    [InlineData("Ixmiquilpan")]
    [InlineData(null)]
    public async Task Municipios_Validos_CA_023_09(string? municipio)
    {
        var (factory, collection) = await WaitlistHttp.CreateAsync();
        using var host = factory;
        using var client = WaitlistHttp.SignedIn(factory);
        using var response = await client.PostAsJsonAsync(WaitlistHttp.Route, new { cp = "99999", municipio });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(1);
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("123456")]
    [InlineData("12abc")]
    public async Task Codigo_Invalido_Lista_De_Espera_R_06(string cp)
    {
        var (factory, collection) = await WaitlistHttp.CreateAsync();
        using var host = factory;
        using var client = WaitlistHttp.SignedIn(factory);
        using var response = await client.PostAsJsonAsync(WaitlistHttp.Route, new { cp });
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").TryGetProperty("cp", out _).Should().BeTrue();
        (await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(0);
    }
}

public sealed class Spec023SeguridadTests
{
    [Fact]
    public async Task Usuario_Del_Token_CA_023_10()
    {
        var (factory, collection) = await WaitlistHttp.CreateAsync();
        using var host = factory;
        var subject = Guid.NewGuid().ToString("D");
        using var client = WaitlistHttp.SignedIn(factory, subject);
        using var response = await client.PostAsJsonAsync(WaitlistHttp.Route, new { cp = "91000", usuarioId = Guid.NewGuid(), lat = 20.0512347, lon = -99.3456781 });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var document = await collection.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        document["usuarioId"].AsString.Should().Be(subject);
        document.Names.Should().BeEquivalentTo("_id", "usuarioId", "cp", "municipio", "creadoEn");
    }

    [Fact]
    public async Task Exige_Sesion_Y_Limite_CA_023_11()
    {
        var (factory, collection) = await WaitlistHttp.CreateAsync();
        using var host = factory;
        using var anonymous = factory.CreateClient();
        using var unauthorized = await anonymous.PostAsJsonAsync(WaitlistHttp.Route, new { cp = "91000" });
        unauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var client = WaitlistHttp.SignedIn(factory);
        for (var i = 0; i < 5; i++)
        {
            using var accepted = await client.PostAsJsonAsync(WaitlistHttp.Route, new { cp = $"9100{i}" });
            accepted.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        using var limited = await client.PostAsJsonAsync(WaitlistHttp.Route, new { cp = "91009" });
        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limited.Headers.RetryAfter.Should().NotBeNull();
        (await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(5);
    }

    [Fact]
    public async Task Cuerpo_Demasiado_Grande_CA_023_16()
    {
        var (factory, collection) = await WaitlistHttp.CreateAsync();
        using var host = factory;
        using var client = WaitlistHttp.SignedIn(factory);
        using var content = new StringContent("{\"cp\":\"91000\",\"extra\":\"" + new string('a', 2048) + "\"}", Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(WaitlistHttp.Route, content);
        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        (await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(0);
    }
}

public sealed class Spec023RegistrosTests
{
    [Fact]
    public async Task Sin_Datos_Personales_CA_023_12()
    {
        var (factory, _) = await WaitlistHttp.CreateAsync();
        using var host = factory;
        using var client = WaitlistHttp.SignedIn(factory);
        var output = await ConsoleCapture.RunAsync(async () =>
        {
            using var response = await client.PostAsJsonAsync(WaitlistHttp.Route, new { cp = "98765", municipio = "Tula", lat = 20.0512347, lon = -99.3456781 });
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            using var repeated = await client.PostAsJsonAsync(WaitlistHttp.Route, new { cp = "98765" });
            repeated.StatusCode.Should().Be(HttpStatusCode.OK);
            using var invalid = await client.PostAsJsonAsync(WaitlistHttp.Route, new { cp = "98765", municipio = "Tula<" });
            invalid.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            using var anonymous = factory.CreateClient();
            using var unauthorized = await anonymous.PostAsJsonAsync(WaitlistHttp.Route, new { cp = "98765" });
            unauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            using var content = new StringContent("{\"cp\":\"98765\",\"extra\":\"" + new string('a', 2048) + "\"}", Encoding.UTF8, "application/json");
            using var oversized = await client.PostAsync(WaitlistHttp.Route, content);
            oversized.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
            using var accepted = await client.PostAsJsonAsync(WaitlistHttp.Route, new { cp = "98765" });
            accepted.StatusCode.Should().Be(HttpStatusCode.OK);
            using var limited = await client.PostAsJsonAsync(WaitlistHttp.Route, new { cp = "98765" });
            limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        });
        output.Should().NotBeNullOrWhiteSpace();
        output.Should().NotContain("98765").And.NotContain("20.0512347").And.NotContain("99.3456781").And.NotContain("Tula");
    }
}
