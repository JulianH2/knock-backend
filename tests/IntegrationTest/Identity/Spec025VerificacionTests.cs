extern alias IdentityApi;

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using IdentityApi::Knock.Identity.Api.Modules.Trust.Data;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Identity;

/// <summary>Una persona con su cuenta, una sesión abierta con su dispositivo y un JWT con ese <c>sid</c>. Solo datos ficticios.</summary>
internal sealed record Spec025Person(string Sub, ObjectId UserId, string DeviceId, string Token);

/// <summary>
/// Spec 025, T-07 (versión sin fotos): <c>POST</c> y <c>GET /api/v1/me/verificacion</c> sobre MongoDB real, con seguridad negativa y registros sin CURP.
/// CURP ficticia.
/// </summary>
public sealed class Spec025VerificacionTests : IAsyncLifetime
{
    private const string _route = "/api/v1/me/verificacion";
    private const string _curp = "GODE561231HDFRRN09";
    private IdentityApiFactory _factory = null!;
    private HttpClient _client = null!;

    private IMongoCollection<BsonDocument> Verifications => _factory.Database.GetCollection<BsonDocument>(VerificationStore.VerificationsCollection);

    private IMongoCollection<BsonDocument> Consents => _factory.Database.GetCollection<BsonDocument>(VerificationStore.ConsentsCollection);

    public async Task InitializeAsync()
    {
        _factory = await IdentityApiFactory.CreateAsync();
        await _factory.Services.GetRequiredService<VerificationStore>().EnsureIndexesAsync(CancellationToken.None);
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<Spec025Person> NewPersonAsync(string state = "activa")
    {
        var sub = Guid.NewGuid().ToString("D");
        var userId = ObjectId.GenerateNewId();
        var sessionId = ObjectId.GenerateNewId();
        var deviceId = "dispositivo-" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(4));
        await _factory.Users.InsertOneAsync(new BsonDocument
        {
            ["_id"] = userId,
            ["sub"] = sub,
            ["celularHash"] = new BsonBinaryData(RandomNumberGenerator.GetBytes(32)),
            ["estado"] = state,
            ["esTecnico"] = true,
            ["perfilCompleto"] = true,
            ["creadoEn"] = new BsonDateTime(DateTime.UtcNow),
            ["version"] = 1,
        });
        await _factory.Database.GetCollection<BsonDocument>("sesiones").InsertOneAsync(new BsonDocument
        {
            ["_id"] = sessionId,
            ["usuarioId"] = userId,
            ["deviceId"] = deviceId,
            ["dispositivo"] = "Teléfono de prueba",
            ["revocada"] = false,
            ["huellaRenovacion"] = new BsonBinaryData(RandomNumberGenerator.GetBytes(32)),
            ["creadaEn"] = new BsonDateTime(DateTime.UtcNow),
        });
        return new Spec025Person(sub, userId, deviceId, AddressesHost.Token(sub, sid: sessionId.ToString()));
    }

