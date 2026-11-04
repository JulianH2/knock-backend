using System.Collections.Concurrent;
using System.Net;
using AwesomeAssertions;
using Knock.Api.Common.Security;
using Knock.IntegrationTest.Support;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Knock.IntegrationTest.Identity;

/// <summary>
/// HU-004 caso 08: con lista de revocados configurada, el <c>jti</c> del access token queda revocado y la API lo rechaza. La prueba usa un doble
/// en memoria del puerto <see cref="IAccessTokenRevocations"/>: el adaptador de Redis necesita un paquete que aún no está aprobado (spec §8, duda 1).
/// </summary>
public class LogoutRedisTests
{
    private const string _phone = "+525555550403";

    private sealed class InMemoryRevocations : IAccessTokenRevocations
    {
        public ConcurrentDictionary<string, TimeSpan> Revoked { get; } = new();

        public bool Enabled => true;

        public Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken) => Task.FromResult(Revoked.ContainsKey(jti));

        public Task RevokeAsync(string jti, TimeSpan timeToLive, CancellationToken cancellationToken)
        {
            Revoked[jti] = timeToLive;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task AccessTokenRevokedWithRedis_TC_HU_004_08()
    {
        var revocations = new InMemoryRevocations();
        using var factory = await IdentityTestDatabase.CreateFactoryAsync(services => services.AddSingleton<IAccessTokenRevocations>(revocations));
        using var client = factory.CreateClient();
        var opened = await SessionFixtures.OpenAsync(client, _phone);
        var jti = new JsonWebTokenHandler().ReadJsonWebToken(opened.AccessToken).GetClaim("jti").Value;
        using (var before = await AuthHttp.MeAsync(client, opened.AccessToken))
        {
            before.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var logout = await AuthHttp.LogoutAsync(client, opened.RefreshToken, opened.AccessToken);

        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        revocations.Revoked.Should().ContainKey(jti);
        revocations.Revoked[jti].Should().BePositive().And.BeLessThanOrEqualTo(TimeSpan.FromMinutes(15), "el TTL es la vida restante del token");
        using var after = await AuthHttp.MeAsync(client, opened.AccessToken);
        after.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "el jti revocado se rechaza antes de vencer");
    }
}
