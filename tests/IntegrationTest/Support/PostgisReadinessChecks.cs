using System.Net;
using AwesomeAssertions;

namespace Knock.IntegrationTest.Support;

/// <summary>
/// Verificación compartida: levanta la API con la cadena dada y comprueba que readiness responde 200.
/// Readiness también exige Blob (TT-API-05) y MongoDB (TT-BD-06), así que se arrancan Azurite y documentdb-local reales junto a la base.
/// </summary>
internal static class PostgisReadinessChecks
{
    public static async Task AssertReadyReturns200Async(string connectionString)
    {
        await using var azurite = await AzuriteFixture.StartAsync();
        var mongo = await DocumentDbFixture.SharedAsync();
        using var factory = new KnockApiFactory(
            connectionString: connectionString,
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
