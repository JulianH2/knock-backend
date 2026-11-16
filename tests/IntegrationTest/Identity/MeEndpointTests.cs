extern alias IdentityApi;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using IdentityApi::Knock.Identity.Api.Modules.Sesiones.Data;
using IdentityApi::Knock.Identity.Api.Modules.Usuarios.Data;
using Knock.Contracts.Colonias;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Identity;

/// <summary>Spec 017: <c>GET</c> y <c>PUT /api/v1/me</c> de punta a punta con MongoDB y Redis reales y el emulador de Firebase. Solo datos ficticios.</summary>
public class MeEndpointTests
{
    // Un teléfono por prueba: el emulador devuelve el mismo uid (y por tanto el mismo sub) para el mismo número, y todas las pruebas comparten
    // el mismo Redis; con números fijos la caché del perfil de una prueba contaminaría a la siguiente.
    private readonly string _phone = NewPhone();
    private readonly string _otherPhone = NewPhone();
    private static readonly Uri _meUri = new("/api/v1/me", UriKind.Relative);
    private static readonly Dictionary<string, string?> _settings = new() { ["Perfil:HostsFoto:0"] = "cdn.knock.test" };

    private sealed class FakeColonias : IColoniaCatalog
    {
        public HashSet<string> Known { get; } = ["Centro", "Tultengo"];

        public bool Down { get; set; }

        public Task<bool> ExistsAsync(string colonia, CancellationToken cancellationToken) =>
            Down ? throw new HttpRequestException("synthetic") : Task.FromResult(Known.Contains(colonia));
    }

    private static async Task<(IdentityApiFactory Factory, FakeColonias Colonias)> StartAsync()
    {
        var colonias = new FakeColonias();
        var factory = await IdentityApiFactory.CreateAsync(_settings, services => services.AddSingleton<IColoniaCatalog>(colonias));
        return (factory, colonias);
    }

    private static string NewPhone() => "+5255" + System.Security.Cryptography.RandomNumberGenerator.GetInt32(10_000_000, 100_000_000).ToString(System.Globalization.CultureInfo.InvariantCulture);

    private async Task<(string AccessToken, string Sub)> SignInAsync(HttpClient client, string? phone = null)
    {
        var (idToken, _) = await FirebaseEmulator.SignInWithPhoneAsync(phone ?? _phone);
        using var response = await AuthHttp.ExchangeAsync(client, idToken);
        response.EnsureSuccessStatusCode();
        var (access, _) = await AuthHttp.ReadSessionAsync(response);
        return (access, new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler().ReadJsonWebToken(access).GetClaim("sub").Value);
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string? token, object? body = null, string? ifNoneMatch = null)
    {
        using var request = new HttpRequestMessage(method, _meUri);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (ifNoneMatch is not null)
        {
            request.Headers.TryAddWithoutValidation("If-None-Match", ifNoneMatch);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request);
    }

    private static string ETagOf(HttpResponseMessage response) =>
        response.Headers.ETag?.ToString() ?? throw new InvalidOperationException("The response has no ETag.");

    private static Task<BsonDocument> UserAsync(IdentityApiFactory factory, string sub) =>
        factory.Users.Find(new BsonDocument(SesionesStore.SubField, sub)).SingleAsync();

