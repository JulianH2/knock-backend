using System.Net;
using System.Net.Http.Headers;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Requests;

/// <summary>
/// Spec 041, R-17 (T-00 y T-06): la ruta interna <c>GET /api/v1/interno/historial</c> de Mercado, que solo llama Identidad con su token de servicio. MongoDB real; solo datos ficticios.
/// </summary>
public sealed class Spec041HistorialInternoTests
{
    private const string _route = "/api/v1/interno/historial";

    private static readonly Dictionary<string, string?> _settings = new() { ["Security:InternalTokens:IdentityKey"] = InternalServiceTokens.KeyBase64 };

    private static async Task<Spec020Host> StartAsync() => await Spec020Host.StartAsync(_settings);

    private static async Task<HttpResponseMessage> GetAsync(Spec020Host host, string query, string? serviceToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{_route}?{query}", UriKind.Relative));
        if (serviceToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceToken);
        }

        return await host.Client.SendAsync(request);
    }

    private static string Query(string client, string element, string? extra = null) => $"usuarioId={client}&elementoId={element}" + (extra is null ? string.Empty : "&" + extra);

    private static async Task<ObjectId> SeedTechnicianAsync(Spec020Host host, string name)
    {
        var id = ObjectId.GenerateNewId();
        await host.Database.GetCollection<BsonDocument>("tecnicos").InsertOneAsync(new BsonDocument { { "_id", id }, { "usuarioId", Guid.NewGuid().ToString("D") }, { "nombre", name }, { "estado", "activo" } });
        return id;
    }

    /// <summary>Una solicitud del cliente (con o sin <c>elementoId</c>) y, si se pide, su visita en el estado dado.</summary>
    private static async Task<ObjectId> SeedAsync(Spec020Host host, string client, string? element, string trade, string? visitState, string day, ObjectId technician)
    {
        var request = new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "clienteId", client },
            { "oficioClave", "mec" },
            { "oficioNombre", trade },
            { "descripcion", "Texto privado de la solicitud" },
            { "direccionCifrada", "1.cifrada" },
            { "estado", visitState == "terminada" ? "terminada" : "cita_confirmada" },
            { "creadoEn", DateTime.UtcNow },
        };
        if (element is not null)
        {
            request.Add("elementoId", element);
        }

        await host.Database.GetCollection<BsonDocument>("solicitudes").InsertOneAsync(request);
        if (visitState is not null)
        {
            await host.Database.GetCollection<BsonDocument>("visitas").InsertOneAsync(new BsonDocument
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

        return request["_id"].AsObjectId;
    }

    [Fact]
    public async Task ListsOnlyTheFinishedVisitsOfThatClientAndElement_NewestFirst_WithoutPricesOrAddresses_CA_041_27()
    {
        await using var host = await StartAsync();
        var client = Guid.NewGuid().ToString("D");
        var element = ObjectId.GenerateNewId().ToString();
        var otherElement = ObjectId.GenerateNewId().ToString();
        var ana = await SeedTechnicianAsync(host, "Técnica Ana de prueba");
        var beto = await SeedTechnicianAsync(host, "Técnico Beto de prueba");
        await SeedAsync(host, client, element, "Mecánica", "terminada", "2026-03-10", ana);
        await SeedAsync(host, client, element, "Plomería", "terminada", "2026-05-02", beto);
        await SeedAsync(host, client, element, "Mecánica", "confirmada", "2026-06-01", ana);
        await SeedAsync(host, client, element, "Mecánica", null, "2026-06-02", ana);
        await SeedAsync(host, client, null, "Mecánica", "terminada", "2026-07-01", ana);
        await SeedAsync(host, client, otherElement, "Mecánica", "terminada", "2026-07-02", ana);
        await SeedAsync(host, Guid.NewGuid().ToString("D"), element, "Mecánica", "terminada", "2026-07-03", ana);

        using var response = await GetAsync(host, Query(client, element), InternalServiceTokens.Issue(client, element));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        var raw = await response.Content.ReadAsStringAsync();
        var json = System.Text.Json.JsonElement.Parse(raw);
        var items = json.GetProperty("items").EnumerateArray().ToList();
        items.Select(item => item.GetProperty("fecha").GetString()).Should().Equal("2026-05-02", "2026-03-10");
        items[0].GetProperty("oficio").GetString().Should().Be("Plomería");
        items[0].GetProperty("tecnico").GetString().Should().Be("Técnico Beto de prueba");
        items[1].EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo("fecha", "oficio", "tecnico");
        json.GetProperty("nextCursor").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
        raw.Should().NotContain("1234").And.NotContain("cifrada").And.NotContain("Texto privado").And.NotContain(client);
    }

    [Fact]
    public async Task ADifferentClientWithTheSameElementId_GetsNothing_CA_041_27()
    {
        await using var host = await StartAsync();
        var owner = Guid.NewGuid().ToString("D");
        var intruder = Guid.NewGuid().ToString("D");
        var element = ObjectId.GenerateNewId().ToString();
        await SeedAsync(host, owner, element, "Mecánica", "terminada", "2026-03-10", await SeedTechnicianAsync(host, "Técnica Ana de prueba"));

        using var response = await GetAsync(host, Query(intruder, element), InternalServiceTokens.Issue(intruder, element));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        System.Text.Json.JsonElement.Parse(await response.Content.ReadAsStringAsync()).GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task PagesByCursor_20ByDefault_AndTheLimitCapsAt50_CA_041_23()
    {
        await using var host = await StartAsync();
        var client = Guid.NewGuid().ToString("D");
        var element = ObjectId.GenerateNewId().ToString();
        var technician = await SeedTechnicianAsync(host, "Técnica Ana de prueba");
        for (var day = 1; day <= 25; day++)
        {
            await SeedAsync(host, client, element, "Mecánica", "terminada", $"2026-04-{day:00}", technician);
        }

        using var first = await GetAsync(host, Query(client, element), InternalServiceTokens.Issue(client, element));
        var page1 = System.Text.Json.JsonElement.Parse(await first.Content.ReadAsStringAsync());
        var cursor = page1.GetProperty("nextCursor").GetString()!;
        using var second = await GetAsync(host, Query(client, element, "cursor=" + cursor), InternalServiceTokens.Issue(client, element));
        var page2 = System.Text.Json.JsonElement.Parse(await second.Content.ReadAsStringAsync());
        using var tooMany = await GetAsync(host, Query(client, element, "limite=51"), InternalServiceTokens.Issue(client, element));

        page1.GetProperty("items").GetArrayLength().Should().Be(20);
        page1.GetProperty("items")[0].GetProperty("fecha").GetString().Should().Be("2026-04-25");
        page2.GetProperty("items").GetArrayLength().Should().Be(5);
        page2.GetProperty("items")[0].GetProperty("fecha").GetString().Should().Be("2026-04-05");
        page2.GetProperty("nextCursor").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
        tooMany.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task SameDayVisits_NeverRepeatOrSkipAcrossPages_CA_041_23()
    {
        await using var host = await StartAsync();
        var client = Guid.NewGuid().ToString("D");
        var element = ObjectId.GenerateNewId().ToString();
        var technician = await SeedTechnicianAsync(host, "Técnica Ana de prueba");
        for (var i = 0; i < 5; i++)
        {
            await SeedAsync(host, client, element, "Mecánica", "terminada", "2026-04-10", technician);
        }

        var seen = new List<string>();
        string? cursor = null;
        do
        {
            using var response = await GetAsync(host, Query(client, element, "limite=2" + (cursor is null ? string.Empty : "&cursor=" + cursor)), InternalServiceTokens.Issue(client, element));
            var page = System.Text.Json.JsonElement.Parse(await response.Content.ReadAsStringAsync());
            seen.AddRange(page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("fecha").GetString()!));
            cursor = page.GetProperty("nextCursor").GetString();
        }
        while (cursor is not null);

        seen.Should().HaveCount(5);
    }

    [Theory]
    [InlineData("limite=0")]
    [InlineData("limite=51")]
    [InlineData("limite=-1")]
    [InlineData("cursor=a%20b")]
    [InlineData("cursor=<script>")]
    public async Task AnInvalidValue_Returns422_CA_041_21(string extra)
    {
        await using var host = await StartAsync();
        var client = Guid.NewGuid().ToString("D");
        var element = ObjectId.GenerateNewId().ToString();

        using var response = await GetAsync(host, Query(client, element, extra), InternalServiceTokens.Issue(client, element));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Theory]
    [InlineData("usuarioId=&elementoId=0123456789abcdef01234567", "usuarioId")]
    [InlineData("usuarioId=cliente&elementoId=abc", "elementoId")]
    [InlineData("usuarioId=cliente", "elementoId")]
    public async Task AnInvalidClientOrElement_Returns422WithTheField_CA_041_21(string query, string field)
    {
        await using var host = await StartAsync();

        using var response = await GetAsync(host, query, InternalServiceTokens.Issue("cliente", "0123456789abcdef01234567"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        System.Text.Json.JsonElement.Parse(await response.Content.ReadAsStringAsync()).GetProperty("errors").TryGetProperty(field, out _).Should().BeTrue();
    }

    [Fact]
    public async Task AnUnreadableCursor_Returns400InvalidCursor_CA_041_21()
    {
        await using var host = await StartAsync();
        var client = Guid.NewGuid().ToString("D");
        var element = ObjectId.GenerateNewId().ToString();

        using var response = await GetAsync(host, Query(client, element, "cursor=aGVsbG8"), InternalServiceTokens.Issue(client, element));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        System.Text.Json.JsonElement.Parse(await response.Content.ReadAsStringAsync()).GetProperty("code").GetString().Should().Be("invalid_cursor");
    }

    [Fact]
    public async Task WithoutAServiceToken_Returns401_AndTheUserTokenDoesNotWork_R_017()
    {
        await using var host = await StartAsync();
        var client = Guid.NewGuid().ToString("D");
        var element = ObjectId.GenerateNewId().ToString();

        using var anonymous = await GetAsync(host, Query(client, element), null);
        using var userToken = await GetAsync(host, Query(client, element), Spec020Host.Token(client));

        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        userToken.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "la clave de los tokens de usuario no firma tokens de servicio");
    }

    [Theory]
    [InlineData("otra-clave")]
    [InlineData("otro-emisor")]
    [InlineData("otra-audiencia")]
    [InlineData("vencido")]
    [InlineData("vida-larga")]
    [InlineData("sin-jti")]
    public async Task ABadServiceToken_Returns401_R_017(string defect)
    {
        await using var host = await StartAsync();
        var client = Guid.NewGuid().ToString("D");
        var element = ObjectId.GenerateNewId().ToString();
        var token = defect switch
        {
            "otra-clave" => InternalServiceTokens.Issue(client, element, key: System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            "otro-emisor" => InternalServiceTokens.Issue(client, element, issuer: "knock-market"),
            "otra-audiencia" => InternalServiceTokens.Issue(client, element, audience: "knock-notifications"),
            "vencido" => InternalServiceTokens.Issue(client, element, issuedAt: DateTime.UtcNow.AddMinutes(-10), lifetime: TimeSpan.FromMinutes(1)),
            "vida-larga" => InternalServiceTokens.Issue(client, element, lifetime: TimeSpan.FromMinutes(30)),
            _ => InternalServiceTokens.Issue(client, element, withJti: false),
        };

        using var response = await GetAsync(host, Query(client, element), token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ATokenWithoutThePermission_Returns403ServiceNotAllowed_R_017()
    {
        await using var host = await StartAsync();
        var client = Guid.NewGuid().ToString("D");
        var element = ObjectId.GenerateNewId().ToString();

        using var response = await GetAsync(host, Query(client, element), InternalServiceTokens.Issue(client, element, permisos: ["avisos"]));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        System.Text.Json.JsonElement.Parse(await response.Content.ReadAsStringAsync()).GetProperty("code").GetString().Should().Be("service_not_allowed");
    }

    [Fact]
    public async Task ATokenForAnotherClientOrElement_Returns403_AndReadsNothing_R_017()
    {
        await using var host = await StartAsync();
        var owner = Guid.NewGuid().ToString("D");
        var element = ObjectId.GenerateNewId().ToString();
        await SeedAsync(host, owner, element, "Mecánica", "terminada", "2026-03-10", await SeedTechnicianAsync(host, "Técnica Ana de prueba"));

        using var otherClient = await GetAsync(host, Query(owner, element), InternalServiceTokens.Issue(Guid.NewGuid().ToString("D"), element));
        using var otherElement = await GetAsync(host, Query(owner, element), InternalServiceTokens.Issue(owner, ObjectId.GenerateNewId().ToString()));

        otherClient.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        otherElement.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task WithoutAConfiguredKey_NoTokenIsAccepted_R_017()
    {
        await using var host = await Spec020Host.StartAsync();
        var client = Guid.NewGuid().ToString("D");
        var element = ObjectId.GenerateNewId().ToString();

        using var response = await GetAsync(host, Query(client, element), InternalServiceTokens.Issue(client, element));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
