using System.Net;
using System.Security.Cryptography;
using System.Text;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Knock.SecurityTest.Identity;

public class SessionJwtValidationTests
{
    private static readonly SymmetricSecurityKey _ownKey = new(KnockApiFactory.TestSigningKey);

    private static string Jwt(
        SymmetricSecurityKey? key = null,
        string issuer = KnockApiFactory.TestIssuer,
        string audience = KnockApiFactory.TestAudience,
        TimeSpan? expiresIn = null,
        string algorithm = SecurityAlgorithms.HmacSha256)
    {
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Claims = new Dictionary<string, object>
            {
                ["sub"] = Guid.NewGuid().ToString(),
                ["jti"] = Guid.NewGuid().ToString(),
            },
            NotBefore = now - TimeSpan.FromMinutes(20),
            IssuedAt = now - TimeSpan.FromMinutes(20),
            Expires = now + (expiresIn ?? TimeSpan.FromMinutes(15)),
            SigningCredentials = new SigningCredentials(key ?? _ownKey, algorithm),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static string UnsignedAlgNone()
    {
        var header = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes("""{"alg":"none","typ":"JWT"}"""));
        var exp = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds();
        var payload = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(
            $$"""{"sub":"{{Guid.NewGuid()}}","jti":"{{Guid.NewGuid()}}","iss":"knock-api","aud":"knock-app","exp":{{exp}}}"""));
        return $"{header}.{payload}.";
    }

    private static string TamperedSignature()
    {
        var token = Jwt();
        var lastDot = token.LastIndexOf('.');
        var signature = token[(lastDot + 1)..].ToCharArray();
        signature[0] = signature[0] == 'A' ? 'B' : 'A';
        return string.Concat(token.AsSpan(0, lastDot + 1), new string(signature));
    }

    public static TheoryData<string, string> InvalidTokens => new()
    {
        { "tampered-signature", TamperedSignature() },
        { "expired-one-second-ago", Jwt(expiresIn: TimeSpan.FromSeconds(-1)) },
        { "wrong-issuer", Jwt(issuer: "otro-emisor") },
        { "wrong-audience", Jwt(audience: "otra-audiencia") },
        { "alg-none", UnsignedAlgNone() },
        { "other-key", Jwt(key: new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32))) },
        { "hs512-signed-with-extended-own-key", Jwt(key: new SymmetricSecurityKey([.. KnockApiFactory.TestSigningKey, .. KnockApiFactory.TestSigningKey]), algorithm: SecurityAlgorithms.HmacSha512) },
        { "garbage", "not-a-jwt" },
    };

    [Fact]
    public async Task ValidJwt_Control_IsAccepted_TC_TT_API_02_08()
    {
        // Control: demuestra que las variantes inválidas fallan por su defecto y no por un armado erróneo de la prueba.
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();

        using var response = await AuthHttp.MeAsync(client, Jwt());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [MemberData(nameof(InvalidTokens))]
    public async Task InvalidJwt_Returns401_TC_TT_API_02_08(string scenario, string token)
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();

        using var response = await AuthHttp.MeAsync(client, token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, scenario);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain("expired").And.NotContain("signature").And.NotContain("IDX");
    }
}
