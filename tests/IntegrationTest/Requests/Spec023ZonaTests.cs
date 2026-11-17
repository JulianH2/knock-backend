using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Requests;

public sealed class Spec023ZonaTests
{
    [Theory]
    [InlineData("1234")]
    [InlineData("123456")]
    [InlineData("12abc")]
    [InlineData(".*")]
    [InlineData("")]
    [InlineData("%7B%22%24ne%22%3A1%7D")]
    public async Task Codigo_Mal_Formado_CA_023_03(string cp)
    {
        using var factory = MarketApiFactory.CreateOffline(_ => { });
        using var client = WaitlistHttp.SignedIn(factory);
        using var response = await client.GetAsync(new Uri("/api/v1/zonas/cp/" + cp, UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").TryGetProperty("cp", out _).Should().BeTrue();
        if (cp.Length > 0) body.ToString().Should().NotContain(Uri.UnescapeDataString(cp));
    }

    [Fact]
    public async Task Consulta_Sin_Sesion_CA_023_11()
    {
        using var factory = MarketApiFactory.CreateOffline(_ => { });
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(new Uri("/api/v1/zonas/cp/42800", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CodigoPiloto_Lista_Sus_Colonias_CA_023_01()
    {
        var (factory, _) = await WaitlistHttp.CreateAsync();
        using var host = factory;
        var colonias = factory.Database.GetCollection<BsonDocument>("colonias");
        await colonias.InsertManyAsync([
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "cp", "01234" }, { "nombre", "Zeta" }, { "nombreBusqueda", "zeta" }, { "municipio", "Tula" }, { "piloto", false } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "cp", "01234" }, { "nombre", "Álamo" }, { "nombreBusqueda", "alamo" }, { "municipio", "Tula" }, { "piloto", true } },
            new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "cp", "01235" }, { "nombre", "Otra" }, { "nombreBusqueda", "otra" }, { "municipio", "Tula" } },
        ]);
        using var client = WaitlistHttp.SignedIn(factory);
        using var response = await client.GetAsync(new Uri("/api/v1/zonas/cp/01234", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("enCobertura").GetBoolean().Should().BeTrue();
        var results = body.GetProperty("colonias").EnumerateArray().ToArray();
        results.Select(item => item.GetProperty("nombre").GetString()).Should().Equal("Álamo", "Zeta");
        results.Should().OnlyContain(item => item.GetProperty("cp").GetString() == "01234");
        results.Should().OnlyContain(item => item.GetProperty("municipio").GetString() == "Tula");
        results.Should().OnlyContain(item => item.GetProperty("id").GetString()!.Length == 24);
        results.Should().OnlyContain(item => item.EnumerateObject().Count() == 4);
    }

    [Fact]
    public async Task Codigo_Fuera_De_Cobertura_CA_023_02()
    {
        var (factory, _) = await WaitlistHttp.CreateAsync();
        using var host = factory;
        using var client = WaitlistHttp.SignedIn(factory);
        using var response = await client.GetAsync(new Uri("/api/v1/zonas/cp/99999", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("enCobertura").GetBoolean().Should().BeFalse();
        body.GetProperty("ciudadPiloto").GetString().Should().Be("Tula de Allende");
        body.GetProperty("colonias").GetArrayLength().Should().Be(0);
    }
}

public sealed class Spec023TransporteTests
{
    [Fact]
    public async Task Rechaza_Byte_Nulo_En_El_Servidor_CA_023_03()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var app = builder.Build();
        var applicationCalls = 0;
        app.Run(context =>
        {
            Interlocked.Increment(ref applicationCalls);
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        using var response = await client.GetAsync(new Uri("/api/v1/zonas/cp/42800%00", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("42800");
        applicationCalls.Should().Be(0, "Kestrel rechaza la ruta antes de llegar al pipeline y al catálogo");
    }
}

public sealed class Spec023CacheTests
{
    [Fact]
    public async Task Etag_Y_304_CA_023_15()
    {
        var (factory, _) = await WaitlistHttp.CreateAsync();
        using var host = factory;
        using var client = WaitlistHttp.SignedIn(factory);
        using var first = await client.GetAsync(new Uri("/api/v1/zonas/cp/99999", UriKind.Relative));
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        first.Headers.ETag.Should().NotBeNull();
        first.Headers.ETag.IsWeak.Should().BeTrue();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/v1/zonas/cp/99999", UriKind.Relative));
        request.Headers.IfNoneMatch.Add(first.Headers.ETag);
        using var second = await client.SendAsync(request);
        second.StatusCode.Should().Be(HttpStatusCode.NotModified);
        second.Headers.CacheControl!.Private.Should().BeTrue();
        second.Headers.CacheControl.NoCache.Should().BeTrue();
        (await second.Content.ReadAsStringAsync()).Should().BeEmpty();
    }
}

public sealed class Spec023PrivacidadTests
{
    [Fact]
    public async Task Sin_Coordenadas_Ni_Campos_Internos_CA_023_04()
    {
        var (factory, _) = await WaitlistHttp.CreateAsync();
        using var host = factory;
        var subject = Guid.NewGuid().ToString("D");
        using var client = WaitlistHttp.SignedIn(factory, subject);
        var logs = await ConsoleCapture.RunAsync(async () =>
        {
            using var response = await client.GetAsync(new Uri("/api/v1/zonas/cp/95678", UriKind.Relative));
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().NotContain("geom").And.NotContain("centro").And.NotContain("piloto\"")
                .And.NotContain("nombreBusqueda").And.NotContain("version").And.NotContain("\"lat\"").And.NotContain("\"lon\"");
        });
        logs.Should().NotBeNullOrWhiteSpace();
        logs.Should().NotContain("95678").And.NotContain(subject);
    }
}
