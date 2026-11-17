using System.Net;
using System.Text;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Knock.SecurityTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.SecurityTest.Identity;

/// <summary>Spec 017: <c>GET</c> y <c>PUT /api/v1/me</c>. Host real en Production, MongoDB y Redis reales, catálogo de colonias doblado.</summary>
public class ProfileSecurityTests
{
    private const string _me = "/api/v1/me";

    private static Task<HttpResponseMessage> PutAsync(SecurityHosts hosts, string token, object body) =>
        SecureHttp.SendAsync(hosts.IdentityClient, HttpMethod.Put, _me, System.Net.Http.Json.JsonContent.Create(body), token);

    private static Task<HttpResponseMessage> GetAsync(SecurityHosts hosts, string token) =>
        SecureHttp.SendAsync(hosts.IdentityClient, HttpMethod.Get, _me, bearer: token);

    private static async Task<BsonDocument> UserAsync(SecurityHosts hosts, string sub) =>
        await hosts.Identity.Users.Find(new BsonDocument("sub", sub)).SingleAsync();

    [Fact]
    public async Task EachPersonReadsOnlyTheirOwnProfile_EvenWithTheCachePrimed_SEC_017_01()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var a = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var b = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        (await PutAsync(hosts, a.AccessToken, new { nombre = "Alicia Aguirre" })).Dispose();

        (await PutAsync(hosts, b.AccessToken, new { nombre = "Beto Bernal" })).Dispose();

        using var aFirst = await GetAsync(hosts, a.AccessToken);
        using var aCached = await GetAsync(hosts, a.AccessToken);
        using var bRead = await GetAsync(hosts, b.AccessToken);

