using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Requests;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Catalog;

public sealed class Spec027BusquedaHttpTests
{
    [Fact]
    public async Task Validaciones_Y_Sesion_CA_027_12_CA_027_16()
    {
        using var factory = await MarketApiFactory.CreateAsync();
        var colonia = await SeedAsync(factory, 0);
        using var anonymous = factory.CreateClient();
        using var denied = await anonymous.GetAsync(new Uri($"/api/v1/busqueda?q=boiler&coloniaId={colonia}", UriKind.Relative));
        denied.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var client = WaitlistHttp.SignedIn(factory);
        var queries = new[]
        {
            $"q=a&coloniaId={colonia}", $"q={new string('a', 81)}&coloniaId={colonia}", $"q=!!!&coloniaId={colonia}",
            $"q=boiler&q=fuga&coloniaId={colonia}", $"q=boiler&coloniaId={colonia}&limit=21", $"q=boiler&coloniaId={colonia}&soloVerificados=otro",
            $"q=boiler&coloniaId={colonia}&limit=1&limit=2", $"q=boiler&coloniaId={colonia}&cursor=a&cursor=b",
            $"q=boiler&coloniaId={colonia}&oficio=plo&oficio=ele", $"q=boiler&coloniaId={colonia}&soloVerificados=true&soloVerificados=false",
            $"q=boiler&coloniaId={colonia}&coloniaId={colonia}", "q=boiler&coloniaId=mal", $"q=boiler&coloniaId={ObjectId.GenerateNewId()}",
            $"q=boiler&coloniaId={colonia}&oficio=inactivo", $"coloniaId={colonia}", $"q=boiler&coloniaId={colonia}&limit=0",
        };
        foreach (var query in queries)
        {
            using var response = await client.GetAsync(new Uri("/api/v1/busqueda?" + query, UriKind.Relative));
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity, query);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        }
        using var badQ = await client.GetAsync(new Uri($"/api/v1/busqueda?q=a&coloniaId={colonia}", UriKind.Relative));
        var invalidBody = await badQ.Content.ReadFromJsonAsync<JsonElement>();
        invalidBody.GetProperty("errors").TryGetProperty("q", out _).Should().BeTrue();
        invalidBody.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
        invalidBody.GetProperty("correlationId").GetString().Should().NotBeNullOrEmpty();
        using var badLimit = await client.GetAsync(new Uri($"/api/v1/busqueda?q=boiler&coloniaId={colonia}&limit=21", UriKind.Relative));
        (await badLimit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").TryGetProperty("limit", out _).Should().BeTrue();
        using var badColonia = await client.GetAsync(new Uri($"/api/v1/busqueda?q=boiler&coloniaId={ObjectId.GenerateNewId()}", UriKind.Relative));
        (await badColonia.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().Should().Be("colonia_not_found");
        using var badTrade = await client.GetAsync(new Uri($"/api/v1/busqueda?q=boiler&coloniaId={colonia}&oficio=inactivo", UriKind.Relative));
        (await badTrade.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().Should().Be("trade_invalid");
    }

    [Fact]
    public async Task Resultados_Cursor_Y_Datos_Publicos_CA_027_01_CA_027_10_CA_027_13()
    {
        using var factory = await MarketApiFactory.CreateAsync();
        var colonia = await SeedAsync(factory, 21);
        using var client = WaitlistHttp.SignedIn(factory);
        using var response = await client.GetAsync(new Uri($"/api/v1/busqueda?q=boiler&coloniaId={colonia}&extra=a&extra=b", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.Private.Should().BeTrue();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("interpretacion").GetProperty("estado").GetString().Should().Be("detectado");
        body.GetProperty("interpretacion").GetProperty("oficioDetectado").GetProperty("clave").GetString().Should().Be("plo");
        body.GetProperty("items").GetArrayLength().Should().Be(20);
        body.GetProperty("items")[0].GetProperty("distanciaKm").GetInt32().Should().Be(1);
        var cursor = body.GetProperty("nextCursor").GetString();
        cursor.Should().NotBeNullOrEmpty();
        var text = body.GetRawText();
        text.Should().NotContain("SearchRank").And.NotContain("searchRank").And.NotContain("usuarioId").And.NotContain("ubicacionBase")
            .And.NotContain("radioAtencionKm").And.NotContain("0.012345").And.NotContain("telefono").And.NotContain("contacto")
            .And.NotContain("direccion").And.NotContain("coordinates").And.NotContain("5500001234")
            .And.NotContain("privado@ejemplo.test").And.NotContain("calle ficticia");
        using var second = await client.GetAsync(new Uri($"/api/v1/busqueda?q=boiler&coloniaId={colonia}&cursor={Uri.EscapeDataString(cursor)}", UriKind.Relative));
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").GetArrayLength().Should().Be(1);
        var altered = (cursor[0] == 'A' ? "B" : "A") + cursor[1..];
        using var bad = await client.GetAsync(new Uri($"/api/v1/busqueda?q=boiler&coloniaId={colonia}&cursor={Uri.EscapeDataString(altered)}", UriKind.Relative));
        bad.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await bad.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().Should().Be("invalid_cursor");
    }

    [Fact]
    public async Task Mongo_Caido_Devuelve_503_CA_027_17()
    {
        using var factory = MarketApiFactory.CreateOffline(_ => { });
        using var client = WaitlistHttp.SignedIn(factory);
        using var response = await client.GetAsync(new Uri($"/api/v1/busqueda?q=boiler&coloniaId={ObjectId.GenerateNewId()}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetString().Should().Be("search_unavailable");
        body.GetRawText().Should().NotContain("127.0.0.1").And.NotContain("SearchRank");
    }

    [Fact]
    public async Task Cursor_De_Otra_Consulta_O_Version_Se_Rechaza_CA_027_11()
    {
        using var factory = await MarketApiFactory.CreateAsync();
        var colonia = await SeedAsync(factory, 2);
        using var client = WaitlistHttp.SignedIn(factory);
        using var first = await client.GetAsync(new Uri($"/api/v1/busqueda?q=boiler&coloniaId={colonia}&limit=1", UriKind.Relative));
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var cursor = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("nextCursor").GetString();
        cursor.Should().NotBeNullOrEmpty();
        using var other = await client.GetAsync(new Uri($"/api/v1/busqueda?q=fuga&coloniaId={colonia}&cursor={Uri.EscapeDataString(cursor)}", UriKind.Relative));
        other.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await other.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().Should().Be("invalid_cursor");
        await factory.Database.GetCollection<BsonDocument>("catalogo_meta").UpdateOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", "sinonimos"), Builders<BsonDocument>.Update.Set("version", 2));
        using var changed = await client.GetAsync(new Uri($"/api/v1/busqueda?q=boiler&coloniaId={colonia}&cursor={Uri.EscapeDataString(cursor)}", UriKind.Relative));
        changed.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await changed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().Should().Be("invalid_cursor");
    }

    [Fact]
    public async Task Registros_De_Busqueda_No_Contienen_Datos_CA_027_18()
    {
        using var factory = await MarketApiFactory.CreateAsync();
        var colonia = await SeedAsync(factory, 1);
        var subject = Guid.NewGuid().ToString("D");
        using var client = WaitlistHttp.SignedIn(factory, subject);
        var terms = new[] { "boiler", "5500001234", "ejemplo@dominio.test", "calle inventada 123" };
        var logs = await ConsoleCapture.RunAsync(async () =>
        {
            foreach (var term in terms)
            {
                using var response = await client.GetAsync(new Uri($"/api/v1/busqueda?q={Uri.EscapeDataString(term)}&coloniaId={colonia}", UriKind.Relative));
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        });
        logs.Should().NotBeNullOrWhiteSpace();
        foreach (var term in terms) logs.Should().NotContain(term);
        logs.Should().NotContain("ejemplo dominio test").And.NotContain(colonia.ToString()).And.NotContain(subject);
    }

    private static async Task<ObjectId> SeedAsync(MarketApiFactory factory, int technicians)
    {
        var db = factory.Database;
        var id = ObjectId.GenerateNewId();
        await db.GetCollection<BsonDocument>("colonias").InsertOneAsync(new BsonDocument
        {
            ["_id"] = id,
            ["activo"] = true,
            ["centro"] = new BsonDocument { ["type"] = "Point", ["coordinates"] = new BsonArray { 0, 0 } }
        });
        await db.GetCollection<BsonDocument>("oficios").InsertOneAsync(new BsonDocument { ["clave"] = "plo", ["nombre"] = "Plomería", ["nombreBusqueda"] = "PLOMERIA", ["modalidad"] = "domicilio", ["activo"] = true });
        await db.GetCollection<BsonDocument>("oficios").InsertOneAsync(new BsonDocument { ["clave"] = "inactivo", ["nombre"] = "Oficio ficticio", ["nombreBusqueda"] = "OFICIO FICTICIO", ["modalidad"] = "domicilio", ["activo"] = false });
        await db.GetCollection<BsonDocument>("sinonimos").InsertOneAsync(new BsonDocument { ["termino"] = "boiler", ["oficioClave"] = "plo", ["version"] = 1 });
        await db.GetCollection<BsonDocument>("catalogo_meta").InsertOneAsync(new BsonDocument { ["_id"] = "sinonimos", ["version"] = 1 });
        if (technicians > 0)
        {
            var docs = Enumerable.Range(0, technicians).Select(_ => new BsonDocument
            {
                ["nombrePublico"] = "Negocio ficticio",
                ["usuarioId"] = Guid.NewGuid().ToString("D"),
                ["telefono"] = "5500001234",
                ["contactoPublico"] = "privado@ejemplo.test",
                ["direccion"] = "calle ficticia",
                ["palabrasBusqueda"] = new BsonArray { "negocio" },
                ["oficios"] = new BsonArray { "plo" },
                ["estado"] = "activo",
                ["oculto"] = false,
                ["nivelVerificacion"] = "ninguno",
                ["coloniaBaseId"] = id.ToString(),
                ["calificacion"] = 4,
                ["totalResenas"] = 1,
                ["radioAtencionKm"] = 5,
                ["ubicacionBase"] = new BsonDocument { ["type"] = "Point", ["coordinates"] = new BsonArray { 0.012345, 0 } },
            });
            var collection = db.GetCollection<BsonDocument>("tecnicos");
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Geo2DSphere("ubicacionBase")));
            await collection.InsertManyAsync(docs);
        }
        return id;
    }
}
