using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.SecurityTest.Logging;

public class SensitiveLogTests
{
    private const string _ip = "203.0.113.80";
    private const string _phone = "+15555550199";
    private const string _bodyToken = "tok-" + "SECRET-422-0123456789abcdef";
    private const string _coordinates = "19.4326/-99.1332";

    [Fact]
    public async Task Logs_DoNotContainTokensPhonesOrCoordinates_TC_TT_API_03_15()
    {
        string bearer = string.Empty;
        var statuses = new List<HttpStatusCode>();
        var output = await ConsoleCapture.RunAsync(async () =>
        {
            using var factory = new KnockApiFactory(settings: TestHttp.Limits(globalPermits: 3, authPermits: 1000));
            using var client = factory.CreateClient();
            bearer = TestHttp.IssueToken(factory, Guid.NewGuid());

            // Excepción no controlada (servicio + error) con coordenadas en la ruta y en la query.
            using var failing = await TestHttp.GetAsync(
                client, $"/api/v1/test/at/{_coordinates}?lat=19.4326&lon=-99.1332&phone=%2B15555550199", _ip, bearerToken: bearer);
            statuses.Add(failing.StatusCode);

            // Validación 422 con token, celular y coordenadas en el cuerpo.
            using var invalid = await TestHttp.SendAsync(
                client, HttpMethod.Post, "/api/v1/test/validated", _ip, bearerToken: bearer,
                jsonBody: $$"""{"phone":"{{_phone}}-bad","token":"{{_bodyToken}}","age":5,"echo":"19.4326,-99.1332"}""");
            statuses.Add(invalid.StatusCode);

            // Petición correcta con coordenadas y celular en la query.
            using var plain = await TestHttp.GetAsync(client, "/api/v1/test/ping?lat=19.4326&lon=-99.1332", _ip, bearerToken: bearer);
            statuses.Add(plain.StatusCode);

            // 429 con las mismas piezas sensibles en ruta, query y encabezado.
            using var limited = await TestHttp.GetAsync(client, $"/api/v1/test/at/{_coordinates}?phone=%2B15555550199", _ip, bearerToken: bearer);
            statuses.Add(limited.StatusCode);
        });

        statuses.Should().Equal(HttpStatusCode.InternalServerError, (HttpStatusCode)422, HttpStatusCode.OK, HttpStatusCode.TooManyRequests);
        output.Should().NotBeNullOrWhiteSpace();
        output.Should().NotContain(bearer, "el JWT no se registra");
        output.Should().NotContain("Bearer ");
        output.Should().NotContain(_bodyToken);
        output.Should().NotContain("15555550199");
        output.Should().NotContain("19.4326");
        output.Should().NotContain("99.1332");
        output.Should().Contain("\"StatusCode\":422", "el 422 sí se registra");
        output.Should().Contain("\"StatusCode\":429", "el 429 sí se registra");
    }
}
