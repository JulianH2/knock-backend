using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Knock.SecurityTest.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Knock.SecurityTest.Logging;

/// <summary>
/// Spec 035 (KNOCK-186): con todos los niveles de Serilog en <c>Verbose</c>, ningún registro de Mercado, Identidad ni Mensajería deja salir la consulta de una
/// petición (R-01, R-02 y R-03). El gateway lo comprueba <c>Spec035GatewayRegistrosTests</c> en IntegrationTest, que tiene su host real con servicios detrás.
/// Solo datos ficticios.
/// </summary>
public sealed class Spec035RegistrosTests
{
    private const string _marketPath = "/api/v1/colonias/en-punto";
    private const string _identityPath = "/api/v1/me";
    private const string _messagingPath = "/sonda/protegida";

    private static readonly Dictionary<string, string?> _verbose = new(QueryLeakDetector.VerboseSettings);

    [Fact]
    public async Task Identity_ProblemDetails_Y_Traza_CA_035_07_CA_035_16()
    {
        using var factory = await SecurityIdentityFactory.CreateAsync();
        await CheckProblemAndTraceAsync(factory, HttpMethod.Post, "/api/v1/auth/refresh", "/api/v{version:apiVersion}/auth/refresh", HttpStatusCode.BadRequest, "{");
    }

    [Fact]
    public async Task Market_ProblemDetails_Y_Traza_CA_035_07_CA_035_16()
    {
        using var factory = await SecurityMarketFactory.CreateAsync();
        await CheckProblemAndTraceAsync(factory, HttpMethod.Get, "/api/v1/colonias/en-punto", "/api/v{version:apiVersion}/colonias/en-punto", HttpStatusCode.UnprocessableEntity, bearer: TokenForge.Valid());
    }

