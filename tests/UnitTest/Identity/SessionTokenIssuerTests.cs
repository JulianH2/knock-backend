using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.Api.Common.Security;
using Knock.Api.Modules.Identity.Services;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Knock.UnitTest.Identity;

public class SessionTokenIssuerTests
{
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static readonly DateTimeOffset _now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    private static JwtSettings NewSettings() =>
        new("knock-api", "knock-app", new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)));

    [Fact]
    public void IssuedJwt_HasHs256_15Min_AndExpectedClaims_TC_TT_API_02_07()
    {
        var settings = NewSettings();
        var userId = UserIdDerivation.Derive("uid-prueba-0001");
        var issuer = new SessionTokenIssuer(settings, new FixedTimeProvider(_now));

        var session = issuer.Issue(userId);

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(session.AccessToken);
        jwt.Alg.Should().Be(SecurityAlgorithms.HmacSha256);
        jwt.ValidTo.Should().Be(_now.AddMinutes(15).UtcDateTime);
        jwt.GetClaim("sub").Value.Should().Be(userId.ToString());
        jwt.GetClaim("sub").Value.Should().NotContain("uid-prueba");
        Guid.TryParse(jwt.GetClaim("jti").Value, out _).Should().BeTrue();
        jwt.Issuer.Should().Be("knock-api");
        jwt.Audiences.Should().ContainSingle().Which.Should().Be("knock-app");
        jwt.Claims.Select(c => c.Type).Should().NotContain(["role", "phone_number", "phone", "uid"]);
    }

    [Fact]
    public async Task IssuedJwt_ValidatesWithTheSigningKey_AndNotWithAnother_TC_TT_API_02_07()
    {
        var settings = NewSettings();
        var session = new SessionTokenIssuer(settings, TimeProvider.System).Issue(UserIdDerivation.Derive("uid-prueba-0001"));
        var handler = new JsonWebTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = "knock-api",
            ValidAudience = "knock-app",
            IssuerSigningKey = settings.SigningKey,
            ClockSkew = TimeSpan.Zero,
        };

        (await handler.ValidateTokenAsync(session.AccessToken, parameters)).IsValid.Should().BeTrue();

        parameters.IssuerSigningKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32));
        (await handler.ValidateTokenAsync(session.AccessToken, parameters)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RefreshToken_Is256BitsRandom_AndDifferentEachTime_TC_TT_API_02_07()
    {
        var issuer = new SessionTokenIssuer(NewSettings(), new FixedTimeProvider(_now));
        var userId = UserIdDerivation.Derive("uid-prueba-0001");

        var first = issuer.Issue(userId);
        var second = issuer.Issue(userId);

        Base64UrlEncoder.DecodeBytes(first.RefreshToken).Should().HaveCount(32);
        first.RefreshToken.Should().NotBe(second.RefreshToken);
        first.RefreshToken.Should().NotBe(first.AccessToken);
        first.AccessToken.Should().NotBe(second.AccessToken, "cada JWT lleva un jti distinto");
    }
}
