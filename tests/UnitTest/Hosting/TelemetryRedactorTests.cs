using System.Diagnostics;
using AwesomeAssertions;
using Knock.Api.Hosting;
using Xunit;

namespace Knock.UnitTest.Hosting;

public class TelemetryRedactorTests
{
    private const string _jwt = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.abc_DEF-123";

    [Theory]
    [InlineData("call +52 55 5555 0101 now", "5555 0101")]
    [InlineData("phone +525555550101", "5555550101")]
    [InlineData("test +15555550199", "5555550199")]
    [InlineData("query phone=%2B15555550199", "15555550199")]
    [InlineData("token " + _jwt, "eyJ")]
    [InlineData("Authorization: Bearer abc.def-ghi", "abc.def-ghi")]
    [InlineData("authorization: bearer abc.def-ghi", "bearer ")]
    [InlineData("curp XEXX010101HNEXXXA4 found", "XEXX010101HNEXXXA4")]
    [InlineData("near 19.4326, -99.1332", "19.4326")]
    [InlineData("route /at/19.4326/-99.1332", "99.1332")]
    [InlineData("point 19.4326 -99.1332", "99.1332")]
    [InlineData("point 19.4326;-99.1332", "19.4326")]
    [InlineData("maps 19.4326,-99.1332", "19.4326")]
    [InlineData("maps 19.4326,-99.1332", "99.1332")]
    [InlineData("lat=19.43&lon=-99.13", "19.43")]
    [InlineData("lat=19.43&lon=-99.13", "99.13")]
    [InlineData("?token=abc123&code=777&sig=xyz", "abc123")]
    [InlineData("?token=abc123&code=777&sig=xyz", "777")]
    [InlineData("?token=abc123&code=777&sig=xyz", "xyz")]
    [InlineData("{\"idToken\":\"tok-1\"}", "tok-1")]
    [InlineData("refreshToken=r-secret-1", "r-secret-1")]
    [InlineData("celular=5555123456", "5555123456")]
    [InlineData("curp xexx010101hnexxxa4 found", "xexx010101hnexxxa4")]
    [InlineData("curp=XEXX010101HNEXXXA4", "XEXX010101HNEXXXA4")]
    [InlineData("accessToken=a-secret-1", "a-secret-1")]
    [InlineData("?id_token=i-secret-1&x=1", "i-secret-1")]
    [InlineData("access_token: a-secret-2", "a-secret-2")]
    [InlineData("{\"refresh_token\":\"r-secret-2\"}", "r-secret-2")]
    [InlineData("password=p-secret-1", "p-secret-1")]
    [InlineData("secret: s-secret-1", "s-secret-1")]
    [InlineData("Cookie: session=c-secret-1; theme=c-secret-2", "c-secret-1")]
    [InlineData("Cookie: session=c-secret-1; theme=c-secret-2", "c-secret-2")]
    [InlineData("Set-Cookie: sid=c-secret-3; Path=/; HttpOnly", "c-secret-3")]
    public void Redact_KnownSensitivePatternsAndKeys_LeavesOthersIntact_TC_TT_OBS_01_12(string input, string secret)
    {
        var redacted = TelemetryRedactor.Redact(input);

        redacted.Should().NotContain(secret);
        redacted.Should().Contain(TelemetryRedactor.Placeholder);
    }

    [Theory]
    [InlineData("HTTP GET /api/v1/oficios responded 200 in 12.3456 ms")]
    [InlineData("HTTP GET /api/v1/test/at/{latitude}/{longitude} responded 500 in 0.4123 ms")]
    [InlineData("elapsed 12.3456")]
    [InlineData("no separator 19.4326-99.1332")]
    [InlineData("StatusCode=500 errorCode=42")]
    [InlineData("3f2504e0-4f89-11d3-9a0c-0305e82c3301")]
    [InlineData("OpenTelemetry 1.20.1 on net10.0")]
    [InlineData("build 1.20251102.1234")]
    [InlineData("")]
    public void Redact_TextWithoutSensitiveData_IsUnchanged_TC_TT_OBS_01_12(string input) =>
        TelemetryRedactor.Redact(input).Should().Be(input);

