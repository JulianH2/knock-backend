using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.SecurityTest.OpenApi;

public class ScalarExposureTests
{
    [Theory]
    [InlineData("Production", "/openapi/v1.json")]
    [InlineData("Production", "/scalar")]
    [InlineData("Production", "/scalar/v1")]
    [InlineData("Staging", "/openapi/v1.json")]
    [InlineData("Staging", "/scalar")]
    public async Task NonDevelopment_OpenApiAndScalar_Return404_TC_TT_API_01_12(string environment, string path)
    {
        using var factory = new KnockApiFactory(environment);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri(path, UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
