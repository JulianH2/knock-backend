extern alias IdentityApi;

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using AwesomeAssertions;
using IdentityApi::Knock.Identity.Api.Modules.HomeAndCar.Services;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Identity;

/// <summary>
/// Spec 041, CAS-05 y CA-28 (T-06): <c>GET /api/v1/me/casa-y-auto/{id}/historial</c> de punta a punta. Identidad y Mercado son hosts reales sobre MongoDB y Redis reales; Identidad llama a la
/// ruta interna de Mercado en proceso, con su token de servicio de verdad. Solo datos ficticios.
/// </summary>
public sealed class Spec041HistorialTests : IAsyncLifetime
{
    private static readonly byte[] _userKey = Convert.FromBase64String(IdentityApiFactory.SigningKeyBase64);

    private MarketApiFactory _market = null!;
    private IdentityApiFactory _identity = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _market = await MarketApiFactory.CreateAsync(new Dictionary<string, string?>
        {
            ["Security:InternalTokens:IdentityKey"] = InternalServiceTokens.KeyBase64,
            ["Security:FieldEncryption:ActiveVersion"] = "1",
            ["Security:FieldEncryption:Keys:1"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
        });
        _identity = await IdentityApiFactory.CreateAsync(
            new Dictionary<string, string?>
            {
                ["Services:Market:BaseUrl"] = "http://market.test",
                ["Services:Market:InternalKey"] = InternalServiceTokens.KeyBase64,
            },
            services => services.Configure<HttpClientFactoryOptions>(
                nameof(IMarketHistory),
                options => options.HttpMessageHandlerBuilderActions.Add(builder => builder.PrimaryHandler = _market.Server.CreateHandler())));
        _client = _identity.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _identity.DisposeAsync();
        await _market.DisposeAsync();
    }

    private static string Token(string sub) => GatewayTestSupport.IssueToken(sub: sub, key: _userKey);

    private IMongoCollection<BsonDocument> Items => _identity.Database.GetCollection<BsonDocument>("mi_casa_y_auto");

