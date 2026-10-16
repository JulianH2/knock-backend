using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Api.Hosting;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.OpenApi;

public class ScalarTests
{
    [Fact]
    public async Task Development_ExposesOpenApiAndScalar_TC_TT_API_01_11()
    {
        using var factory = new KnockApiFactory("Development");
        using var client = factory.CreateClient();

        using var openApi = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        using var scalar = await client.GetAsync(new Uri("/scalar", UriKind.Relative));

        openApi.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await openApi.Content.ReadAsStringAsync());
        document.RootElement.TryGetProperty("openapi", out _).Should().BeTrue();
        scalar.StatusCode.Should().Be(HttpStatusCode.OK);
        scalar.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
    }

    [Fact]
    public static void MapKnockOpenApi_WithoutEnvironment_Throws_TC_TT_API_01_11()
    {
        var act = () => OpenApiExtensions.MapKnockOpenApi(null!, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("environment");
    }
}