    [Fact]
    public async Task Messaging_ProblemDetails_Y_Traza_CA_035_07_CA_035_16()
    {
        using var factory = await SecurityMessagingFactory.CreateAsync();
        await CheckProblemAndTraceAsync(factory, HttpMethod.Get, "/sonda/protegida", "/sonda/protegida", HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Market_Error_500_Y_Registro_Sin_Consulta_CA_035_07()
    {
        using var factory = await SecurityMarketFactory.CreateAsync();
        factory.Locator.Failure = new InvalidOperationException("catalog down");
        using var client = factory.CreateClient();
        HttpResponseMessage? response = null;
        var output = await ConsoleCapture.RunAsync(async () =>
        {
            response = await SecureHttp.SendAsync(client, HttpMethod.Get,
                "/api/v1/colonias/en-punto?lat=19.4&lon=-99.1&q=secreto&token=x", bearer: TokenForge.Valid());
        });
        using (response)
        {
            response!.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            var body = await SecureHttp.ReadJsonAsync(response);
            body.GetProperty("instance").GetString().Should().Be("/api/v1/colonias/en-punto");
        }

        output.Should().NotBeNullOrWhiteSpace();
        output.Should().Contain("\"StatusCode\":500");
        output.Should().NotContain("secreto").And.NotContain("token=x").And.NotContain("q=");
    }

    private static async Task CheckProblemAndTraceAsync<T>(
        WebApplicationFactory<T> factory, HttpMethod method, string path, string expectedInstance, HttpStatusCode status, string? json = null, string? bearer = null)
        where T : class
    {
        var capture = new QueryTraceCapture();
        using var host = factory.WithWebHostBuilder(builder => builder.ConfigureServices(capture.Configure));
        using var client = host.CreateClient();
        var url = path + "?q=secreto&token=x";
        using var response = json is null
            ? await SecureHttp.SendAsync(client, method, url, bearer: bearer)
            : await SecureHttp.SendRawJsonAsync(client, method, url, json, bearer);
        response.StatusCode.Should().Be(status);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var body = await SecureHttp.ReadJsonAsync(response);
        body.GetProperty("instance").GetString().Should().Be(expectedInstance);
        await capture.AssertNoQueryAsync();
    }

    [Fact]
    public async Task Market_Sin_Consulta_En_Ningun_Nivel_CA_035_01()
    {
        using var factory = await SecurityMarketFactory.CreateAsync(_verbose);
        using var client = factory.CreateClient();

        var output = await ConsoleCapture.RunAsync(async () =>
        {
            using var response = await SecureHttp.SendAsync(client, HttpMethod.Get, _marketPath + QueryLeakDetector.Query, bearer: TokenForge.Valid());
        });

        output.Should().NotBeNullOrWhiteSpace("el host registra algo con todos los niveles en Verbose");
        QueryLeakDetector.Find(output).Should().BeEmpty();
    }

    [Fact]
    public async Task Identity_Sin_Consulta_En_Ningun_Nivel_CA_035_01()
    {
        using var factory = await SecurityIdentityFactory.CreateAsync(_verbose);
        using var client = factory.CreateClient();

        var output = await ConsoleCapture.RunAsync(async () =>
        {
            using var response = await SecureHttp.SendAsync(client, HttpMethod.Get, _identityPath + QueryLeakDetector.Query);
        });

        output.Should().NotBeNullOrWhiteSpace("el host registra algo con todos los niveles en Verbose");
        QueryLeakDetector.Find(output).Should().BeEmpty();
    }

    [Fact]
    public async Task Messaging_Sin_Consulta_En_Ningun_Nivel_CA_035_01()
    {
        using var factory = await SecurityMessagingFactory.CreateAsync(_verbose);
        using var client = factory.CreateClient();

        var output = await ConsoleCapture.RunAsync(async () =>
        {
            using var response = await SecureHttp.SendAsync(client, HttpMethod.Get, _messagingPath + QueryLeakDetector.Query);
        });

        output.Should().NotBeNullOrWhiteSpace("el host registra algo con todos los niveles en Verbose");
        QueryLeakDetector.Find(output).Should().BeEmpty();
    }

    [Fact]
    public async Task Market_Llamada_Saliente_Sin_Consulta_CA_035_02()
    {
        using var factory = await SecurityMarketFactory.CreateAsync(_verbose);

        var output = await ConsoleCapture.RunAsync(() => OutboundCallAsync(factory));

        output.Should().NotBeNullOrWhiteSpace();
        QueryLeakDetector.Find(output).Should().BeEmpty();
        output.Should().NotContain("/ruta-saliente", "la dirección completa tampoco sale");
    }

    [Fact]
    public async Task Messaging_Llamada_Saliente_Sin_Consulta_CA_035_02()
    {
        using var factory = await SecurityMessagingFactory.CreateAsync(_verbose);

        var output = await ConsoleCapture.RunAsync(() => OutboundCallAsync(factory));

        output.Should().NotBeNullOrWhiteSpace();
        QueryLeakDetector.Find(output).Should().BeEmpty();
        output.Should().NotContain("/ruta-saliente", "la dirección completa tampoco sale");
    }

    [Fact]
    public void La_Prueba_Falla_Sin_Filtro_CA_035_04()
    {
        // Lo que dejaría salir ASP.NET Core con el nivel Information si el filtro de R-01 no existiera: el detector tiene que marcarlo.
        const string leaked = """{"@m":"Request starting HTTP/1.1 GET http://localhost/api/v1/colonias/en-punto?lat=19.4326&lon=-99.1332&q=fuga%20de%20agua&cp=42000&token=abc"}""";
        const string clean = """{"@m":"HTTP GET /api/v1/colonias/en-punto responded 401 in 1.2 ms"}""";

        QueryLeakDetector.Find(leaked).Should().Contain(["lat=", "lon=", "q=", "cp=", "token=", "19.4326", "99.1332"]);
        QueryLeakDetector.Find(clean).Should().BeEmpty();
    }

    [Fact]
    public async Task Market_Registro_De_Peticion_Minimo_CA_035_05()
    {
        using var factory = await SecurityMarketFactory.CreateAsync();
        using var client = factory.CreateClient();

        var output = await ConsoleCapture.RunAsync(async () =>
        {
            using var response = await SecureHttp.SendAsync(client, HttpMethod.Get, _marketPath + QueryLeakDetector.Query, bearer: TokenForge.Valid());
        });

        var requestLog = output.Split('\n').Where(line => line.Contains("\"RequestMethod\"", StringComparison.Ordinal)).ToList();
        requestLog.Should().ContainSingle("hay un solo registro de petición");
        requestLog[0].Should().Contain("\"RequestMethod\":\"GET\"")
            .And.Contain("\"RequestPath\":\"" + _marketPath + "\"")
            .And.Contain("\"StatusCode\":")
            .And.Contain("\"Elapsed\":")
            .And.NotContain("localhost", "no lleva el host")
            .And.NotContain("Authorization", "no lleva encabezados");
    }

    /// <summary>Una llamada saliente con consulta desde un cliente HTTP con nombre del host real, a un puerto sin servicio: falla, pero se registra.</summary>
    private static async Task OutboundCallAsync<T>(WebApplicationFactory<T> factory)
        where T : class
    {
        using var withClient = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddHttpClient("spec035")));
        var http = withClient.Services.GetRequiredService<IHttpClientFactory>().CreateClient("spec035");
        try
        {
            using var response = await http.GetAsync(new Uri("http://127.0.0.1:9/ruta-saliente" + QueryLeakDetector.Query));
        }
        catch (HttpRequestException)
        {
            // Esperado: no hay nada escuchando; lo que importa es lo que se registró.
        }
    }
}