        (await aCached.Content.ReadAsStringAsync()).Should().Contain("Alicia").And.NotContain("Beto").And.NotContain(b.Sub);
        var raw = await bRead.Content.ReadAsStringAsync();
        raw.Should().Contain("Beto").And.NotContain("Alicia").And.NotContain(a.Sub).And.NotContain(b.Phone);
    }

    [Fact]
    public async Task AnIdentifierInTheRouteQueryHeadersOrBody_NeverSelectsAnotherPerson_SEC_017_02()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var a = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var b = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        (await PutAsync(hosts, b.AccessToken, new { nombre = "Beto Bernal" })).Dispose();

        using var byRoute = await SecureHttp.SendAsync(hosts.IdentityClient, HttpMethod.Get, $"{_me}/{b.Sub}", bearer: a.AccessToken);
        using var byQuery = await SecureHttp.SendAsync(hosts.IdentityClient, HttpMethod.Get, $"{_me}?sub={b.Sub}&id={b.Sub}", bearer: a.AccessToken);
        using var byHeader = new HttpRequestMessage(HttpMethod.Get, new Uri(_me, UriKind.Relative));
        byHeader.Headers.TryAddWithoutValidation("Authorization", "Bearer " + a.AccessToken);
        byHeader.Headers.TryAddWithoutValidation("X-User-Id", b.Sub);
        byHeader.Headers.TryAddWithoutValidation("X-Sub", b.Sub);
        using var headerResponse = await hosts.IdentityClient.SendAsync(byHeader);
        using var byBody = await PutAsync(hosts, a.AccessToken, new { nombre = "Alicia Aguirre", sub = b.Sub, id = b.Sub });

        (await byRoute.Content.ReadAsStringAsync()).Should().NotContain("Beto");
        foreach (var response in new[] { byQuery, headerResponse, byBody })
        {
            var raw = await response.Content.ReadAsStringAsync();
            raw.Should().NotContain("Beto").And.NotContain(b.Sub);
        }

        (await UserAsync(hosts, b.Sub))["nombre"].AsString.Should().Be("Beto Bernal");
        (await UserAsync(hosts, a.Sub))["nombre"].AsString.Should().Be("Alicia Aguirre");
    }

    [Fact]
    public async Task ProtectedFieldsInThePutBody_AreIgnored_AndNoNewFieldsAreStored_SEC_017_03()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var a = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var before = await UserAsync(hosts, a.Sub);

        using var response = await SecureHttp.SendRawJsonAsync(hosts.IdentityClient, HttpMethod.Put, _me, """
            {"nombre":"Alicia Aguirre","sub":"x","id":"x","celular":"+520000000000","celularHash":"AAAA","celularCifrado":"AAAA","estado":"suspendida",
             "esTecnico":true,"perfilCompleto":false,"consentimientos":[],"creadoEn":"2000-01-01T00:00:00Z","version":999,"roles":["admin"],"role":"admin","isAdmin":true}
            """, a.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var after = await UserAsync(hosts, a.Sub);
        foreach (var field in new[] { "sub", "celularHash", "celularCifrado", "estado", "esTecnico", "consentimientos", "creadoEn" })
        {
            after[field].Should().Be(before[field], $"{field} no se puede cambiar por /me");
        }

        after["perfilCompleto"].AsBoolean.Should().BeTrue();
        after["version"].ToInt32().Should().Be(before["version"].ToInt32() + 1);
        after.Names.Should().NotContain(["roles", "role", "isAdmin", "celular", "id"]);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(a.Phone).And.NotContainEquivalentOf("celular");
    }

    [Theory]
    [InlineData("Robert'); DROP TABLE usuarios;--")]
    [InlineData("{\"$gt\":\"\"}")]
    [InlineData("$where: 1")]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("Ana\r\nSet-Cookie: a=b")]
    [InlineData("Ana\nPérez")]
    [InlineData("Ana\0")]
    [InlineData("Ana\a")]
    [InlineData("Ana‮Pérez")]
    [InlineData("Ana​Pérez")]
    [InlineData("A")]
    [InlineData("1234")]
    [InlineData("")]
    public async Task HostileName_Returns422AndChangesNothing_SEC_017_04(string name)
    {
        var hosts = await SecurityHosts.SharedAsync();
        var a = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var before = await UserAsync(hosts, a.Sub);

        using var response = await PutAsync(hosts, a.AccessToken, new { nombre = name });

        var raw = await SecureHttp.AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        raw.Should().NotContain("DROP TABLE").And.NotContain("<script>").And.NotContain("onerror");
        (await UserAsync(hosts, a.Sub)).ToJson().Should().Be(before.ToJson());
    }

    [Theory]
    [InlineData("nombre", "A")]
    [InlineData("codigoPostal", "1234")]
    [InlineData("fotoUrl", "https://evil.example/a.png")]
    public async Task InvalidProfileField_Returns422WithFieldErrorsAndChangesNothing_CA_017_06(string field, string value)
    {
        var hosts = await SecurityHosts.SharedAsync();
        var a = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var before = await UserAsync(hosts, a.Sub);
        var body = new Dictionary<string, string> { ["nombre"] = "Alicia Aguirre", [field] = value };

        using var response = await PutAsync(hosts, a.AccessToken, body);

        var raw = await SecureHttp.AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        using var json = System.Text.Json.JsonDocument.Parse(raw);
        json.RootElement.GetProperty("errors").GetProperty(field).GetArrayLength().Should().BeGreaterThan(0);
        (await UserAsync(hosts, a.Sub)).ToJson().Should().Be(before.ToJson());
    }

    [Theory]
    [InlineData("colonia", "Centro'; DROP TABLE colonias;--")]
    [InlineData("colonia", "{\"$ne\":null}")]
    [InlineData("colonia", "<b>Centro</b>")]
    [InlineData("colonia", "Centro\r\nX-Injected: 1")]
    [InlineData("codigoPostal", "1234")]
    [InlineData("codigoPostal", "123456")]
    [InlineData("codigoPostal", "12a45")]
    [InlineData("codigoPostal", "١٢٣٤٥")]
    [InlineData("codigoPostal", "12345; DROP")]
    [InlineData("fotoUrl", "http://cdn.knock.test/a.png")]
    [InlineData("fotoUrl", "https://evil.example/a.png")]
    [InlineData("fotoUrl", "https://cdn.knock.test@evil.example/a.png")]
    [InlineData("fotoUrl", "https://cdn.knock.test.evil.example/a.png")]
    [InlineData("fotoUrl", "https://evil.example/?u=cdn.knock.test")]
    [InlineData("fotoUrl", "javascript:alert(1)")]
    [InlineData("fotoUrl", "data:text/html;base64,PHNjcmlwdD4=")]
    [InlineData("fotoUrl", "//cdn.knock.test/a.png")]
    [InlineData("fotoUrl", "https://127.0.0.1/a.png")]
    [InlineData("fotoUrl", "https://cdn.knock.tеst/a.png")]
    public async Task HostileOptionalField_Returns422AndChangesNothing_SEC_017_05(string field, string value)
    {
        var hosts = await SecurityHosts.SharedAsync();
        var a = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var before = await UserAsync(hosts, a.Sub);
        var body = new Dictionary<string, string> { ["nombre"] = "Alicia Aguirre", [field] = value };

        using var response = await PutAsync(hosts, a.AccessToken, body);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("DROP TABLE").And.NotContain("<b>").And.NotContain("stackTrace");
        (await UserAsync(hosts, a.Sub)).ToJson().Should().Be(before.ToJson());
    }

    [Fact]
    public async Task PhotoFromTheConfiguredHost_IsStoredWithoutControlCharacters_SEC_017_06()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var a = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        using var response = await PutAsync(hosts, a.AccessToken, new { nombre = "Alicia Aguirre", fotoUrl = "https://cdn.knock.test/x.png\r\nX-Injected: 1" });

        var stored = (await UserAsync(hosts, a.Sub)).GetValue("fotoUrl", BsonNull.Value);
        if (!stored.IsBsonNull)
        {
            stored.AsString.Should().NotContainAny("\r", "\n", "\0");
        }

        ((int)response.StatusCode).Should().BeLessThan(500);
    }

    [Theory]
    [InlineData("""{"nombre":123}""")]
    [InlineData("""{"nombre":["Ana"]}""")]
    [InlineData("""{"nombre":{"$ne":null}}""")]
    [InlineData("""{"nombre":"Ana","codigoPostal":12345}""")]
    [InlineData("""{"nombre":"Ana","colonia":true}""")]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("")]
    public async Task WrongTypesOrBrokenJson_AreRejectedAs4xxNeverAs500_SEC_017_07(string json)
    {
        var hosts = await SecurityHosts.SharedAsync();
        var a = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        using var response = await SecureHttp.SendRawJsonAsync(hosts.IdentityClient, HttpMethod.Put, _me, json, a.AccessToken);

        ((int)response.StatusCode).Should().BeInRange(400, 499);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("$ne").And.NotContain("stackTrace");
    }

    [Fact]
    public async Task MalformedJson_Returns400AndChangesNothing_CA_017_06()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var a = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var before = await UserAsync(hosts, a.Sub);

        using var response = await SecureHttp.SendRawJsonAsync(hosts.IdentityClient, HttpMethod.Put, _me, "{", a.AccessToken);

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest);
        (await UserAsync(hosts, a.Sub)).ToJson().Should().Be(before.ToJson());
    }

    [Fact]
    public async Task WrongJsonType_Returns400WithoutFrameworkTypeNames_SEC_017_16()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var a = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        using var response = await SecureHttp.SendRawJsonAsync(hosts.IdentityClient, HttpMethod.Put, _me, """{"nombre":123}""", a.AccessToken);

        (await response.Content.ReadAsStringAsync()).Should().NotContain("System.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PutBodyOver4KiB_Returns413_SEC_017_08(bool withoutContentLength)
    {
        var hosts = await SecurityHosts.SharedAsync();
        var a = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var json = $$"""{"nombre":"{{new string('a', 5000)}}"}""";
        using HttpContent content = withoutContentLength ? new UnknownLengthContent(json) : new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await SecureHttp.SendAsync(hosts.IdentityClient, HttpMethod.Put, _me, content, a.AccessToken);

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.RequestEntityTooLarge);
    }

    [Theory]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    [InlineData("POST")]
    public async Task OtherMethods_Return405_SEC_017_09(string method)
    {
        var hosts = await SecurityHosts.SharedAsync();

        using var response = await SecureHttp.SendAsync(hosts.IdentityClient, new HttpMethod(method), _me, bearer: TokenForge.Valid());

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task PutWithANonJsonContentType_Returns415_SEC_017_10()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var a = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        using var content = new StringContent("nombre=Ana", Encoding.UTF8, "application/x-www-form-urlencoded");

        using var response = await SecureHttp.SendAsync(hosts.IdentityClient, HttpMethod.Put, _me, content, a.AccessToken);

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task ETagOfOnePerson_NeverProducesA304ForAnother_AndDoesNotRevealTheSub_SEC_017_11()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var a = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var b = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        using var aRead = await GetAsync(hosts, a.AccessToken);
        var etag = aRead.Headers.ETag!.ToString();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_me, UriKind.Relative));
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + b.AccessToken);
        request.Headers.TryAddWithoutValidation("If-None-Match", etag);

        using var bRead = await hosts.IdentityClient.SendAsync(request);

        bRead.StatusCode.Should().Be(HttpStatusCode.OK);
        etag.Should().NotContain(a.Sub).And.NotContain(a.Sub[..8]);
        aRead.Headers.CacheControl!.Private.Should().BeTrue();
        aRead.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle();
    }

    [Fact]
    public async Task PutResponseWithPersonalData_IsNoStore_AndGetCarriesNoPhone_SEC_017_12()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var a = await hosts.Identity.SignInAsync(hosts.IdentityClient);

        using var put = await PutAsync(hosts, a.AccessToken, new { nombre = "Alicia Aguirre" });
        using var get = await GetAsync(hosts, a.AccessToken);

        put.Headers.CacheControl!.NoStore.Should().BeTrue();
        foreach (var response in new[] { put, get })
        {
            (await response.Content.ReadAsStringAsync()).Should().NotContain(a.Phone).And.NotContain(a.Phone[3..]);
        }
    }

    [Fact]
    public async Task ValidTokenOfAnUnknownAccount_Returns404WithoutEchoingTheSubject_SEC_017_13()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var sub = TokenForge.Sub();

        using var response = await GetAsync(hosts, TokenForge.Valid(sub));

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.NotFound, sub);
    }

    [Fact]
    public async Task Logs_NeverContainNamePhoneOrSubject_OnReadsWritesAndErrors_SEC_017_14()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var a = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        const string name = "Zacarías Quintanilla";

        var output = await ConsoleCapture.RunAsync(async () =>
        {
            using var put = await PutAsync(hosts, a.AccessToken, new { nombre = name, colonia = "Colonia Inexistente" });
            using var ok = await PutAsync(hosts, a.AccessToken, new { nombre = name });
            using var bad = await PutAsync(hosts, a.AccessToken, new { nombre = "Zacarías <script>" });
            using var get = await GetAsync(hosts, a.AccessToken);
        });

        output.Should().NotBeNullOrWhiteSpace();
        output.Should().NotContain("Zacarías").And.NotContain("Quintanilla").And.NotContain(a.Phone).And.NotContain(a.Phone[3..])
            .And.NotContain(a.Sub).And.NotContain(a.AccessToken).And.NotContain("Colonia Inexistente");
    }

    [Fact]
    public async Task GlobalLimit_Returns429WithRetryAfterOnMe_AndOtherIpsAreUnaffected_SEC_017_15()
    {
        using var factory = await SecurityIdentityFactory.CreateAsync(new Dictionary<string, string?>
        {
            ["RateLimiting:Global:PermitLimit"] = "3",
            ["RateLimiting:Global:Window"] = "00:01:00",
        });
        using var client = factory.CreateClient();
        var token = TokenForge.Valid();
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
        {
            using var response = await SecureHttp.SendAsync(client, HttpMethod.Get, _me, bearer: token, remoteIp: "203.0.113.70");
            statuses.Add(response.StatusCode);
            if (i == 3)
            {
                await SecureHttp.AssertProblemAsync(response, HttpStatusCode.TooManyRequests, "203.0.113.70");
                int.Parse(response.Headers.GetValues("Retry-After").Single(), System.Globalization.CultureInfo.InvariantCulture).Should().BeInRange(1, 60);
            }
        }

        statuses.Take(3).Should().OnlyContain(s => s != HttpStatusCode.TooManyRequests);
        using var other = await SecureHttp.SendAsync(client, HttpMethod.Get, _me, bearer: token, remoteIp: "203.0.113.71");
        other.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
    }
}