    [Theory]
    [InlineData("lat", 19.4326)]
    [InlineData("LON", -99.1332)]
    [InlineData("Phone", "5555")]
    [InlineData("idToken", "tok")]
    [InlineData("authorization", "anything")]
    [InlineData("sig", "abc")]
    [InlineData("curp", "XEXX010101HNEXXXA4")]
    [InlineData("cookie", "session=abc")]
    [InlineData("Set-Cookie", "sid=abc")]
    [InlineData("accessToken", "tok")]
    [InlineData("access_token", "tok")]
    [InlineData("id_token", "tok")]
    [InlineData("refresh_token", "tok")]
    [InlineData("password", "p")]
    [InlineData("secret", "s")]
    public void RedactAttribute_KnownKey_IsAlwaysReplaced_TC_TT_OBS_01_12(string key, object value) =>
        TelemetryRedactor.RedactAttribute(key, value).Should().Be(TelemetryRedactor.Placeholder);

    [Theory]
    [InlineData("StatusCode", 500)]
    [InlineData("Elapsed", 12.3456)]
    [InlineData("RequestPath", "/api/v1/oficios")]
    public void RedactAttribute_OtherKeys_KeepTheirValue_TC_TT_OBS_01_12(string key, object value) =>
        TelemetryRedactor.RedactAttribute(key, value).Should().Be(value);

    [Fact]
    public void RedactAttribute_TextContainers_AreRedacted_TC_TT_OBS_01_12()
    {
        // Fija qué tipos se redactan: un tipo nuevo que pueda llevar texto debe agregarse a RedactAttribute y aquí.
        const string phone = "+52 55 5555 0101";

        TelemetryRedactor.RedactAttribute("note", phone).Should().Be(TelemetryRedactor.Placeholder);
        TelemetryRedactor.RedactAttribute("notes", new[] { phone, "ok" }).Should().BeEquivalentTo(new[] { TelemetryRedactor.Placeholder, "ok" });
        TelemetryRedactor.RedactAttribute("items", new object[] { phone, 42 }).Should().BeEquivalentTo(new object[] { TelemetryRedactor.Placeholder, 42 });
        TelemetryRedactor.RedactAttribute("list", new List<string> { phone }).Should().BeEquivalentTo(new[] { TelemetryRedactor.Placeholder });
        TelemetryRedactor.RedactAttribute("count", 42).Should().Be(42);
        TelemetryRedactor.RedactAttribute("flag", true).Should().Be(true);
    }

    [Fact]
    public void RedactUrl_RemovesEveryQueryValue_KeepsNames_TC_TT_OBS_01_12()
    {
        var url = TelemetryRedactor.RedactUrl("https://knock.blob.core.windows.net/files/a.jpg?sig=abc%2Bdef&se=2026-11-02&token=t1");

        url.Should().Be("https://knock.blob.core.windows.net/files/a.jpg?sig=Redacted&se=Redacted&token=Redacted");
        TelemetryRedactor.RedactUrl("https://example.test/path").Should().Be("https://example.test/path");
    }

    [Theory]
    [InlineData("+5")]
    [InlineData("+5 ")]
    [InlineData("eyJa")]
    [InlineData("eyJa.")]
    [InlineData("1.11111 ")]
    [InlineData("11111.1111")]
    [InlineData("token=")]
    [InlineData("Bearer ")]
    [InlineData("XEXX0101")]
    [InlineData("authorization")]
    [InlineData("eyJ+5 1.11111 bearer token=XEXX0101 ")]
    public void Redact_LongAdversarialInput_TakesUnderOneMillisecondPer4KB_TC_TT_OBS_01_12(string unit)
    {
        var input = string.Concat(Enumerable.Repeat(unit, 4096 / unit.Length + 1))[..4096];
        // Calentamiento hasta el JIT optimizado (Tier-1) y el mejor de varios lotes: el planificador y las otras pruebas en paralelo
        // solo suman tiempo; un retroceso catastrófico seguiría lento en todos los lotes.
        var warmUp = Stopwatch.StartNew();
        while (warmUp.ElapsedMilliseconds < 300)
        {
            TelemetryRedactor.Redact(input);
        }

        var best = double.MaxValue;
        for (var batch = 0; batch < 7; batch++)
        {
            var watch = Stopwatch.StartNew();
            for (var i = 0; i < 50; i++)
            {
                TelemetryRedactor.Redact(input);
            }

            best = Math.Min(best, watch.Elapsed.TotalMilliseconds / 50);
        }

        best.Should().BeLessThan(1.0, "sin retroceso catastrófico en un atributo de 4 KB");
    }
}
