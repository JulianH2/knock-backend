using System.Net;
using AwesomeAssertions;

namespace Knock.IntegrationTest.Support;

/// <summary>
/// Verificación compartida: levanta la API y comprueba que readiness responde 200. Readiness exige MongoDB (TT-BD-06) y
/// Blob (TT-API-05), así que se arrancan documentdb-local y Azurite reales.
/// </summary>
internal static class ReadinessChecks
{
    public static async Task AssertReadyReturns200Async()
    {
        await using var azurite = await AzuriteFixture.StartAsync();
        var mongo = await DocumentDbFixture.SharedAsync();
        using var factory = new KnockApiFactory(
            settings: new Dictionary<string, string?>
            {
                ["ConnectionStrings:KnockBlob"] = azurite.ConnectionString,
                ["ConnectionStrings:KnockMongo"] = mongo.ConnectionString,
            });
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
