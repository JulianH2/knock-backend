using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.IntegrationTest.Errors;

public class ErrorResponseCorrelationTests
{
    private const string _header = "X-Correlation-Id";

    [Fact]
    public async Task ErrorsFromAnyLayer_CarryProblemJsonAndCorrelationId_TC_TT_API_03_12()
    {
        // Límite global de 1 por IP: cada escenario usa su propia IP y solo el de 429 la repite.
        using var factory = new KnockApiFactory(settings: TestHttp.Limits(globalPermits: 1, authPermits: 1000));
        using var client = factory.CreateClient();
        var sent = new Dictionary<string, string> { [_header] = "corr-layers.1" };

        var responses = new List<(string Layer, HttpStatusCode Expected, HttpResponseMessage Response)>
        {
            ("500 exception handler", HttpStatusCode.InternalServerError, await TestHttp.GetAsync(client, "/api/v1/test/boom", "203.0.113.21", sent)),
            ("404 domain exception", HttpStatusCode.NotFound, await TestHttp.GetAsync(client, "/api/v1/test/domain/NotFound", "203.0.113.22", sent)),
            ("401 authentication", HttpStatusCode.Unauthorized, await TestHttp.GetAsync(client, "/api/v1/me", "203.0.113.23", sent)),
            ("422 validation", (HttpStatusCode)422, await TestHttp.SendAsync(client, HttpMethod.Post, "/api/v1/test/validated", "203.0.113.24", sent, jsonBody: """{"phone":"x"}""")),
            ("413 body limit", HttpStatusCode.RequestEntityTooLarge, await OversizedExchangeAsync(client, sent)),
        };
        using var first = await TestHttp.GetAsync(client, "/api/v1/test/ping", "203.0.113.26", sent);
        responses.Add(("429 rate limiter", HttpStatusCode.TooManyRequests, await TestHttp.GetAsync(client, "/api/v1/test/ping", "203.0.113.26", sent)));

        try
        {
            foreach (var (layer, expected, response) in responses)
            {
                response.StatusCode.Should().Be(expected, layer);
                response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json", layer);
                response.Headers.GetValues(_header).Should().ContainSingle().Which.Should().Be("corr-layers.1", layer);
                using var body = await TestHttp.ReadJsonAsync(response);
                body.RootElement.GetProperty("correlationId").GetString().Should().Be("corr-layers.1", layer);
                var traceId = body.RootElement.GetProperty("traceId").GetString();
                traceId.Should().NotBeNullOrWhiteSpace(layer);
                traceId.Should().NotBe("corr-layers.1", "traceId y correlationId son independientes");
            }
        }
        finally
        {
            foreach (var (_, _, response) in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task ErrorWithoutIncomingHeader_UsesTheGeneratedIdInHeaderAndBody_TC_TT_API_03_12()
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();

        using var response = await TestHttp.GetAsync(client, "/api/v1/test/boom");

        var id = response.Headers.GetValues(_header).Single();
        id.Should().MatchRegex("^[0-9a-f]{32}$");
        using var body = await TestHttp.ReadJsonAsync(response);
        body.RootElement.GetProperty("correlationId").GetString().Should().Be(id);
    }

    private static async Task<HttpResponseMessage> OversizedExchangeAsync(HttpClient client, Dictionary<string, string> headers)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, TestHttp.Path("/api/v1/auth/firebase"));
        request.Headers.Add(KnockApiFactory.RemoteIpHeader, "203.0.113.25");
        foreach (var (name, value) in headers)
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }

        request.Content = new StringContent(new string('a', 9000), Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
        return await client.SendAsync(request);
    }
}
