using System.Net;
using System.Text;
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

    private static string BodyWithToken(int tokenLength) => $$"""{"firebaseIdToken":"{{new string('a', tokenLength)}}"}""";

    public static TheoryData<string, string, HttpStatusCode> RejectedBodies => new()
    {
        { "empty-body", string.Empty, HttpStatusCode.BadRequest },
        { "empty-object", "{}", HttpStatusCode.BadRequest },
        { "null-token", """{"firebaseIdToken":null}""", HttpStatusCode.BadRequest },
        { "empty-token", """{"firebaseIdToken":""}""", HttpStatusCode.BadRequest },
        { "blank-token", """{"firebaseIdToken":"   "}""", HttpStatusCode.BadRequest },
        { "malformed-json", "{not json", HttpStatusCode.BadRequest },
        { "token-over-4096-chars", BodyWithToken(4097), HttpStatusCode.BadRequest },
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
