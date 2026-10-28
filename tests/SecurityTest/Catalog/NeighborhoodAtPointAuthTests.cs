using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Knock.SecurityTest.Identity;
using Xunit;

namespace Knock.SecurityTest.Catalog;

public sealed class NeighborhoodAtPointAuthTests
{
    /// <summary>Sin <c>Authorization</c> más los tokens inválidos de TC-TT-API-02-08 (vencido, firma alterada, otro algoritmo…).</summary>
    public static TheoryData<string, string?> Sessions
    {
        get
        {
            var data = new TheoryData<string, string?> { { "missing-authorization", null } };
            foreach (var row in SessionJwtValidationTests.InvalidTokens)
            {
                data.Add((string)row[0], (string)row[1]);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Sessions))]
    public async Task MissingOrInvalidSession_Returns401AndNeverCallsCatalog_TC_TT_API_07_10(string scenario, string? token)
    {
        var catalog = new CountingNeighborhoodLocator();
        using var factory = catalog.CreateFactory();
        using var client = factory.CreateClient();

        using var response = await TestHttp.GetAsync(client, PointLookupHttp.Path(PointLookupHttp.Query("20.0505", "-99.3495")), bearerToken: token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, scenario);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        catalog.Calls.Should().Be(0, "sin sesión válida no se consulta el catálogo");
    }
}
