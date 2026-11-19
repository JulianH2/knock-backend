using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Gateway.Hosting;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Knock.IntegrationTest.Gateway;

/// <summary>
/// Spec 035 (KNOCK-186, CA-03): con todos los niveles en <c>Verbose</c>, el gateway real reenvía una petición con consulta a un servicio y ningún registro
/// deja salir la consulta, ni la de YARP ni la de ASP.NET Core. Los demás servicios los comprueba <c>Spec035RegistrosTests</c> en SecurityTest.
/// Solo datos ficticios.
/// </summary>
public sealed class Spec035GatewayRegistrosTests
{
    [Fact]
    public async Task Error_Http_Y_Traza_Exportada_Sin_Consulta_CA_035_07_CA_035_16()
    {
        var capture = new QueryTraceCapture();
        await using var host = await GatewayHost.StartAsync(
            configureServices: services =>
            {
                services.AddSingleton<IRevokedSessions>(new NoRevokedSessions());
                services.AddSingleton<IRequestCounter>(new NoLimitCounter());
                capture.Configure(services);
            });
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/v1/me?q=secreto&token=x", UriKind.Relative));
        using var response = await host.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await capture.AssertNoQueryAsync();

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("instance").GetString().Should().Be("/api/v1/me");
    }

    [Fact]
    public async Task Reenvio_Sin_Consulta_CA_035_03()
    {
        await using var host = await GatewayHost.StartAsync(
            QueryLeakDetector.VerboseSettings,
            services =>
            {
                services.AddSingleton<IRevokedSessions>(new NoRevokedSessions());
                services.AddSingleton<IRequestCounter>(new NoLimitCounter());
            });

        HttpStatusCode? status = null;
        var output = await ConsoleCapture.RunAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/v1/colonias/en-punto" + QueryLeakDetector.Query, UriKind.Relative));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GatewayTestSupport.IssueToken());
            using var response = await host.Client.SendAsync(request);
            status = response.StatusCode;
        });

        status.Should().Be(HttpStatusCode.OK, "la petición sí se reenvió al servicio de Mercado");
        host.PathsSeen.Should().ContainSingle().Which.Should().Be("/api/v1/colonias/en-punto");
        output.Should().NotBeNullOrWhiteSpace("el gateway registra algo con todos los niveles en Verbose");
        QueryLeakDetector.Find(output).Should().BeEmpty();
    }
}
