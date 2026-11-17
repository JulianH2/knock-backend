using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using MongoDB.Bson;
using Xunit;

namespace Knock.IntegrationTest.Requests;

public sealed class Spec020SeguridadTests
{
    [Fact]
    public async Task Exige_Sesion_Y_Rol_CA_020_16()
    {
        using var factory = await MarketApiFactory.CreateAsync();
        using var anonymous = factory.CreateClient();
        using var client = WaitlistHttp.SignedIn(factory);
        var route = new Uri($"/api/v1/solicitudes/{ObjectId.GenerateNewId()}/cotizaciones", UriKind.Relative);
        var body = new { tipoPrecio = "fijo", precio = 100, dia = DateOnly.FromDateTime(DateTime.UtcNow), hora = "12:00" };

        using var unauthenticated = await anonymous.PostAsJsonAsync(route, body);
        using var nonTechnician = await client.PostAsJsonAsync(route, body);

        unauthenticated.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        nonTechnician.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Limita_Por_Tecnico_Y_Devuelve_RetryAfter()
    {
        using var factory = await MarketApiFactory.CreateAsync();
        using var client = await QuoteHttp.Technician(factory);
        var route = QuoteHttp.Route(ObjectId.GenerateNewId());
        for (var i = 0; i < 3; i++)
        {
            using var response = await client.PostAsJsonAsync(route, QuoteHttp.Body());
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        using var limited = await client.PostAsJsonAsync(route, QuoteHttp.Body());
        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limited.Headers.RetryAfter.Should().NotBeNull();
        using var another = await QuoteHttp.Technician(factory);
        using var allowed = await another.PostAsJsonAsync(route, QuoteHttp.Body());
        allowed.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Nota_Con_Moderador_No_Disponible_Falla_Cerrado()
    {
        using var factory = await MarketApiFactory.CreateAsync();
        var id = await QuoteHttp.Request(factory);
        using var client = await QuoteHttp.Technician(factory);
        using var response = await client.PostAsJsonAsync(QuoteHttp.Route(id), new
        {
            tipoPrecio = "fijo",
            precio = 100,
            dia = DateOnly.FromDateTime(DateTime.UtcNow),
            hora = "12:00",
            nota = "nota ficticia"
        });
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Headers.RetryAfter.Should().NotBeNull();
        await QuoteHttp.Code(response, "moderation_unavailable");
        (await factory.Quotes.CountDocumentsAsync(MongoDB.Driver.FilterDefinition<BsonDocument>.Empty)).Should().Be(0);
    }
}