    private static object Body(string level = "basica", bool? consent = true, string curp = _curp) =>
        new { nivel = level, aceptaBiometria = consent, versionAviso = "1.0", curp };

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string? token, object? body = null, string? raw = null)
    {
        using var request = new HttpRequestMessage(method, _route);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (raw is not null) request.Content = new StringContent(raw, Encoding.UTF8, "application/json");
        else if (body is not null) request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return await _client.SendAsync(request);
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) => JsonElement.Parse(await response.Content.ReadAsStringAsync());

    private static async Task<string?> CodeAsync(HttpResponseMessage response) =>
        (await JsonAsync(response)).TryGetProperty("code", out var code) ? code.GetString() : null;

    private async Task UnderReviewAsync(Spec025Person person, string? granted = null, string state = "en_revision") =>
        await Verifications.InsertOneAsync(new BsonDocument
        {
            ["usuarioId"] = person.Sub,
            ["nivel"] = "basica",
            ["estado"] = state,
            ["curpVigente"] = state != "no_verificada",
            ["curpHmac"] = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32)),
            ["fotos"] = new BsonArray { new BsonDocument { ["tipo"] = "ine_frente", ["estado"] = "cifrada" }, new BsonDocument { ["tipo"] = "selfie", ["estado"] = "cifrada" } },
            ["nivelOtorgado"] = granted is null ? BsonNull.Value : new BsonString(granted),
            ["motivo"] = state == "no_verificada" ? "foto_ilegible" : BsonNull.Value,
            ["creadoEn"] = new BsonDateTime(DateTime.UtcNow.AddDays(-1)),
            ["expiraEn"] = new BsonDateTime(DateTime.UtcNow.AddDays(6)),
        });

    [Fact]
    public async Task Sin_Consentimiento_Responde_422_Y_No_Crea_CA_025_01()
    {
        var ana = await NewPersonAsync();
        using var response = await SendAsync(HttpMethod.Post, ana.Token, Body(consent: false));
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await CodeAsync(response)).Should().Be("consent_required");
        (await Verifications.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(0);
        (await Consents.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(0);
    }

    [Fact]
    public async Task Registra_Consentimiento_Con_Dispositivo_De_La_Sesion_CA_025_02()
    {
        var ana = await NewPersonAsync();
        using var response = await SendAsync(HttpMethod.Post, ana.Token, Body());
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await JsonAsync(response)).GetProperty("id").GetString();
        var consent = await Consents.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        consent["usuarioId"].AsString.Should().Be(ana.Sub);
        consent["tipo"].AsString.Should().Be("biometria");
        consent["version"].AsString.Should().Be("1.0");
        consent["texto"].AsString.Should().NotBeNullOrWhiteSpace();
        consent["dispositivoId"].AsString.Should().Be(ana.DeviceId);
        consent["verificacionId"].AsObjectId.ToString().Should().Be(id);
    }

    [Fact]
    public async Task Nivel_Basico_En_Captura_Con_Dos_Fotos_CA_025_03()
    {
        var ana = await NewPersonAsync();
        using var response = await SendAsync(HttpMethod.Post, ana.Token, Body());
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        var json = await JsonAsync(response);
        json.GetProperty("estado").GetString().Should().Be("en_captura");
        json.GetProperty("fotos").EnumerateArray().Select(f => f.GetProperty("tipo").GetString()).Should().Equal("ine_frente", "selfie");
    }

    [Fact]
    public async Task Nivel_Completo_Y_Nivel_Desconocido_CA_025_04()
    {
        var ana = await NewPersonAsync();
        var bruno = await NewPersonAsync();
        using var full = await SendAsync(HttpMethod.Post, ana.Token, Body("completa"));
        using var unknown = await SendAsync(HttpMethod.Post, bruno.Token, Body("premium"));
        (await JsonAsync(full)).GetProperty("fotos").GetArrayLength().Should().Be(3);
        unknown.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var reasons = ProblemBody.Reasons(await unknown.Content.ReadAsStringAsync());
        reasons.Should().Contain("nivel").And.NotContain("premium");
    }

    [Fact]
    public async Task Curp_Mal_Formada_Responde_422_Sin_Repetirla_CA_025_05()
    {
        var ana = await NewPersonAsync();
        using var response = await SendAsync(HttpMethod.Post, ana.Token, Body(curp: "CURPFALSA123456789"));
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await CodeAsync(response)).Should().Be("curp_invalid");
        ProblemBody.Reasons(await response.Content.ReadAsStringAsync()).Should().NotContain("CURPFALSA");
        (await Verifications.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(0);
    }

    [Fact]
    public async Task Ninguna_Coleccion_Guarda_La_Curp_CA_025_06()
    {
        var ana = await NewPersonAsync();
        (await SendAsync(HttpMethod.Post, ana.Token, Body())).Dispose();
        var verification = await Verifications.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        var consent = await Consents.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        verification.ToJson().ToUpperInvariant().Should().NotContain(_curp);
        consent.ToJson().Should().NotContain(_curp);
        verification["curpVigente"].AsBoolean.Should().BeFalse();
    }

    [Fact]
    public async Task Otra_En_Revision_Responde_409_CA_025_08()
    {
        var ana = await NewPersonAsync();
        await UnderReviewAsync(ana);
        using var response = await SendAsync(HttpMethod.Post, ana.Token, Body());
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CodeAsync(response)).Should().Be("verification_under_review");
    }

    [Fact]
    public async Task Nueva_Captura_Reemplaza_La_Anterior_CA_025_08()
    {
        var ana = await NewPersonAsync();
        (await SendAsync(HttpMethod.Post, ana.Token, Body())).Dispose();
        (await SendAsync(HttpMethod.Post, ana.Token, Body("completa"))).Dispose();
        var states = (await Verifications.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).Select(d => d["estado"].AsString).ToList();
        states.Count(s => s == "en_captura").Should().Be(1);
    }

    [Fact]
    public async Task Estado_En_Revision_Sin_Direcciones_Ni_Curp_CA_025_14()
    {
        var ana = await NewPersonAsync();
        await UnderReviewAsync(ana);
        using var response = await SendAsync(HttpMethod.Get, ana.Token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await response.Content.ReadAsStringAsync();
        var json = JsonElement.Parse(raw);
        json.GetProperty("estado").GetString().Should().Be("en_revision");
        json.GetProperty("fotos").GetArrayLength().Should().Be(2);
        json.TryGetProperty("expiraEn", out var expires).Should().BeTrue();
        expires.ValueKind.Should().Be(JsonValueKind.String);
        raw.Should().NotContain("curp").And.NotContain("blob").And.NotContain("http");
    }

    [Fact]
    public async Task Sin_Ninguna_Verificacion_Es_Sin_Verificar_CA_025_14()
    {
        var ana = await NewPersonAsync();
        using var response = await SendAsync(HttpMethod.Get, ana.Token);
        var json = await JsonAsync(response);
        json.GetProperty("estado").GetString().Should().Be("sin_verificar");
        json.GetProperty("puedeReintentar").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Estado_Rechazado_Con_Motivo_Y_Consejos_CA_025_15()
    {
        var ana = await NewPersonAsync();
        await UnderReviewAsync(ana, state: "no_verificada");
        var json = await JsonAsync(await SendAsync(HttpMethod.Get, ana.Token));
        json.GetProperty("estado").GetString().Should().Be("no_verificada");
        json.GetProperty("motivo").GetString().Should().Be("foto_ilegible");
        json.GetProperty("consejos").GetArrayLength().Should().BeInRange(1, 3);
        json.GetProperty("puedeReintentar").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Sin_Token_401_Y_No_Ve_La_De_Otra_Persona_CA_025_17()
    {
        var ana = await NewPersonAsync();
        var bruno = await NewPersonAsync();
        await UnderReviewAsync(ana);
        using var anonymousGet = await SendAsync(HttpMethod.Get, null);
        using var anonymousPost = await SendAsync(HttpMethod.Post, null, Body());
        using var other = await SendAsync(HttpMethod.Get, bruno.Token);
        anonymousGet.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        anonymousPost.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await JsonAsync(other)).GetProperty("estado").GetString().Should().Be("sin_verificar");
    }

    [Fact]
    public async Task Registros_Sin_Curp_CA_025_18()
    {
        var ana = await NewPersonAsync();
        var output = await ConsoleCapture.RunAsync(async () =>
        {
            (await SendAsync(HttpMethod.Post, ana.Token, Body())).Dispose();
            (await SendAsync(HttpMethod.Post, ana.Token, Body(curp: "CURPFALSA123456789"))).Dispose();
            (await SendAsync(HttpMethod.Get, ana.Token)).Dispose();
        });
        output.Should().NotBeNullOrWhiteSpace();
        output.Should().NotContain(_curp).And.NotContain("CURPFALSA");
    }

    [Fact]
    public async Task Ya_Verificado_Responde_409_CA_025_19()
    {
        var ana = await NewPersonAsync();
        var bruno = await NewPersonAsync();
        await UnderReviewAsync(ana, granted: "basica", state: "verificada");
        await UnderReviewAsync(bruno, granted: "completa", state: "verificada");
        using var same = await SendAsync(HttpMethod.Post, ana.Token, Body("basica"));
        using var full = await SendAsync(HttpMethod.Post, bruno.Token, Body("completa"));
        (await CodeAsync(same)).Should().Be("already_verified");
        (await CodeAsync(full)).Should().Be("already_fully_verified");
    }

    [Fact]
    public async Task Eliminacion_Programada_409_Y_Cuerpo_Grande_413_CA_025_21()
    {
        var leaving = await NewPersonAsync("eliminacion_programada");
        var ana = await NewPersonAsync();
        using var scheduled = await SendAsync(HttpMethod.Post, leaving.Token, Body());
        using var tooLarge = await SendAsync(HttpMethod.Post, ana.Token, raw: "{\"nivel\":\"basica\",\"curp\":\"" + new string('A', 9000) + "\"}");
        scheduled.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CodeAsync(scheduled)).Should().Be("deletion_scheduled");
        tooLarge.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        (await Verifications.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(0);
    }
}
