using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Requests;

internal static class QuoteHttp
{
    public static Uri Route(ObjectId id) => new($"/api/v1/solicitudes/{id}/cotizaciones", UriKind.Relative);
    public static object Body(decimal price = 100) => new { tipoPrecio = "fijo", precio = price, dia = DateOnly.FromDateTime(DateTime.UtcNow), hora = "12:00" };

    public static async Task<ObjectId> Request(MarketApiFactory factory, string state = "enviada")
    {
        var id = ObjectId.GenerateNewId();
        await factory.Requests.InsertOneAsync(new BsonDocument
        {
            ["_id"] = id,
            ["oficioClave"] = "plo",
            ["coloniaId"] = "test-zone",
            ["estado"] = state,
            ["venceEn"] = DateTime.UtcNow.AddHours(1),
            ["dia"] = DateTime.UtcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
        });
        return id;
    }

    public static async Task<HttpClient> Technician(MarketApiFactory factory, string trade = "plo")
    {
        var sub = Guid.NewGuid().ToString("D");
        await factory.Database.GetCollection<BsonDocument>("tecnicos").InsertOneAsync(new BsonDocument
        {
            ["usuarioId"] = sub,
            ["estado"] = "activo",
            ["oculto"] = false,
            ["oficios"] = new BsonArray { trade },
            ["coloniaBaseId"] = "test-zone"
        });
        return WaitlistHttp.SignedIn(factory, sub);
    }

    public static async Task Code(HttpResponseMessage response, string code)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetString().Should().Be(code);
    }
}

public sealed class Spec020CotizacionesTests
{
    [Theory]
    [InlineData("oficioClave", false)]
    [InlineData("coloniaId", false)]
    [InlineData("estado", false)]
    [InlineData("venceEn", false)]
    [InlineData("dia", false)]
    [InlineData("dia", true)]
    [InlineData("oficioClave", true)]
    [InlineData("coloniaId", true)]
    [InlineData("estado", true)]
    [InlineData("venceEn", true)]
    public async Task Solicitud_Malformada_Se_Trata_Como_Inexistente_CA_020_09(string field, bool wrongType)
    {
        using var factory = await MarketApiFactory.CreateAsync();
        var id = await QuoteHttp.Request(factory);
        var update = wrongType
            ? Builders<BsonDocument>.Update.Set(field, new BsonDocument("private", "synthetic-sensitive-value"))
            : Builders<BsonDocument>.Update.Unset(field);
        await factory.Requests.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", id), update);
        using var client = await QuoteHttp.Technician(factory);
        using var response = await client.PostAsJsonAsync(QuoteHttp.Route(id), QuoteHttp.Body());
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await factory.Quotes.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(0);
    }

