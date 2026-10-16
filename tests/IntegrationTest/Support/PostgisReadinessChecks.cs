using System.Net;
using AwesomeAssertions;

namespace Knock.IntegrationTest.Support;

/// <summary>Verificación compartida: levanta la API con la cadena dada y comprueba que readiness responde 200.</summary>
internal static class PostgisReadinessChecks
{
    public static async Task AssertReadyReturns200Async(string connectionString)
    {
        using var factory = new KnockApiFactory(connectionString: connectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
