using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using AwesomeAssertions;
using Knock.SecurityTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.SecurityTest.Market;

public sealed class Spec027BusquedaSecurityTests
{
    [Fact]
    public async Task Cursor_Invalido_No_Expone_Detalles_CA_027_11()
    {
        using var factory = await SecurityMarketFactory.CreateAsync();
        var id = ObjectId.GenerateNewId();
        await factory.Database.GetCollection<BsonDocument>("colonias").InsertOneAsync(new BsonDocument { ["_id"] = id, ["activo"] = true });
        await factory.Database.GetCollection<BsonDocument>("catalogo_meta").InsertOneAsync(new BsonDocument { ["_id"] = "sinonimos", ["version"] = 1 });
        using var client = factory.CreateClient();
        using var response = await SecureHttp.SendAsync(client, HttpMethod.Get, $"/api/v1/busqueda?q=boiler&coloniaId={id}&cursor=cursor-malformado", bearer: TokenForge.Valid());
        await VerifyAsync(response, HttpStatusCode.BadRequest, "invalid_cursor");
    }

    [Fact]
    public async Task Mongo_No_Disponible_No_Expone_Detalles_CA_027_17()
    {
        SecurityHostConfiguration.ClearFirebaseEmulatorHost();
        var settings = new Dictionary<string, string?>
        {
            ["Busqueda:Cursor:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            ["ConnectionStrings:KnockMongo"] = new MongoUrlBuilder { Server = new MongoServerAddress("127.0.0.1", 1), ServerSelectionTimeout = TimeSpan.FromMilliseconds(500) }.ToString(),
            ["Mongo:Databases:Catalog"] = "market_offline",
            ["Mongo:Databases:Requests"] = "market_offline",
        };
        using var factory = new SecurityMarketFactory(settings, "Production");
        using var client = factory.CreateClient();
        using var response = await SecureHttp.SendAsync(client, HttpMethod.Get, $"/api/v1/busqueda?q=boiler&coloniaId={ObjectId.GenerateNewId()}", bearer: TokenForge.Valid());
        await VerifyAsync(response, HttpStatusCode.ServiceUnavailable, "search_unavailable");
    }

    private static async Task VerifyAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        response.StatusCode.Should().Be(status);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetString().Should().Be(code);
        body.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("correlationId").GetString().Should().NotBeNullOrEmpty();
        body.GetRawText().Should().NotContain("SearchRank").And.NotContain("127.0.0.1").And.NotContain("cursor-malformado").And.NotContain("stackTrace");
    }
}
