using System.Net;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Api.Modules.Identity.Services;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Knock.SecurityTest.Identity;

public class FirebaseRequestValidationTests
{
    private sealed class CountingVerifier : IFirebaseIdTokenVerifier
    {
        public int Calls { get; private set; }

        public Task<FirebaseIdentity?> VerifyAsync(string idToken, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult<FirebaseIdentity?>(null);
        }
    }

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    private static string BodyWithToken(int tokenLength) => $$"""{"firebaseIdToken":"{{new string('a', tokenLength)}}","deviceId":"3f2b8c1e-5a7d-4e9b-8c3a-1d2e3f4a5b6c","aceptaAviso":true}""";

    public static TheoryData<string, string, HttpStatusCode> RejectedBodies => new()
    {
        { "empty-body", string.Empty, HttpStatusCode.BadRequest },
        { "empty-object", "{}", HttpStatusCode.UnprocessableEntity },
        { "null-token", """{"firebaseIdToken":null}""", HttpStatusCode.UnprocessableEntity },
        { "empty-token", """{"firebaseIdToken":""}""", HttpStatusCode.UnprocessableEntity },
        { "blank-token", """{"firebaseIdToken":"   "}""", HttpStatusCode.UnprocessableEntity },
        { "malformed-json", "{not json", HttpStatusCode.BadRequest },
        { "token-over-4096-chars", BodyWithToken(4097), HttpStatusCode.UnprocessableEntity },
        { "body-over-8-kib", BodyWithToken(8300), HttpStatusCode.RequestEntityTooLarge },
        { "body-way-over-8-kib", BodyWithToken(200_000), HttpStatusCode.RequestEntityTooLarge },
    };

    [Theory]
    [MemberData(nameof(RejectedBodies))]
    public async Task InvalidOrOversizedBody_IsRejected_TC_TT_API_02_11(string scenario, string body, HttpStatusCode expected)
    {
        var verifier = new CountingVerifier();
        using var factory = new KnockApiFactory(configureServices: services => services.AddSingleton<IFirebaseIdTokenVerifier>(verifier));
        using var client = factory.CreateClient();
        using var content = Json(body);

        using var response = await client.PostAsync(AuthHttp.ExchangeUri, content);

        response.StatusCode.Should().Be(expected, scenario);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json", scenario);
        verifier.Calls.Should().Be(0, "una petición inválida no debe llegar a Firebase");
    }

    public static TheoryData<string, string, string> InvalidFields => new()
    {
        { "no-device", """{"firebaseIdToken":"t","aceptaAviso":true}""", "deviceId" },
        { "device-not-uuid", """{"firebaseIdToken":"t","deviceId":"canary-not-a-uuid","aceptaAviso":true}""", "deviceId" },
        { "device-without-hyphens", """{"firebaseIdToken":"t","deviceId":"3f2b8c1e5a7d4e9b8c3a1d2e3f4a5b6c","aceptaAviso":true}""", "deviceId" },
        { "notice-false", """{"firebaseIdToken":"t","deviceId":"3f2b8c1e-5a7d-4e9b-8c3a-1d2e3f4a5b6c","aceptaAviso":false}""", "aceptaAviso" },
        { "notice-missing", """{"firebaseIdToken":"t","deviceId":"3f2b8c1e-5a7d-4e9b-8c3a-1d2e3f4a5b6c"}""", "aceptaAviso" },
        { "no-token", """{"deviceId":"3f2b8c1e-5a7d-4e9b-8c3a-1d2e3f4a5b6c","aceptaAviso":true}""", "firebaseIdToken" },
    };

    [Theory]
    [MemberData(nameof(InvalidFields))]
    public async Task InvalidField_Returns422OnItsFieldWithoutEchoingTheValue_TC_HU_001_21(string scenario, string body, string field)
    {
        var verifier = new CountingVerifier();
        using var factory = new KnockApiFactory(configureServices: services => services.AddSingleton<IFirebaseIdTokenVerifier>(verifier));
        using var client = factory.CreateClient();
        using var content = Json(body);

        using var response = await client.PostAsync(AuthHttp.ExchangeUri, content);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity, scenario);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        response.Headers.CacheControl?.NoStore.Should().BeTrue("toda respuesta de /auth lleva no-store");
        var raw = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(raw);
        json.RootElement.GetProperty("errors").EnumerateObject().Select(e => e.Name).Should().Contain(field, scenario);
        raw.Should().NotContain("canary-not-a-uuid").And.NotContain("3f2b8c1e", "el valor enviado no se repite");
        verifier.Calls.Should().Be(0, "no se llama a Firebase ni a la base");
    }

    [Fact]
    public async Task WrongTypeValue_IsNotEchoed_TC_HU_001_21()
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();
        using var content = Json("""{"firebaseIdToken":987654321012345,"deviceId":"3f2b8c1e-5a7d-4e9b-8c3a-1d2e3f4a5b6c","aceptaAviso":true}""");

        using var response = await client.PostAsync(AuthHttp.ExchangeUri, content);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("987654321012345");
    }

    [Fact]
    public async Task TokenOfExactly4096Chars_PassesValidationAndReachesVerifier_TC_TT_API_02_11()
    {
        // Frontera: 4096 caracteres es válido para el validador (el verificador lo rechaza después); 4097 no llega.
        var verifier = new CountingVerifier();
        using var factory = new KnockApiFactory(configureServices: services => services.AddSingleton<IFirebaseIdTokenVerifier>(verifier));
        using var client = factory.CreateClient();
        using var content = Json(BodyWithToken(4096));

        using var response = await client.PostAsync(AuthHttp.ExchangeUri, content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        verifier.Calls.Should().Be(1);
    }
}
