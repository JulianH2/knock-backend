using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using OpenTelemetry.Metrics;
using Xunit;

namespace Knock.IntegrationTest.Observability;

public class MetricsTests
{
    private const string _correlationId = "corr-obs-metrics-07";

    [Fact]
    public async Task RequestDuration_HasRouteAndStatus_NoHighCardinalityAttributes_TC_TT_OBS_01_07()
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();
        var headers = new Dictionary<string, string> { ["X-Correlation-Id"] = _correlationId };

        var statuses = new List<HttpStatusCode>();
        foreach (var path in new[] { "/api/v1/test/ping?lat=19.4326", "/api/v1/test/missing-route", "/api/v1/test/at/19.4326/-99.1332?lon=-99.1332", "/api/v1/test/boom" })
        {
            using var response = await TestHttp.GetAsync(client, path, headers: headers);
            statuses.Add(response.StatusCode);
        }

        using (var invalid = await TestHttp.SendAsync(client, HttpMethod.Post, "/api/v1/test/validated", headers: headers, jsonBody: """{"age":5}"""))
        {
            statuses.Add(invalid.StatusCode);
        }

        await factory.Telemetry.WaitForServerSpansAsync(statuses.Count);
        var metrics = factory.Telemetry.CollectMetrics(factory.Services);

        statuses.Should().Equal(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError, HttpStatusCode.InternalServerError, (HttpStatusCode)422);
        var duration = metrics.Where(m => m.Name == "http.server.request.duration").SelectMany(m => m.MetricPoints).Select(Tags).ToList();
        duration.Should().NotBeEmpty();
        duration.Should().Contain(t => Equals(t.GetValueOrDefault("http.route"), "api/v{version:apiVersion}/test/ping")
            && Equals(t.GetValueOrDefault("http.request.method"), "GET")
            && Equals(t.GetValueOrDefault("http.response.status_code"), 200));
        duration.Should().Contain(t => Equals(t.GetValueOrDefault("http.response.status_code"), 404));
        duration.Should().Contain(t => Equals(t.GetValueOrDefault("http.response.status_code"), 422));
        duration.Should().Contain(t => Equals(t.GetValueOrDefault("http.route"), "api/v{version:apiVersion}/test/boom")
            && Equals(t.GetValueOrDefault("http.response.status_code"), 500), "base de la alerta de 5xx");
        duration.Should().Contain(t => Equals(t.GetValueOrDefault("http.route"), "api/v{version:apiVersion}/test/at/{latitude}/{longitude}"));

        var allValues = metrics.SelectMany(m => m.MetricPoints).SelectMany(p => Tags(p).Values).Select(v => v?.ToString() ?? string.Empty).ToList();
        allValues.Should().NotBeEmpty();
        allValues.Should().AllSatisfy(value =>
        {
            value.Should().NotContain("19.4326", "ningún atributo de métrica lleva la ruta con valores");
            value.Should().NotContain("99.1332");
            value.Should().NotContain("?", "ni la query");
            value.Should().NotContain("lat=");
            value.Should().NotContain(_correlationId, "ni el X-Correlation-Id");
        });
    }

    private static Dictionary<string, object?> Tags(MetricPoint point)
    {
        var tags = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var tag in point.Tags)
        {
            tags[tag.Key] = tag.Value;
        }

        return tags;
    }
}
