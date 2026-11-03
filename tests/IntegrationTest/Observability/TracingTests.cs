using System.Diagnostics;
using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Xunit;

namespace Knock.IntegrationTest.Observability;

public class TracingTests
{
    [Fact]
    public async Task ServerSpan_HasRouteTemplateAndResource_HealthIsExcluded_TC_TT_OBS_01_03()
    {
        var mongo = await DocumentDbFixture.SharedAsync();
        var (factory, _) = await PointLookupHttp.CreateSeededFactoryAsync(mongo.ConnectionString);
        using (factory)
        {
            using var client = factory.CreateClient();
            using var oficios = await TestHttp.GetAsync(client, "/api/v1/oficios");
            using var withParameter = await TestHttp.GetAsync(client, "/api/v1/test/domain/NotFound");
            using var health = await TestHttp.GetAsync(client, "/health");
            using var ready = await TestHttp.GetAsync(client, "/health/ready");

            oficios.StatusCode.Should().Be(HttpStatusCode.OK);
            withParameter.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var spans = await factory.Telemetry.WaitForServerSpansAsync(2);
            // Margen para que, si /health generara span, también llegara.
            await Task.Delay(200);

            factory.Telemetry.ServerSpans.Should().HaveCount(2, "/health y /health/ready no generan spans");
            var oficiosSpan = spans.Single(s => Equals(s.GetTagItem("http.route"), "api/v{version:apiVersion}/oficios"));
            var parameterSpan = spans.Single(s => Equals(s.GetTagItem("http.route"), "api/v{version:apiVersion}/test/domain/{kind}"));

            oficiosSpan.GetTagItem("http.request.method").Should().Be("GET");
            oficiosSpan.GetTagItem("http.response.status_code").Should().Be(200);
            oficiosSpan.GetTagItem("url.path").Should().Be("/api/v1/oficios");
            parameterSpan.GetTagItem("http.response.status_code").Should().Be(404);
            parameterSpan.GetTagItem("url.path").Should().Be("/api/v1/test/domain/{kind}", "url.path es la plantilla, sin el valor del parámetro");
            spans.Should().AllSatisfy(s => s.Kind.Should().Be(ActivityKind.Server));

            var resource = factory.Services.GetRequiredService<TracerProvider>().GetResource().Attributes.ToDictionary(a => a.Key, a => a.Value);
            resource["service.name"].Should().Be("knock-api");
            resource["service.version"].Should().BeOfType<string>().Which.Should().NotBeNullOrWhiteSpace();
            resource["deployment.environment"].Should().Be("Development");
        }
    }
}
