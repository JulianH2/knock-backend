extern alias IdentityApi;

using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using IdentityApi::Knock.Identity.Api.Modules.Sesiones.Data;
using IdentityApi::Knock.Identity.Api.Modules.Sesiones.Services;
using Knock.IntegrationTest.Support;
using Microsoft.IdentityModel.JsonWebTokens;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Identity;

/// <summary>Spec 015: el canje de punta a punta con el emulador de Firebase y documentdb-local. Solo celulares y datos ficticios.</summary>
public class FirebaseExchangeTests
{
    private const string _phone = "+525555550211";
    private const string _otherPhone = "+525555550212";

    private static string Forge(string uid, string? phone, string projectId = FirebaseEmulator.ProjectId, int validForMinutes = 59)
    {
        var now = DateTimeOffset.UtcNow;
        return FirebaseEmulator.ForgeUnsignedIdToken(uid, phone, projectId, now.AddMinutes(validForMinutes - 60), now.AddMinutes(validForMinutes));
    }

    private static async Task<(long Users, long Sessions)> CountsAsync(IdentityApiFactory factory) =>
        (await factory.Users.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty),
         await factory.Sessions.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));

    private static string SessionId(string accessToken) => new JsonWebTokenHandler().ReadJsonWebToken(accessToken).GetClaim("sid").Value;

    [Fact]
    public async Task NewPhone_Returns201AndStoresTheAccountConsentsAndSession_CA_015_01()
    {
        var (idToken, uid) = await FirebaseEmulator.SignInWithPhoneAsync(_phone);
        using var factory = await IdentityApiFactory.CreateAsync();
        using var client = factory.CreateClient();
        var deviceId = Guid.NewGuid().ToString("D");

        using var response = await AuthHttp.ExchangeAsync(client, idToken, deviceId);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("esNuevo").GetBoolean().Should().BeTrue();
        body.RootElement.GetProperty("perfilCompleto").GetBoolean().Should().BeFalse();
        var user = await factory.Users.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        user[SesionesStore.StateField].AsString.Should().Be("activa");
        user[SesionesStore.IsTechnicianField].AsBoolean.Should().BeFalse();
        user[SesionesStore.ProfileCompleteField].AsBoolean.Should().BeFalse();
        var consents = user[SesionesStore.ConsentsField].AsBsonArray.Select(c => c.AsBsonDocument).ToList();
        consents.Select(c => c[SesionesStore.DocumentField].AsString).Should().BeEquivalentTo("aviso_privacidad", "terminos");
        consents.Should().OnlyContain(c => c[SesionesStore.DeviceIdField].AsString == deviceId && c[SesionesStore.VersionField].AsString == "1.0");
        var session = await factory.Sessions.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        session[SesionesStore.UserIdField].AsObjectId.Should().Be(user[SesionesStore.IdField].AsObjectId);
        session[SesionesStore.DeviceIdField].AsString.Should().Be(deviceId);
        session[SesionesStore.DeviceNameField].AsString.Should().Be("Teléfono de prueba");
        session[SesionesStore.RevokedField].AsBoolean.Should().BeFalse();
        var (accessToken, _) = await AuthHttp.ReadSessionAsync(response);
        SessionId(accessToken).Should().Be(session[SesionesStore.IdField].AsObjectId.ToString(), "el sid del JWT es el id de la sesión guardada");
        new JsonWebTokenHandler().ReadJsonWebToken(accessToken).GetClaim("sub").Value.Should().Be(user[SesionesStore.SubField].AsString);
        user.ToJson().Should().NotContain(uid);
    }

    [Fact]
    public async Task ExistingPhone_Returns200AndOnlyAddsASession_CA_015_02()
    {
        var (idToken, _) = await FirebaseEmulator.SignInWithPhoneAsync(_phone);
        using var factory = await IdentityApiFactory.CreateAsync();
        using var client = factory.CreateClient();
        using var first = await AuthHttp.ExchangeAsync(client, idToken);
        var before = await CountsAsync(factory);

        using var second = await AuthHttp.ExchangeAsync(client, idToken);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("esNuevo").GetBoolean().Should().BeFalse();
        var after = await CountsAsync(factory);
        after.Users.Should().Be(before.Users, "no se crea otra cuenta");
        after.Sessions.Should().Be(before.Sessions + 1);
        var user = await factory.Users.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        user[SesionesStore.ConsentsField].AsBsonArray.Should().HaveCount(2, "no se crean otros consentimientos");
        var sub = new JsonWebTokenHandler().ReadJsonWebToken((await AuthHttp.ReadSessionAsync(second)).AccessToken).GetClaim("sub").Value;
        sub.Should().Be(user[SesionesStore.SubField].AsString);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("device")]
    [InlineData("notice")]
    [InlineData("name")]
    public async Task InvalidBody_Returns422WithFieldErrorsAndCreatesNothing_CA_015_03(string broken)
    {
        var (idToken, _) = await FirebaseEmulator.SignInWithPhoneAsync(_phone);
        using var factory = await IdentityApiFactory.CreateAsync();
        using var client = factory.CreateClient();
        object body = broken switch
        {
            "missing" => new { deviceId = Guid.NewGuid().ToString("D"), aceptaAviso = true, dispositivo = "Teléfono de prueba" },
            "device" => new { firebaseIdToken = idToken, deviceId = "no-es-uuid", aceptaAviso = true, dispositivo = "Teléfono de prueba" },
            "notice" => new { firebaseIdToken = idToken, deviceId = Guid.NewGuid().ToString("D"), aceptaAviso = false, dispositivo = "Teléfono de prueba" },
            _ => new { firebaseIdToken = idToken, deviceId = Guid.NewGuid().ToString("D"), aceptaAviso = true },
        };

        using var response = await AuthHttp.SendAsync(client, body);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var raw = await response.Content.ReadAsStringAsync();
        using var problem = JsonDocument.Parse(raw);
        problem.RootElement.GetProperty("errors").EnumerateObject().Should().NotBeEmpty();
        ProblemBody.Reasons(raw).Should().NotContain("no-es-uuid", "no repite el valor enviado");
        (await CountsAsync(factory)).Should().Be((0, 0));
    }

    [Fact]
    public async Task InvalidFirebaseToken_Returns401AndCreatesNothing_CA_015_04()
    {
        using var factory = await IdentityApiFactory.CreateAsync();
        using var client = factory.CreateClient();
        var anonymous = await FirebaseEmulator.SignInAnonymouslyAsync();
        var tokens = new[]
        {
            Forge("uid-prueba-015-vencido", _phone, validForMinutes: -120),
            Forge("uid-prueba-015-otro-proyecto", _phone, projectId: "otro-proyecto"),
            anonymous,
            "esto-no-es-un-token",
        };

        foreach (var token in tokens)
        {
            using var response = await AuthHttp.ExchangeAsync(client, token);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await response.Content.ReadAsStringAsync()).Should().NotContain("accessToken");
        }

        (await CountsAsync(factory)).Should().Be((0, 0));
    }

    [Fact]
    public async Task SuspendedAccount_Returns403WithCodeAndNoSession_CA_015_05()
    {
        var (idToken, uid) = await FirebaseEmulator.SignInWithPhoneAsync(_phone);
        using var factory = await IdentityApiFactory.CreateAsync();
        using var client = factory.CreateClient();
        using var created = await AuthHttp.ExchangeAsync(client, idToken);
        await factory.Users.UpdateOneAsync(FilterDefinition<BsonDocument>.Empty, Builders<BsonDocument>.Update.Set(SesionesStore.StateField, "suspendida"));
        var before = await CountsAsync(factory);

        using var response = await AuthHttp.ExchangeAsync(client, idToken);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var raw = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(raw);
        body.RootElement.GetProperty("code").GetString().Should().Be("account_suspended");
        body.RootElement.TryGetProperty("accessToken", out _).Should().BeFalse();
        raw.Should().NotContain(_phone).And.NotContain(uid);
        (await CountsAsync(factory)).Should().Be(before, "no se crea ninguna sesión");
    }

    [Fact]
    public async Task AccountWithScheduledDeletion_Returns200_CA_015_06()
    {
        var (idToken, _) = await FirebaseEmulator.SignInWithPhoneAsync(_phone);
        using var factory = await IdentityApiFactory.CreateAsync();
        using var client = factory.CreateClient();
        using var created = await AuthHttp.ExchangeAsync(client, idToken);
        await factory.Users.UpdateOneAsync(FilterDefinition<BsonDocument>.Empty, Builders<BsonDocument>.Update.Set(SesionesStore.StateField, "eliminacion_programada"));

        using var response = await AuthHttp.ExchangeAsync(client, idToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await factory.Users.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        user[SesionesStore.StateField].AsString.Should().Be("eliminacion_programada", "el canje no cambia el estado de la cuenta");
    }

    [Fact]
    public async Task EleventhSession_RevokesTheOldestAndKeepsTen_CA_015_07()
    {
        var (idToken, _) = await FirebaseEmulator.SignInWithPhoneAsync(_phone);
        using var factory = await IdentityApiFactory.CreateAsync();
        using var client = factory.CreateClient();
        var sessionIds = new List<string>();
        for (var i = 0; i < 11; i++)
        {
            using var response = await AuthHttp.ExchangeAsync(client, idToken);
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
            sessionIds.Add(SessionId((await AuthHttp.ReadSessionAsync(response)).AccessToken));
        }

        var sessions = await factory.Sessions.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();

        sessions.Should().HaveCount(11);
        sessions.Count(s => !s[SesionesStore.RevokedField].AsBoolean).Should().Be(10);
        var revoked = sessions.Single(s => s[SesionesStore.RevokedField].AsBoolean);
        revoked[SesionesStore.IdField].AsObjectId.ToString().Should().Be(sessionIds[0], "la revocada es la más antigua");
    }

    [Fact]
    public async Task EightSimultaneousFirstExchanges_CreateOneAccount_CA_015_08()
    {
        var (idToken, _) = await FirebaseEmulator.SignInWithPhoneAsync(_phone);
        using var factory = await IdentityApiFactory.CreateAsync();
        using var client = factory.CreateClient();

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => AuthHttp.ExchangeAsync(client, idToken)));

        try
        {
            var statuses = string.Join(", ", responses.Select(r => (int)r.StatusCode));
            responses.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(1, $"solo un canje crea la cuenta (respuestas: {statuses})");
            responses.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(7, $"respuestas: {statuses}");
            var counts = await CountsAsync(factory);
            counts.Users.Should().Be(1);
            counts.Sessions.Should().Be(8, "cada canje exitoso guarda su sesión");
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task PhoneOrUidConflict_Returns409AndCreatesNothing_CA_015_09()
    {
        var (idToken, uid) = await FirebaseEmulator.SignInWithPhoneAsync(_phone);
        using var factory = await IdentityApiFactory.CreateAsync();
        using var client = factory.CreateClient();
        using var created = await AuthHttp.ExchangeAsync(client, idToken);
        var before = await CountsAsync(factory);

        using var samePhoneOtherUid = await AuthHttp.ExchangeAsync(client, Forge("uid-prueba-015-otro", _phone));
        using var sameUidOtherPhone = await AuthHttp.ExchangeAsync(client, Forge(uid, _otherPhone));

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        foreach (var conflict in new[] { samePhoneOtherUid, sameUidOtherPhone })
        {
            conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
            var raw = await conflict.Content.ReadAsStringAsync();
            using var body = JsonDocument.Parse(raw);
            body.RootElement.GetProperty("code").GetString().Should().Be("account_conflict");
            raw.Should().NotContain("accessToken").And.NotContain(_phone).And.NotContain(_otherPhone).And.NotContain(uid);
        }

        (await CountsAsync(factory)).Should().Be(before, "ni cuentas ni sesiones nuevas");
    }

    [Fact]
    public async Task Response_AndDatabase_NeverHoldThePhoneInClearOrTheRawRefreshToken_CA_015_10()
    {
        var (idToken, uid) = await FirebaseEmulator.SignInWithPhoneAsync(_phone);
        using var factory = await IdentityApiFactory.CreateAsync();
        using var client = factory.CreateClient();

        using var response = await AuthHttp.ExchangeAsync(client, idToken);

        response.Headers.CacheControl?.NoStore.Should().BeTrue();
        response.Content.Headers.ContentEncoding.Should().BeEmpty("la respuesta con tokens no se comprime");
        var (accessToken, refreshToken) = await AuthHttp.ReadSessionAsync(response);
        var user = await factory.Users.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        user.Contains(SesionesStore.PhoneCiphertextField).Should().BeTrue();
        user.Contains(SesionesStore.PhoneHashField).Should().BeTrue();
        user.ToJson().Should().NotContain(_phone).And.NotContain("5555550211").And.NotContain(uid);
        var session = await factory.Sessions.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        session[SesionesStore.FingerprintField].AsBsonBinaryData.Bytes.Should().Equal(RefreshTokenFingerprint.Compute(refreshToken));
        session.ToJson().Should().NotContain(refreshToken).And.NotContain(accessToken);
    }

    [Fact]
    public async Task MoreRequestsThanTheAuthLimit_Returns429_CA_015_11()
    {
        await AuthRateLimitCheck.AssertFourthRequestIsLimitedAsync(client => AuthHttp.ExchangeAsync(client, "esto-no-es-un-token", remoteIp: "203.0.113.15"));
    }

    [Fact]
    public async Task Logs_NeverContainThePhoneTheUidOrTheTokens_CA_015_12()
    {
        var (idToken, uid) = await FirebaseEmulator.SignInWithPhoneAsync(_phone);
        using var factory = await IdentityApiFactory.CreateAsync();
        using var client = factory.CreateClient();
        (string AccessToken, string RefreshToken) session = default;

        var output = await ConsoleCapture.RunAsync(async () =>
        {
            using var response = await AuthHttp.ExchangeAsync(client, idToken);
            session = await AuthHttp.ReadSessionAsync(response);
            using var rejected = await AuthHttp.ExchangeAsync(client, Forge("uid-prueba-015-otro", _phone));
        });

        output.Should().NotBeNullOrWhiteSpace("la operación se registra");
        output.Should().NotContain(_phone).And.NotContain("5555550211").And.NotContain(uid)
            .And.NotContain(idToken).And.NotContain(session.AccessToken).And.NotContain(session.RefreshToken);
    }
}
