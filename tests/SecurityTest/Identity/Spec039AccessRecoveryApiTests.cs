using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.SecurityTest.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.SecurityTest.Identity;

/// <summary>Spec 039: <c>POST /api/v1/recuperacion-acceso</c> (USR-05, público). Host real en Production, MongoDB y Redis reales. Solo datos ficticios.</summary>
public class Spec039AccessRecoveryApiTests
{
    private const string _path = "/api/v1/recuperacion-acceso";

    /// <summary>IP distinta en cada llamada: el límite de 3 por hora es por IP y el host de pruebas es compartido.</summary>
    private static string NewIp() => "10." + string.Join('.', RandomNumberGenerator.GetBytes(3).Select(b => b.ToString(CultureInfo.InvariantCulture)));

    private static IMongoCollection<BsonDocument> Recoveries(SecurityHosts hosts) => hosts.Identity.Database.GetCollection<BsonDocument>("recuperaciones");

    private static Task<HttpResponseMessage> RecoverAsync(SecurityHosts hosts, string token, string previousPhone, string type = "cliente", string? ip = null) =>
        SecureHttp.PostJsonAsync(hosts.IdentityClient, _path, new { firebaseIdToken = token, celularAnterior = previousPhone, tipo = type }, remoteIp: ip ?? NewIp());

    [Fact]
    public async Task ExistingAccount_Returns202WithoutSessionAndLeavesAPendingCase_CA_039_11()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var before = await hosts.Identity.Users.Find(new BsonDocument("sub", person.Sub)).SingleAsync();
        var newPhone = SecurityIdentityFactory.NewPhone();
        var token = hosts.Identity.Firebase.Issue("uid-nuevo-" + Guid.NewGuid().ToString("N"), newPhone);

        using var response = await RecoverAsync(hosts, token, person.Phone);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await response.Content.ReadAsStringAsync()).Should().NotContain(newPhone).And.NotContain(person.Phone);
        var cases = await Recoveries(hosts).Find(new BsonDocument("usuarioId", before["_id"])).ToListAsync();
        var recoveryCase = cases.Should().ContainSingle().Subject;
        recoveryCase["estado"].AsString.Should().Be("pendiente");
        recoveryCase["tipo"].AsString.Should().Be("cliente");
        recoveryCase.Contains("celularNuevoProtegido").Should().BeTrue();
        recoveryCase.ToJson().Should().NotContain(newPhone, "el celular nuevo se guarda protegido");
        (await hosts.Identity.Users.Find(new BsonDocument("sub", person.Sub)).SingleAsync()).ToJson().Should().Be(before.ToJson(), "no cambia ningún dato hasta que un verificador la apruebe");
    }

    [Fact]
    public async Task ExistingAndUnknownAccounts_GetTheSameAnswer_AndTheFourthRequestInAnHourIsLimited_CA_039_34()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        string Token() => hosts.Identity.Firebase.Issue("uid-nuevo-" + Guid.NewGuid().ToString("N"), SecurityIdentityFactory.NewPhone());

        using var known = await RecoverAsync(hosts, Token(), person.Phone);
        using var unknown = await RecoverAsync(hosts, Token(), SecurityIdentityFactory.NewPhone());

        known.StatusCode.Should().Be(HttpStatusCode.Accepted);
        unknown.StatusCode.Should().Be(known.StatusCode);
        (await unknown.Content.ReadAsStringAsync()).Should().Be(await known.Content.ReadAsStringAsync());
        unknown.Headers.Select(h => h.Key).Order().Should().Equal(known.Headers.Select(h => h.Key).Order());
        (await Recoveries(hosts).CountDocumentsAsync(new BsonDocument("tipo", "cliente"))).Should().BeGreaterThan(0);

        var ip = NewIp();
        for (var attempt = 0; attempt < 3; attempt++)
        {
            using var allowed = await RecoverAsync(hosts, Token(), SecurityIdentityFactory.NewPhone(), ip: ip);
            allowed.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        using var limited = await RecoverAsync(hosts, Token(), SecurityIdentityFactory.NewPhone(), ip: ip);
        await SecureHttp.AssertProblemAsync(limited, HttpStatusCode.TooManyRequests);
        int.Parse(limited.Headers.GetValues("Retry-After").Single(), CultureInfo.InvariantCulture).Should().BeInRange(1, 3600);
    }

    [Fact]
    public async Task TheFourthRequestForTheSamePreviousNumber_IsLimitedEvenFromAnotherIp_CA_039_34()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        string Token() => hosts.Identity.Firebase.Issue("uid-nuevo-" + Guid.NewGuid().ToString("N"), SecurityIdentityFactory.NewPhone());
        for (var attempt = 0; attempt < 3; attempt++)
        {
            using var allowed = await RecoverAsync(hosts, Token(), person.Phone);
            allowed.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        using var limited = await RecoverAsync(hosts, Token(), person.Phone);

        await SecureHttp.AssertProblemAsync(limited, HttpStatusCode.TooManyRequests, person.Phone);
        limited.Headers.Contains("Retry-After").Should().BeTrue();
    }

    [Theory]
    [InlineData("", "+525512345678", "cliente", "firebaseIdToken")]
    [InlineData("tok", "5512345678", "cliente", "celularAnterior")]
    [InlineData("tok", "+525512345678", "admin", "tipo")]
    [InlineData("tok", "+525512345678", "", "tipo")]
    public async Task InvalidField_Returns422WithFieldErrors_CA_039_12(string token, string previous, string type, string field)
    {
        var hosts = await SecurityHosts.SharedAsync();

        using var response = await RecoverAsync(hosts, token, previous, type);

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "+525512345678");
        (await SecureHttp.ReadJsonAsync(response)).GetProperty("errors").TryGetProperty(field, out _).Should().BeTrue();
    }

    [Fact]
    public async Task UnverifiableFirebaseToken_Returns422AndCreatesNoCase_CA_039_12()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var person = await hosts.Identity.SignInAsync(hosts.IdentityClient);
        var user = await hosts.Identity.Users.Find(new BsonDocument("sub", person.Sub)).SingleAsync();

        using var response = await RecoverAsync(hosts, "not-a-firebase-token", person.Phone);

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "not-a-firebase-token");
        (await Recoveries(hosts).CountDocumentsAsync(new BsonDocument("usuarioId", user["_id"]))).Should().Be(0);
    }

    [Fact]
    public async Task UnreadableBody_Returns400_CA_039_13()
    {
        var hosts = await SecurityHosts.SharedAsync();

        using var response = await SecureHttp.SendRawJsonAsync(hosts.IdentityClient, HttpMethod.Post, _path, "{\"firebaseIdToken\":", remoteIp: NewIp());

        await SecureHttp.AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }
}