    [Fact]
    public async Task BodyWithAWrongJsonType_Returns400WithoutTheInternalDotNetMessage_CA_017_06()
    {
        var (factory, _) = await StartAsync();
        using var _f = factory;
        using var client = factory.CreateClient();
        var (token, _) = await SignInAsync(client);

        using var response = await SendAsync(client, HttpMethod.Put, token, new { nombre = 123 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain("System.").And.NotContain("JSON value").And.NotContain("LineNumber", "el 400 no revela detalles internos de .NET (KNOCK-203, H-2)");
        JsonElement.Parse(raw).GetProperty("errors").TryGetProperty("nombre", out _).Should().BeTrue();
    }

    [Fact]
    public async Task NewAccount_Reads200WithItsOwnProfileAndNoPhone_CA_017_01()
    {
        var (factory, _) = await StartAsync();
        using var _f = factory;
        using var client = factory.CreateClient();
        var (token, sub) = await SignInAsync(client);

        using var response = await SendAsync(client, HttpMethod.Get, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl?.Private.Should().BeTrue();
        response.Headers.CacheControl?.NoCache.Should().BeTrue();
        response.Headers.ETag!.IsWeak.Should().BeTrue();
        var raw = await response.Content.ReadAsStringAsync();
        var body = JsonElement.Parse(raw);
        body.GetProperty("id").GetString().Should().Be(sub);
        body.GetProperty("perfilCompleto").GetBoolean().Should().BeFalse();
        body.GetProperty("estado").GetString().Should().Be("activa");
        body.GetProperty("esTecnico").GetBoolean().Should().BeFalse();
        raw.Should().NotContain(_phone).And.NotContainEquivalentOf("celular");
    }

    [Fact]
    public async Task SecondRead_IsServedFromTheCacheWithoutMongo_CA_017_02()
    {
        var (factory, _) = await StartAsync();
        using var _f = factory;
        using var client = factory.CreateClient();
        var (token, sub) = await SignInAsync(client);
        using var first = await SendAsync(client, HttpMethod.Get, token);
        (await factory.Redis.KeyExistsAsync(RedisProfileCache.Key(sub))).Should().BeTrue("la primera lectura llena la caché");
        (await factory.Redis.KeyTimeToLiveAsync(RedisProfileCache.Key(sub)))!.Value.Should().BePositive().And.BeLessThanOrEqualTo(TimeSpan.FromSeconds(60));

        // Se cambia el documento directo en MongoDB: si la segunda lectura fuera a la base, traería el nombre nuevo.
        await factory.Users.UpdateOneAsync(new BsonDocument(SesionesStore.SubField, sub), Builders<BsonDocument>.Update.Set(UsuariosStore.NameField, "Cambiado en la base"));
        using var second = await SendAsync(client, HttpMethod.Get, token);

        (await second.Content.ReadAsStringAsync()).Should().Be(await first.Content.ReadAsStringAsync(), "mismos datos, sin ir a MongoDB");
    }

    [Fact]
    public async Task ValidIfNoneMatch_Returns304WithoutBody_CA_017_03()
    {
        var (factory, _) = await StartAsync();
        using var _f = factory;
        using var client = factory.CreateClient();
        var (token, _) = await SignInAsync(client);
        using var first = await SendAsync(client, HttpMethod.Get, token);
        var etag = ETagOf(first);

        using var notModified = await SendAsync(client, HttpMethod.Get, token, ifNoneMatch: etag);
        using var stale = await SendAsync(client, HttpMethod.Get, token, ifNoneMatch: "W/\"otro\"");

        notModified.StatusCode.Should().Be(HttpStatusCode.NotModified);
        (await notModified.Content.ReadAsStringAsync()).Should().BeEmpty();
        ETagOf(notModified).Should().Be(etag);
        stale.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WithoutTokenOrWithABadOne_Returns401_CA_017_04()
    {
        var (factory, _) = await StartAsync();
        using var _f = factory;
        using var client = factory.CreateClient();
        var (token, _) = await SignInAsync(client);

        using var none = await SendAsync(client, HttpMethod.Get, null);
        using var garbage = await SendAsync(client, HttpMethod.Get, "esto-no-es-un-jwt");
        using var tampered = await SendAsync(client, HttpMethod.Get, token[..^3] + (token.EndsWith("AAA", StringComparison.Ordinal) ? "BBB" : "AAA"));
        using var putNone = await SendAsync(client, HttpMethod.Put, null, new { nombre = "Ana" });

        new[] { none, garbage, tampered, putNone }.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ValidPut_Returns200TheNextGetHasTheNewDataAndTheVersionGrows_CA_017_05()
    {
        var (factory, _) = await StartAsync();
        using var _f = factory;
        using var client = factory.CreateClient();
        var (token, sub) = await SignInAsync(client);
        using var before = await SendAsync(client, HttpMethod.Get, token);
        var versionBefore = (await UserAsync(factory, sub))[SesionesStore.VersionField].ToInt32();

        using var put = await SendAsync(client, HttpMethod.Put, token, new { nombre = "Ana Pérez", fotoUrl = "https://cdn.knock.test/fotos/ana.jpg", colonia = "Centro", codigoPostal = "42800" });
        using var after = await SendAsync(client, HttpMethod.Get, token);

        put.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonElement.Parse(await after.Content.ReadAsStringAsync());
        body.GetProperty("nombre").GetString().Should().Be("Ana Pérez");
        body.GetProperty("colonia").GetString().Should().Be("Centro");
        body.GetProperty("codigoPostal").GetString().Should().Be("42800");
        body.GetProperty("fotoUrl").GetString().Should().Be("https://cdn.knock.test/fotos/ana.jpg");
        var user = await UserAsync(factory, sub);
        user[SesionesStore.VersionField].ToInt32().Should().Be(versionBefore + 1);
        user.Contains(UsuariosStore.UpdatedAtField).Should().BeTrue();
        ETagOf(after).Should().NotBe(ETagOf(before), $"el ETag cambia con la escritura (versión en Mongo antes {versionBefore}, después {user[SesionesStore.VersionField].ToInt32()}, ETag antes {ETagOf(before)}, después {ETagOf(after)})");
    }

    [Fact]
    public async Task Put_ClearsTheCacheBeforeAnswering_CA_017_05()
    {
        var (factory, _) = await StartAsync();
        using var _f = factory;
        using var client = factory.CreateClient();
        var (token, sub) = await SignInAsync(client);
        using var read = await SendAsync(client, HttpMethod.Get, token);
        (await factory.Redis.KeyExistsAsync(RedisProfileCache.Key(sub))).Should().BeTrue();

        using var put = await SendAsync(client, HttpMethod.Put, token, new { nombre = "Ana" });

        put.StatusCode.Should().Be(HttpStatusCode.OK);
        (await factory.Redis.KeyExistsAsync(RedisProfileCache.Key(sub))).Should().BeFalse("la clave se borra antes de responder");
    }

    [Theory]
    [InlineData("nombre-corto", "nombre")]
    [InlineData("cp", "codigoPostal")]
    [InlineData("foto", "fotoUrl")]
    [InlineData("sin-nombre", "nombre")]
    public async Task InvalidFields_Return422AndChangeNothing_CA_017_06(string broken, string field)
    {
        var (factory, _) = await StartAsync();
        using var _f = factory;
        using var client = factory.CreateClient();
        var (token, sub) = await SignInAsync(client);
        var before = await UserAsync(factory, sub);
        object body = broken switch
        {
            "nombre-corto" => new { nombre = "A" },
            "cp" => new { nombre = "Ana", codigoPostal = "1234" },
            "foto" => new { nombre = "Ana", fotoUrl = "https://ejemplo.com/a.jpg" },
            _ => new { codigoPostal = "42800" },
        };

        using var response = await SendAsync(client, HttpMethod.Put, token, body);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var raw = await response.Content.ReadAsStringAsync();
        JsonElement.Parse(raw).GetProperty("errors").GetProperty(field).GetArrayLength().Should().BeGreaterThan(0);
        raw.Should().NotContain("ejemplo.com");
        (await UserAsync(factory, sub)).ToJson().Should().Be(before.ToJson(), "no cambia nada");
    }

    [Fact]
    public async Task MalformedJson_Returns400AndChangesNothing_CA_017_06()
    {
        var (factory, _) = await StartAsync();
        using var _f = factory;
        using var client = factory.CreateClient();
        var (token, sub) = await SignInAsync(client);
        var before = await UserAsync(factory, sub);
        using var request = new HttpRequestMessage(HttpMethod.Put, _meUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent("{", System.Text.Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await UserAsync(factory, sub)).ToJson().Should().Be(before.ToJson());
    }

    [Fact]
    public async Task UnknownColonia_Returns422AndChangesNothing_CA_017_07()
    {
        var (factory, _) = await StartAsync();
        using var _f = factory;
        using var client = factory.CreateClient();
        var (token, sub) = await SignInAsync(client);
        var before = await UserAsync(factory, sub);

        using var response = await SendAsync(client, HttpMethod.Put, token, new { nombre = "Ana", colonia = "Inventada" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        JsonElement.Parse(await response.Content.ReadAsStringAsync()).GetProperty("code").GetString().Should().Be("colonia_not_found");
        (await UserAsync(factory, sub)).ToJson().Should().Be(before.ToJson());
    }

    [Fact]
    public async Task ProtectedFieldsInTheBody_AreIgnored_CA_017_08()
    {
        var (factory, _) = await StartAsync();
        using var _f = factory;
        using var client = factory.CreateClient();
        var (token, sub) = await SignInAsync(client);
        var before = await UserAsync(factory, sub);

        using var response = await SendAsync(client, HttpMethod.Put, token, new
        {
            nombre = "Ana",
            sub = "otro-sub",
            celularHash = "AAAA",
            celularCifrado = "AAAA",
            esTecnico = true,
            estado = "suspendida",
            perfilCompleto = false,
            consentimientos = Array.Empty<string>(),
            creadoEn = "2000-01-01T00:00:00Z",
            version = 99,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var after = await UserAsync(factory, sub);
        foreach (var field in new[] { SesionesStore.SubField, SesionesStore.PhoneHashField, SesionesStore.PhoneCiphertextField, SesionesStore.IsTechnicianField, SesionesStore.StateField, SesionesStore.ConsentsField, SesionesStore.CreatedAtField })
        {
            after[field].Should().Be(before[field], field);
        }

        after[SesionesStore.ProfileCompleteField].AsBoolean.Should().BeTrue("solo el nombre lo completa");
        after[SesionesStore.VersionField].ToInt32().Should().Be(before[SesionesStore.VersionField].ToInt32() + 1);
    }

    [Fact]
    public async Task TwoPeople_EachReadOnlyTheirOwnProfile_CA_017_09()
    {
        var (factory, _) = await StartAsync();
        using var _f = factory;
        using var client = factory.CreateClient();
        var (tokenA, subA) = await SignInAsync(client);
        var (tokenB, subB) = await SignInAsync(client, _otherPhone);
        using var putA = await SendAsync(client, HttpMethod.Put, tokenA, new { nombre = "Persona Uno" });
        using var putB = await SendAsync(client, HttpMethod.Put, tokenB, new { nombre = "Persona Dos" });

        using var readA = await SendAsync(client, HttpMethod.Get, tokenA);
        using var readB = await SendAsync(client, HttpMethod.Get, tokenB);

        var a = JsonElement.Parse(await readA.Content.ReadAsStringAsync());
        var b = JsonElement.Parse(await readB.Content.ReadAsStringAsync());
        a.GetProperty("id").GetString().Should().Be(subA);
        a.GetProperty("nombre").GetString().Should().Be("Persona Uno");
        b.GetProperty("id").GetString().Should().Be(subB);
        b.GetProperty("nombre").GetString().Should().Be("Persona Dos");
        ETagOf(readA).Should().NotBe(ETagOf(readB));
        using var crossed = await SendAsync(client, HttpMethod.Get, tokenB, ifNoneMatch: ETagOf(readA));
        crossed.StatusCode.Should().Be(HttpStatusCode.OK, "el ETag de otra persona nunca da un 304");
    }

    [Fact]
    public async Task Logs_NeverContainTheNameOrThePhone_CA_017_10()
    {
        var (factory, _) = await StartAsync();
        using var _f = factory;
        using var client = factory.CreateClient();
        var (token, _) = await SignInAsync(client);

        var output = await ConsoleCapture.RunAsync(async () =>
        {
            using var put = await SendAsync(client, HttpMethod.Put, token, new { nombre = "Nombre Secreto", colonia = "Centro", codigoPostal = "42800" });
            using var get = await SendAsync(client, HttpMethod.Get, token);
        });

        output.Should().NotBeNullOrWhiteSpace();
        output.Should().NotContain("Nombre Secreto").And.NotContain(_phone).And.NotContain(_phone[3..]).And.NotContain(token);
    }

    [Fact]
    public async Task NewAccount_BecomesCompleteWhenItSavesItsName_CA_017_11()
    {
        var (factory, _) = await StartAsync();
        using var _f = factory;
        using var client = factory.CreateClient();
        var (token, _) = await SignInAsync(client);
        using var before = await SendAsync(client, HttpMethod.Get, token);

        using var put = await SendAsync(client, HttpMethod.Put, token, new { nombre = "Ana" });
        using var after = await SendAsync(client, HttpMethod.Get, token);

        JsonElement.Parse(await before.Content.ReadAsStringAsync()).GetProperty("perfilCompleto").GetBoolean().Should().BeFalse();
        var raw = await after.Content.ReadAsStringAsync();
        JsonElement.Parse(raw).GetProperty("perfilCompleto").GetBoolean().Should().BeTrue();
        raw.Should().NotContain(_phone).And.NotContainEquivalentOf("celular");
    }

    [Fact]
    public async Task MarketDown_Returns503AndTheProfileDoesNotChange_CA_017_12()
    {
        var (factory, colonias) = await StartAsync();
        using var _f = factory;
        using var client = factory.CreateClient();
        var (token, sub) = await SignInAsync(client);
        var before = await UserAsync(factory, sub);
        colonias.Down = true;

        using var response = await SendAsync(client, HttpMethod.Put, token, new { nombre = "Ana", colonia = "Centro" });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        JsonElement.Parse(await response.Content.ReadAsStringAsync()).GetProperty("code").GetString().Should().Be("market_unavailable");
        (await UserAsync(factory, sub)).ToJson().Should().Be(before.ToJson());
    }

    [Fact]
    public async Task AccountWithScheduledDeletion_Returns409AndChangesNothing_CA_017_13()
    {
        var (factory, _) = await StartAsync();
        using var _f = factory;
        using var client = factory.CreateClient();
        var (token, sub) = await SignInAsync(client);
        await factory.Users.UpdateOneAsync(new BsonDocument(SesionesStore.SubField, sub), Builders<BsonDocument>.Update.Set(SesionesStore.StateField, "eliminacion_programada"));
        var before = await UserAsync(factory, sub);

        using var response = await SendAsync(client, HttpMethod.Put, token, new { nombre = "Ana" });
        using var read = await SendAsync(client, HttpMethod.Get, token);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        JsonElement.Parse(await response.Content.ReadAsStringAsync()).GetProperty("code").GetString().Should().Be("deletion_scheduled");
        (await UserAsync(factory, sub)).ToJson().Should().Be(before.ToJson());
        read.StatusCode.Should().Be(HttpStatusCode.OK, "la lectura sigue permitida");
        JsonElement.Parse(await read.Content.ReadAsStringAsync()).GetProperty("estado").GetString().Should().Be("eliminacion_programada");
    }
}
