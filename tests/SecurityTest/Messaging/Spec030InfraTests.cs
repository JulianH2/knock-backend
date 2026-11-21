using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Knock.SecurityTest.Support;
using Xunit;

namespace Knock.SecurityTest.Messaging;

/// <summary>
/// Spec 030, T-04 (KNOCK-72): la infraestructura de controllers de Mensajería (versión en la ruta, ProblemDetails con <c>code</c>, filtro de validación, <c>DomainException</c>,
/// <c>[PrivateNoStore]</c> y límite por persona) sobre una ruta de sonda, porque las rutas de negocio llegan con T-05 a T-07. Solo datos ficticios.
/// </summary>
public sealed class Spec030InfraTests
{
    private const string _base = "/api/v1/sonda/infra";

    private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    [Fact]
    public async Task WithoutAToken_Returns401WithAStableCode_CA_030_20()
    {
        await using var host = await SecurityMessagingFactory.CreateAsync();
        using var client = host.CreateClient();

        using var response = await SecureHttp.SendAsync(client, HttpMethod.Get, $"{_base}/publico");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problem = await ReadProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be("unauthorized");
        problem.GetProperty("status").GetInt32().Should().Be(401);
        problem.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task AnInvalidBody_Returns422WithErrorsPerField_AndNeverEchoesTheValue()
    {
        await using var host = await SecurityMessagingFactory.CreateAsync();
        using var client = host.CreateClient();
        var token = TokenForge.Valid();
        const string secretText = "llámame al 7711234567";

        using var empty = await SecureHttp.PostJsonAsync(client, $"{_base}/validar", new { texto = "" }, token);
        using var tooLong = await SecureHttp.PostJsonAsync(client, $"{_base}/validar", new { texto = secretText }, token);
        using var valid = await SecureHttp.PostJsonAsync(client, $"{_base}/validar", new { texto = "hola" }, token);

        empty.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var emptyProblem = await ReadProblemAsync(empty);
        emptyProblem.GetProperty("code").GetString().Should().Be("validation_failed");
        emptyProblem.GetProperty("errors").GetProperty("texto").EnumerateArray().Select(e => e.GetString()).Should().Contain("Texto is required.");

        tooLong.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var tooLongBody = await tooLong.Content.ReadAsStringAsync();
        var reasons = ProblemBody.Reasons(tooLongBody);
        reasons.Should().NotContain(secretText, "el texto enviado nunca vuelve en el error");
        reasons.Should().NotContain("7711234567");
        using var tooLongJson = JsonDocument.Parse(tooLongBody);
        tooLongJson.RootElement.GetProperty("errors").GetProperty("texto")[0].GetString().Should().Be("The value is not valid.");

        valid.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("BadRequest", HttpStatusCode.BadRequest)]
    [InlineData("Forbidden", HttpStatusCode.Forbidden)]
    [InlineData("NotFound", HttpStatusCode.NotFound)]
    [InlineData("Conflict", HttpStatusCode.Conflict)]
    [InlineData("Unprocessable", HttpStatusCode.UnprocessableEntity)]
    [InlineData("ServiceUnavailable", HttpStatusCode.ServiceUnavailable)]
    public async Task ADomainException_BecomesProblemDetailsWithItsCode_AndNoInternalDetail(string kind, HttpStatusCode expected)
    {
        await using var host = await SecurityMessagingFactory.CreateAsync();
        using var client = host.CreateClient();

        using var response = await SecureHttp.SendAsync(client, HttpMethod.Get, $"{_base}/dominio/{kind}", bearer: TokenForge.Valid());

        response.StatusCode.Should().Be(expected);
        var problem = await ReadProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be("probe_code");
        problem.GetProperty("status").GetInt32().Should().Be((int)expected);
        ProblemBody.Reasons(problem.ToString()).Should().NotContain("detalle interno").And.NotContain("stackTrace");
    }

    [Fact]
    public async Task AnUnexpectedException_Returns500WithoutDetail()
    {
        await using var host = await SecurityMessagingFactory.CreateAsync();
        using var client = host.CreateClient();

        using var response = await SecureHttp.SendAsync(client, HttpMethod.Get, $"{_base}/explota", bearer: TokenForge.Valid());

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync();
        var reasons = ProblemBody.Reasons(body);
        reasons.Should().NotContainAny(SecureHttp.ForbiddenInErrors);
        reasons.Should().NotContain("detalle interno");
    }

    [Fact]
    public async Task PrivateNoStore_MakesTheResponsePrivate_WhileOthersKeepTheGeneralNoStore()
    {
        await using var host = await SecurityMessagingFactory.CreateAsync();
        using var client = host.CreateClient();
        var token = TokenForge.Valid();

        using var annotated = await SecureHttp.SendAsync(client, HttpMethod.Get, $"{_base}/privado", bearer: token);
        using var plain = await SecureHttp.SendAsync(client, HttpMethod.Get, $"{_base}/publico", bearer: token);

        annotated.StatusCode.Should().Be(HttpStatusCode.OK);
        annotated.Headers.CacheControl!.ToString().Should().Be("no-store, private");
        annotated.Headers.Contains("ETag").Should().BeFalse();
        plain.Headers.CacheControl!.ToString().Should().Be("no-store", "sin el atributo queda el no-store general del servicio, sin private");
    }

    [Fact]
    public async Task TheVersionIsPartOfTheRoute()
    {
        await using var host = await SecurityMessagingFactory.CreateAsync();
        using var client = host.CreateClient();
        var token = TokenForge.Valid();

        using var v1 = await SecureHttp.SendAsync(client, HttpMethod.Get, $"{_base}/publico", bearer: token);
        using var v2 = await SecureHttp.SendAsync(client, HttpMethod.Get, "/api/v2/sonda/infra/publico", bearer: token);
        using var unversioned = await SecureHttp.SendAsync(client, HttpMethod.Get, "/api/sonda/infra/publico", bearer: token);

        v1.StatusCode.Should().Be(HttpStatusCode.OK);
        v1.Headers.GetValues("api-supported-versions").Should().ContainSingle().Which.Should().Be("1.0");
        v2.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
        unversioned.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TheCreationLimitIs20PerHourPerPerson_Returns429OnThe21st_CA_030_20()
    {
        await using var host = await SecurityMessagingFactory.CreateAsync();
        using var client = host.CreateClient();
        var attacker = TokenForge.Valid(TokenForge.Sub());
        var bystander = TokenForge.Valid(TokenForge.Sub());

        for (var i = 0; i < 20; i++)
        {
            using var allowed = await SecureHttp.SendAsync(client, HttpMethod.Post, $"{_base}/crear", bearer: attacker, remoteIp: "203.0.113.31");
            allowed.StatusCode.Should().Be(HttpStatusCode.Created, $"la creación {i + 1} cabe en el límite");
        }

        using var limited = await SecureHttp.SendAsync(client, HttpMethod.Post, $"{_base}/crear", bearer: attacker, remoteIp: "203.0.113.31");
        using var sameIpOtherPerson = await SecureHttp.SendAsync(client, HttpMethod.Post, $"{_base}/crear", bearer: bystander, remoteIp: "203.0.113.31");

        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limited.Headers.GetValues("Retry-After").Should().ContainSingle().Which.Should().MatchRegex("^[1-9][0-9]*$");
        var problem = await ReadProblemAsync(limited);
        problem.GetProperty("code").GetString().Should().Be("rate_limited");
        ProblemBody.Reasons(problem.ToString()).Should().NotContain(TokenForge.Claim(attacker, "sub")).And.NotContain("conversaciones-crear");
        sameIpOtherPerson.StatusCode.Should().Be(HttpStatusCode.Created, "el límite es por persona, no por IP");
    }
}
