using AwesomeAssertions;
using Knock.Api.Modules.Catalog.Services;
using Knock.Api.Modules.Identity.Services;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Xunit;

namespace Knock.SecurityTest.Observability;

public class SpanPrivacyTests
{
    [Fact]
    public async Task Spans_DoNotContainPhonesTokensCoordinatesQueryOrAuthorization_TC_TT_OBS_01_08()
    {
        using var factory = SensitiveTraffic.CreateFactory();
        var bearer = await SensitiveTraffic.SendAsync(factory);

        var spans = await factory.Telemetry.WaitForServerSpansAsync(SensitiveTraffic.RequestCount);
        var resource = factory.Services.GetRequiredService<TracerProvider>().GetResource().Attributes.Select(a => $"{a.Key}={a.Value}");
        var exported = string.Join('\n', factory.Telemetry.Spans.Select(TelemetryCapture.Dump).Concat(resource));

        SensitiveTraffic.AssertNoSensitiveValues(exported, bearer);
        spans.Should().AllSatisfy(span =>
        {
            span.GetTagItem("url.query").Should().BeNull("la query no se exporta");
            span.GetTagItem("url.path").Should().BeOfType<string>().Which.Should().NotMatchRegex("[0-9]{2}\\.[0-9]{4}", "url.path es la plantilla de la ruta");
            span.TagObjects.Should().NotContain(t => t.Key.StartsWith("http.request.header.", StringComparison.Ordinal), "sin encabezados (Authorization ni Cookie)");
        });
        spans.Select(s => s.GetTagItem("url.path")).Should().Contain(["/api/v1/colonias/en-punto", "/api/v1/test/at/{latitude}/{longitude}", "/api/v1/auth/firebase"]);
    }
}

/// <summary>Peticiones con datos sensibles ficticios en ruta, query, cuerpo y encabezados (casos 08 y 09 de TT-OBS-01).</summary>
internal static class SensitiveTraffic
{
    public const int RequestCount = 4;
    public const string Phone = "15555550199";
    public const string QueryToken = "tok-SECRET-08-query";
    public const string IdToken = "eyJhbGciOiJSUzI1NiJ9.eyJmaXJlYmFzZSI6InRlc3QifQ.c3ludGhldGljLWlkLXRva2Vu";
    public const string Cookie = "cookie-SECRET-08";

    public static KnockApiFactory CreateFactory() => new(configureServices: services =>
    {
        services.RemoveAll<IFirebaseIdTokenVerifier>();
        services.AddSingleton<IFirebaseIdTokenVerifier>(new RejectingVerifier());
        services.AddSingleton<INeighborhoodLocator>(new CountingNeighborhoodLocator());
    });

    /// <summary>Envía las peticiones y devuelve el JWT propio que se usó en <c>Authorization</c>.</summary>
    public static async Task<string> SendAsync(KnockApiFactory factory)
    {
        using var client = factory.CreateClient();
        var bearer = TestHttp.IssueToken(factory, Guid.NewGuid());
        var headers = new Dictionary<string, string> { ["Cookie"] = $"session={Cookie}" };

        using var point = await TestHttp.GetAsync(client, PointLookupHttp.Path("lat=19.4326&lon=-99.1332"), headers: headers, bearerToken: bearer);
        using var route = await TestHttp.GetAsync(client, $"/api/v1/test/at/19.4326/-99.1332?phone=%2B{Phone}&token={QueryToken}", headers: headers, bearerToken: bearer);
        using var login = await TestHttp.SendAsync(
            client, HttpMethod.Post, "/api/v1/auth/firebase", headers: headers, bearerToken: bearer, jsonBody: $$"""{"firebaseIdToken":"{{IdToken}}"}""");
        using var log = await TestHttp.GetAsync(client, "/api/v1/test/log-sensitive", headers: headers, bearerToken: bearer);
        return bearer;
    }

    public static void AssertNoSensitiveValues(string exported, string bearer)
    {
        exported.Should().NotBeNullOrWhiteSpace();
        exported.Should().NotContain("19.4326", "sin coordenadas");
        exported.Should().NotContain("99.1332");
        exported.Should().NotContain(Phone, "sin celulares");
        exported.Should().NotContain("5555 0101");
        exported.Should().NotContain(bearer, "sin el JWT");
        exported.Should().NotContain("Bearer ");
        exported.Should().NotContain(IdToken, "sin el ID token de Firebase");
        exported.Should().NotContain(QueryToken, "sin la query");
        exported.Should().NotContain(Cookie, "sin la cookie");
        exported.Should().NotContain(TestController.SensitiveCurp, "sin la CURP");
    }

    private sealed class RejectingVerifier : IFirebaseIdTokenVerifier
    {
        public Task<FirebaseIdentity?> VerifyAsync(string idToken, CancellationToken cancellationToken) => Task.FromResult<FirebaseIdentity?>(null);
    }
}