    private async Task<HttpResponseMessage> GetAsync(string path, string? token) => await SendAsync(HttpMethod.Get, path, token);

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? token)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await _client.SendAsync(request);
    }

    private async Task<ObjectId> SeedItemAsync(string owner, string type = "auto", string name = "Auto de prueba")
    {
        var id = ObjectId.GenerateNewId();
        await Items.InsertOneAsync(new BsonDocument { { "_id", id }, { "usuarioId", owner }, { "tipo", type }, { "nombre", name }, { "creadoEn", DateTime.UtcNow } });
        return id;
    }

    private async Task<ObjectId> SeedTechnicianAsync(string name)
    {
        var id = ObjectId.GenerateNewId();
        await _market.Database.GetCollection<BsonDocument>("tecnicos").InsertOneAsync(new BsonDocument { { "_id", id }, { "usuarioId", Guid.NewGuid().ToString("D") }, { "nombre", name }, { "estado", "activo" } });
        return id;
    }

    private async Task SeedJobAsync(string client, string? element, string day, ObjectId technician, string trade = "Mecánica", string visitState = "terminada")
    {
        var request = new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "clienteId", client },
            { "oficioClave", "mec" },
            { "oficioNombre", trade },
            { "descripcion", "Texto privado de la solicitud" },
            { "direccionCifrada", "1.cifrada" },
            { "estado", "terminada" },
            { "creadoEn", DateTime.UtcNow },
        };
        if (element is not null)
        {
            request.Add("elementoId", element);
        }

        await _market.Database.GetCollection<BsonDocument>("solicitudes").InsertOneAsync(request);
        await _market.Database.GetCollection<BsonDocument>("visitas").InsertOneAsync(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "solicitudId", request["_id"] },
            { "clienteId", client },
            { "tecnicoUsuarioId", Guid.NewGuid().ToString("D") },
            { "tecnicoId", technician },
            { "dia", day },
            { "hora", "10:00" },
            { "precio", 1234 },
            { "estado", visitState },
        });
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) => JsonElement.Parse(await response.Content.ReadAsStringAsync());

    [Fact]
    public async Task TheOwnerSeesTheFinishedJobsOfTheirItem_WithoutPricesOrAddresses_CA_041_18()
    {
        var sub = Guid.NewGuid().ToString("D");
        var item = await SeedItemAsync(sub);
        var technician = await SeedTechnicianAsync("Técnica Ana de prueba");
        await SeedJobAsync(sub, item.ToString(), "2026-03-10", technician, "Mecánica");
        await SeedJobAsync(sub, item.ToString(), "2026-05-02", technician, "Electricidad");

        using var response = await GetAsync($"/api/v1/me/casa-y-auto/{item}/historial", Token(sub));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        var raw = await response.Content.ReadAsStringAsync();
        var items = JsonElement.Parse(raw).GetProperty("items").EnumerateArray().ToList();
        items.Select(entry => entry.GetProperty("fecha").GetString()).Should().Equal("2026-05-02", "2026-03-10");
        items[0].GetProperty("oficio").GetString().Should().Be("Electricidad");
        items[0].GetProperty("tecnico").GetString().Should().Be("Técnica Ana de prueba");
        items[0].EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo("fecha", "oficio", "tecnico");
        JsonElement.Parse(raw).GetProperty("nextCursor").ValueKind.Should().Be(JsonValueKind.Null);
        raw.Should().NotContain("1234").And.NotContain("cifrada").And.NotContain("Texto privado").And.NotContain(sub);
    }

    [Fact]
    public async Task OnlyTerminatedJobsWithThatElementAppear_AndRequestsWithoutElementDoNot_CA_041_27()
    {
        var sub = Guid.NewGuid().ToString("D");
        var item = await SeedItemAsync(sub);
        var otherItem = await SeedItemAsync(sub, "otro", "Otro de prueba");
        var technician = await SeedTechnicianAsync("Técnica Ana de prueba");
        await SeedJobAsync(sub, item.ToString(), "2026-03-10", technician);
        await SeedJobAsync(sub, item.ToString(), "2026-04-10", technician, visitState: "confirmada");
        await SeedJobAsync(sub, null, "2026-05-10", technician);
        await SeedJobAsync(sub, otherItem.ToString(), "2026-06-10", technician);

        using var response = await GetAsync($"/api/v1/me/casa-y-auto/{item}/historial", Token(sub));

        (await ReadAsync(response)).GetProperty("items").EnumerateArray().Select(entry => entry.GetProperty("fecha").GetString()).Should().Equal("2026-03-10");
    }

    [Fact]
    public async Task AnotherPersonsJobsLinkedToMyItemId_NeverAppear_CA_041_27()
    {
        var mine = Guid.NewGuid().ToString("D");
        var other = Guid.NewGuid().ToString("D");
        var item = await SeedItemAsync(mine);
        var technician = await SeedTechnicianAsync("Técnica Ana de prueba");
        await SeedJobAsync(other, item.ToString(), "2026-03-10", technician);

        using var response = await GetAsync($"/api/v1/me/casa-y-auto/{item}/historial", Token(mine));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAsync(response)).GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Theory]
    [InlineData("none")]
    [InlineData("expired")]
    public async Task WithoutTokenOrWithAnExpiredOne_Returns401_CA_041_19(string kind)
    {
        var sub = Guid.NewGuid().ToString("D");
        var item = await SeedItemAsync(sub);

        using var response = await GetAsync(
            $"/api/v1/me/casa-y-auto/{item}/historial",
            kind == "expired" ? GatewayTestSupport.IssueToken(sub: sub, key: _userKey, lifetime: TimeSpan.FromMinutes(-5)) : null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // La spec lista 403 para quien no es dueño (CA-20), pero R-16 y CA-26 piden 404 «sin revelar que existe»: gana el 404 (ver Memoria.md).
    [Fact]
    public async Task AnotherPersonsItem_Returns404_CA_041_20()
    {
        var owner = Guid.NewGuid().ToString("D");
        var item = await SeedItemAsync(owner);
        var technician = await SeedTechnicianAsync("Técnica Ana de prueba");
        await SeedJobAsync(owner, item.ToString(), "2026-03-10", technician);

        using var response = await GetAsync($"/api/v1/me/casa-y-auto/{item}/historial", Token(Guid.NewGuid().ToString("D")));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("Técnica Ana");
    }

    [Theory]
    [InlineData("/api/v1/me/casa-y-auto/abc/historial", "id")]
    [InlineData("/api/v1/me/casa-y-auto/0123456789abcdef0123456789/historial", "id")]
    [InlineData("/api/v1/me/casa-y-auto/0123456789abcdef01234567/historial?limite=51", "limite")]
    [InlineData("/api/v1/me/casa-y-auto/0123456789abcdef01234567/historial?limite=0", "limite")]
    [InlineData("/api/v1/me/casa-y-auto/0123456789abcdef01234567/historial?limite=-3", "limite")]
    [InlineData("/api/v1/me/casa-y-auto/0123456789abcdef01234567/historial?limite=abc", "limite")]
    [InlineData("/api/v1/me/casa-y-auto/0123456789abcdef01234567/historial?cursor=a%20b%3Cscript%3E", "cursor")]
    public async Task AnInvalidValue_Returns422WithTheField_AndDoesNotRepeatIt_CA_041_21(string path, string field)
    {
        using var response = await GetAsync(path, Token(Guid.NewGuid().ToString("D")));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var raw = await response.Content.ReadAsStringAsync();
        JsonElement.Parse(raw).GetProperty("errors").TryGetProperty(field, out _).Should().BeTrue($"el error va en errors.{field}");
        raw.Should().NotContain("abc").And.NotContain("script");
    }

    [Fact]
    public async Task ACursorThatMarketCannotReadReturns422Cursor_CA_041_21()
    {
        var sub = Guid.NewGuid().ToString("D");
        var item = await SeedItemAsync(sub);

        using var response = await GetAsync($"/api/v1/me/casa-y-auto/{item}/historial?cursor=aGVsbG8", Token(sub));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        JsonElement.Parse(await response.Content.ReadAsStringAsync()).GetProperty("errors").TryGetProperty("cursor", out _).Should().BeTrue();
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("deleted")]
    public async Task AnItemThatDoesNotExist_Returns404ProblemDetails_CA_041_22(string kind)
    {
        var sub = Guid.NewGuid().ToString("D");
        var id = ObjectId.GenerateNewId();
        if (kind == "deleted")
        {
            id = await SeedItemAsync(sub);
            await Items.DeleteOneAsync(new BsonDocument("_id", id));
        }

        using var response = await GetAsync($"/api/v1/me/casa-y-auto/{id}/historial", Token(sub));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task MoreThan20Jobs_Return20WithTheNextCursor_AndLimitsAreChecked_CA_041_23()
    {
        var sub = Guid.NewGuid().ToString("D");
        var item = await SeedItemAsync(sub);
        var technician = await SeedTechnicianAsync("Técnica Ana de prueba");
        for (var day = 1; day <= 25; day++)
        {
            await SeedJobAsync(sub, item.ToString(), $"2026-04-{day:00}", technician);
        }

        using var first = await GetAsync($"/api/v1/me/casa-y-auto/{item}/historial", Token(sub));
        var page1 = await ReadAsync(first);
        var cursor = page1.GetProperty("nextCursor").GetString()!;
        using var second = await GetAsync($"/api/v1/me/casa-y-auto/{item}/historial?cursor={cursor}", Token(sub));
        var page2 = await ReadAsync(second);
        using var capped = await GetAsync($"/api/v1/me/casa-y-auto/{item}/historial?limite=50", Token(sub));
        using var tooMany = await GetAsync($"/api/v1/me/casa-y-auto/{item}/historial?limite=51", Token(sub));

        page1.GetProperty("items").GetArrayLength().Should().Be(20);
        page1.GetProperty("items")[0].GetProperty("fecha").GetString().Should().Be("2026-04-25");
        page2.GetProperty("items").GetArrayLength().Should().Be(5);
        page2.GetProperty("nextCursor").ValueKind.Should().Be(JsonValueKind.Null);
        (await ReadAsync(capped)).GetProperty("items").GetArrayLength().Should().Be(25);
        tooMany.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task RemovingTheItem_DropsItFromTheList_ButTheFinishedJobsStayInMarket_CA_041_28()
    {
        var sub = Guid.NewGuid().ToString("D");
        var item = await SeedItemAsync(sub);
        var technician = await SeedTechnicianAsync("Técnica Ana de prueba");
        await SeedJobAsync(sub, item.ToString(), "2026-03-10", technician);
        var requests = _market.Database.GetCollection<BsonDocument>("solicitudes");
        var visits = _market.Database.GetCollection<BsonDocument>("visitas");
        var requestsBefore = await requests.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        var visitsBefore = await visits.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);

        using var deleted = await SendAsync(HttpMethod.Delete, $"/api/v1/me/casa-y-auto/{item}", Token(sub));
        using var list = await GetAsync("/api/v1/me/casa-y-auto", Token(sub));
        using var history = await GetAsync($"/api/v1/me/casa-y-auto/{item}/historial", Token(sub));

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ReadAsync(list)).EnumerateArray().Select(entry => entry.GetProperty("id").GetString()).Should().NotContain(item.ToString());
        history.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await requests.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(requestsBefore, "los trabajos de Mercado no se borran");
        (await visits.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(visitsBefore);
        (await requests.CountDocumentsAsync(new BsonDocument("elementoId", item.ToString()))).Should().Be(1, "solo se desliga el historial");
    }

    [Fact]
    public async Task AnIdentifierOfAnotherPersonInTheQuery_IsIgnored_TheTokenSubWins_CA_041_30()
    {
        var mine = Guid.NewGuid().ToString("D");
        var other = Guid.NewGuid().ToString("D");
        var item = await SeedItemAsync(mine);
        var technician = await SeedTechnicianAsync("Técnica Ana de prueba");
        await SeedJobAsync(mine, item.ToString(), "2026-03-10", technician);
        await SeedJobAsync(other, item.ToString(), "2026-04-10", technician);

        using var response = await GetAsync($"/api/v1/me/casa-y-auto/{item}/historial?usuarioId={other}&sub={other}", Token(mine));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAsync(response)).GetProperty("items").EnumerateArray().Select(entry => entry.GetProperty("fecha").GetString()).Should().Equal("2026-03-10");
    }

    [Fact]
    public async Task WhenMarketDoesNotAnswer_Returns503MarketUnavailable_AndInventsNothing_CA_041_18()
    {
        await using var identity = await IdentityApiFactory.CreateAsync(new Dictionary<string, string?>
        {
            ["Services:Market:BaseUrl"] = "http://127.0.0.1:1",
            ["Services:Market:InternalKey"] = InternalServiceTokens.KeyBase64,
            ["Services:Market:TimeoutSeconds"] = "1",
        });
        using var client = identity.CreateClient();
        var sub = Guid.NewGuid().ToString("D");
        var id = ObjectId.GenerateNewId();
        await identity.Database.GetCollection<BsonDocument>("mi_casa_y_auto").InsertOneAsync(
            new BsonDocument { { "_id", id }, { "usuarioId", sub }, { "tipo", "auto" }, { "nombre", "Auto de prueba" }, { "creadoEn", DateTime.UtcNow } });
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"/api/v1/me/casa-y-auto/{id}/historial", UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token(sub));

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        JsonElement.Parse(await response.Content.ReadAsStringAsync()).GetProperty("code").GetString().Should().Be("market_unavailable");
    }

    [Fact]
    public async Task WithoutTheServiceKey_Returns503MarketUnavailable_CA_041_18()
    {
        await using var identity = await IdentityApiFactory.CreateAsync(new Dictionary<string, string?> { ["Services:Market:BaseUrl"] = "http://market.test" });
        using var client = identity.CreateClient();
        var sub = Guid.NewGuid().ToString("D");
        var id = ObjectId.GenerateNewId();
        await identity.Database.GetCollection<BsonDocument>("mi_casa_y_auto").InsertOneAsync(
            new BsonDocument { { "_id", id }, { "usuarioId", sub }, { "tipo", "auto" }, { "nombre", "Auto de prueba" }, { "creadoEn", DateTime.UtcNow } });
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"/api/v1/me/casa-y-auto/{id}/historial", UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token(sub));

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
