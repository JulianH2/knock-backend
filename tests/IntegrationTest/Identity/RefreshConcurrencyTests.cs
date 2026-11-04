using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.Identity;

/// <summary>HU-003 caso 08: la rotación es atómica; con el mismo refresh solo una renovación gana. Solo datos ficticios.</summary>
public class RefreshConcurrencyTests
{
    private const string _phone = "+525555550303";
    private const int _parallel = 6;

    [Fact]
    public async Task TwoParallelRenewals_OnlyOneWins_TC_HU_003_08()
    {
        using var factory = await IdentityTestDatabase.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var opened = await SessionFixtures.OpenAsync(client, _phone);

        var responses = await Task.WhenAll(
            Enumerable.Range(0, _parallel).Select(_ => AuthHttp.RefreshAsync(client, opened.RefreshToken, opened.DeviceId)));

        try
        {
            responses.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1, "nunca hay dos 200 para el mismo refresh");
            responses.Count(r => r.StatusCode == HttpStatusCode.Unauthorized).Should().Be(_parallel - 1, "el perdedor se trata como reuso, no como error");
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }
}