    [Fact]
    public async Task Solicitud_Malformada_Registra_Solo_Identificador_CA_020_09()
    {
        using var factory = await MarketApiFactory.CreateAsync();
        var id = await QuoteHttp.Request(factory);
        await factory.Requests.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", id),
            Builders<BsonDocument>.Update.Set("dia", new BsonDocument("private", "synthetic-sensitive-value")));
        using var client = await QuoteHttp.Technician(factory);
        var output = await ConsoleCapture.RunAsync(async () =>
        {
            using var response = await client.PostAsJsonAsync(QuoteHttp.Route(id), QuoteHttp.Body());
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        });
        output.Should().Contain("Malformed request document").And.Contain(id.ToString());
        output.Should().NotContain("synthetic-sensitive-value");
    }

    [Fact]
    public async Task Tecnico_Con_Id_Malformado_No_Puede_Cotizar_CA_020_09()
    {
        using var factory = await MarketApiFactory.CreateAsync();
        var id = await QuoteHttp.Request(factory);
        var sub = Guid.NewGuid().ToString("D");
        await factory.Database.GetCollection<BsonDocument>("tecnicos").InsertOneAsync(new BsonDocument
        {
            ["_id"] = "invalid-id",
            ["usuarioId"] = sub,
            ["estado"] = "activo",
            ["oculto"] = false,
            ["oficios"] = new BsonArray { "plo" },
            ["coloniaBaseId"] = "test-zone"
        });
        using var client = WaitlistHttp.SignedIn(factory, sub);
        using var response = await client.PostAsJsonAsync(QuoteHttp.Route(id), QuoteHttp.Body());
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await factory.Quotes.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(0);
    }

    [Fact]
    public async Task Envia_Cotizacion_CA_020_06()
    {
        using var factory = await MarketApiFactory.CreateAsync();
        var id = await QuoteHttp.Request(factory);
        using var client = await QuoteHttp.Technician(factory);
        using var response = await client.PostAsJsonAsync(QuoteHttp.Route(id), QuoteHttp.Body());
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var quote = await response.Content.ReadFromJsonAsync<JsonElement>();
        quote.GetProperty("estado").GetString().Should().Be("enviada");
        var request = await factory.Requests.Find(Builders<BsonDocument>.Filter.Eq("_id", id)).SingleAsync();
        request["estado"].AsString.Should().Be("cotizando");
        var message = await factory.Database.GetCollection<BsonDocument>("outbox").Find(Builders<BsonDocument>.Filter.Eq("solicitudId", id)).SingleAsync();
        message["tipo"].AsString.Should().Be("CotizacionEnviada");
    }

    [Fact]
    public async Task Rechaza_Cotizacion_Duplicada_CA_020_07()
    {
        using var factory = await MarketApiFactory.CreateAsync();
        var id = await QuoteHttp.Request(factory);
        using var client = await QuoteHttp.Technician(factory);
        using var first = await client.PostAsJsonAsync(QuoteHttp.Route(id), QuoteHttp.Body());
        using var second = await client.PostAsJsonAsync(QuoteHttp.Route(id), QuoteHttp.Body());
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await QuoteHttp.Code(second, "quote_duplicate");
    }

    [Theory]
    [InlineData("vencida")]
    [InlineData("cancelada")]
    [InlineData("cita_confirmada")]
    public async Task Rechaza_Solicitud_No_Cotizable_CA_020_08(string state)
    {
        using var factory = await MarketApiFactory.CreateAsync();
        var id = await QuoteHttp.Request(factory, state);
        using var client = await QuoteHttp.Technician(factory);
        using var response = await client.PostAsJsonAsync(QuoteHttp.Route(id), QuoteHttp.Body());
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await QuoteHttp.Code(response, "request_not_quotable");
    }

    [Fact]
    public async Task Oculta_Solicitud_Ajena_CA_020_09()
    {
        using var factory = await MarketApiFactory.CreateAsync();
        var id = await QuoteHttp.Request(factory);
        using var client = await QuoteHttp.Technician(factory, "ele");
        using var other = await client.PostAsJsonAsync(QuoteHttp.Route(id), QuoteHttp.Body());
        using var missing = await client.PostAsJsonAsync(QuoteHttp.Route(ObjectId.GenerateNewId()), QuoteHttp.Body());
        other.StatusCode.Should().Be(HttpStatusCode.NotFound);
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(200001)]
    [InlineData(1.5)]
    public async Task Rechaza_Montos_Invalidos_CA_020_10(decimal price)
    {
        using var factory = await MarketApiFactory.CreateAsync();
        var id = await QuoteHttp.Request(factory);
        using var client = await QuoteHttp.Technician(factory);
        using var response = await client.PostAsJsonAsync(QuoteHttp.Route(id), QuoteHttp.Body(price));
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errors").TryGetProperty("precio", out _).Should().BeTrue();
        (await factory.Quotes.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(0);
    }

    [Theory]
    [InlineData(100, 100)]
    [InlineData(101, 100)]
    public async Task Rechaza_Rango_Invalido_CA_020_10(int min, int max)
    {
        using var factory = await MarketApiFactory.CreateAsync();
        var id = await QuoteHttp.Request(factory);
        using var client = await QuoteHttp.Technician(factory);
        using var response = await client.PostAsJsonAsync(QuoteHttp.Route(id), new
        {
            tipoPrecio = "rango",
            precioMin = min,
            precioMax = max,
            dia = DateOnly.FromDateTime(DateTime.UtcNow),
            hora = "12:00"
        });
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errors").TryGetProperty("precioMax", out _).Should().BeTrue();
        (await factory.Quotes.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(0);
    }
}

public sealed class Spec020ConcurrenciaTests
{
    [Fact]
    public async Task Cupo_De_Cinco_Cotizaciones_CA_020_19()
    {
        using var factory = await MarketApiFactory.CreateAsync();
        var id = await QuoteHttp.Request(factory);
        for (var i = 0; i < 4; i++)
        {
            using var client = await QuoteHttp.Technician(factory);
            using var response = await client.PostAsJsonAsync(QuoteHttp.Route(id), QuoteHttp.Body());
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        using var a = await QuoteHttp.Technician(factory);
        using var b = await QuoteHttp.Technician(factory);
        var responses = await Task.WhenAll(a.PostAsJsonAsync(QuoteHttp.Route(id), QuoteHttp.Body()), b.PostAsJsonAsync(QuoteHttp.Route(id), QuoteHttp.Body()));
        try
        {
            responses.Select(x => x.StatusCode).Should().BeEquivalentTo(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict });
            await QuoteHttp.Code(responses.Single(x => x.StatusCode == HttpStatusCode.Conflict), "quote_quota_full");
            (await factory.Quotes.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(5);
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }
}
