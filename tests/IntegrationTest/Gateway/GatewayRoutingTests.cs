using System.Net;
using AwesomeAssertions;
using Knock.Contracts.Correlation;
using Knock.IntegrationTest.Gateway.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Knock.IntegrationTest.Gateway;

/// <summary>
/// El gateway enruta cada petición externa al servicio correcto y la deja con <c>X-Correlation-Id</c> (spec 001, CA-02).
/// Los tres servicios son aplicaciones mínimas en memoria que dicen quién respondió y qué identificador de correlación vieron.
/// </summary>
public sealed class GatewayRoutingTests : IAsyncLifetime, IDisposable
{
    private readonly Dictionary<string, WebApplication> _services = [];
    private readonly Dictionary<string, string> _addresses = [];
    private readonly List<string?> _correlationSeen = [];
    private readonly List<(string Path, string Method, string? Authorization)> _requestsSeen = [];
    private GatewayTestHost? _host;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        foreach (var name in new[] { "identidad", "mercado", "mensajeria" })
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            var app = builder.Build();
            var serviceName = name;
            app.Map(
                "/{**ruta}",
                (HttpContext context) =>
                {
                    _correlationSeen.Add(context.Request.Headers[CorrelationId.HeaderName]);
                    _requestsSeen.Add((context.Request.Path.Value!, context.Request.Method, context.Request.Headers.Authorization.ToString()));
                    context.Response.Headers["X-Servicio"] = serviceName;
                    return Results.Ok(new { servicio = serviceName });
                });
            await app.StartAsync();
            _services[name] = app;
            _addresses[name] = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First() + "/";
        }

        _host = GatewayTestHost.Start(_addresses);
        _client = _host.Client;
    }

    [Theory]
    [InlineData("/api/v1/zonas/lista-espera")]
    [InlineData("/api/v1/solicitudes/507f1f77bcf86cd799439011/cotizaciones")]
    public async Task ProtectedPost_IsForwardedUnchangedToMarket_CA_020_16(string path)
    {
        using var response = await _client!.PostAsync(new Uri(path, UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-Servicio").Should().ContainSingle().Which.Should().Be("mercado");
        var forwarded = _requestsSeen.Should().ContainSingle().Which;
        forwarded.Path.Should().Be(path);
        forwarded.Method.Should().Be("POST");
        forwarded.Authorization.Should().Be(_client.DefaultRequestHeaders.Authorization!.ToString());
    }

    [Theory]
    [InlineData("/api/v1/zonas/lista-espera")]
    [InlineData("/api/v1/solicitudes/507f1f77bcf86cd799439011/cotizaciones")]
    public async Task ProtectedPost_WithoutToken_IsNotForwarded_CA_020_16(string path)
    {
        _client!.DefaultRequestHeaders.Authorization = null;
        using var response = await _client.PostAsync(new Uri(path, UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _requestsSeen.Should().BeEmpty();
    }

    public async Task DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.DisposeAsync();
        }

        foreach (var service in _services.Values)
        {
            await service.StopAsync();
            await service.DisposeAsync();
        }
    }

    public void Dispose()
    {
        _host?.Dispose();
    }

    [Theory]
    [InlineData("/api/v1/auth/firebase", "identidad")]
    [InlineData("/api/v1/me", "identidad")]
    [InlineData("/api/v1/me/sesiones", "identidad")]
    [InlineData("/api/v1/oficios", "mercado")]
    [InlineData("/api/v1/colonias/en-punto", "mercado")]
    [InlineData("/api/v1/zonas/cp/42800", "mercado")]
    [InlineData("/api/v1/mensajes/1", "mensajeria")]
    [InlineData("/api/v1/notificaciones", "mensajeria")]
    public async Task Request_IsRoutedToTheRightService_CA_02(string path, string expectedService)
    {
        var response = await _client!.GetAsync(new Uri(path, UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-Servicio").Should().ContainSingle().Which.Should().Be(expectedService);
    }

    [Fact]
    public async Task Request_WithoutCorrelationId_GetsOneAndTheServiceSeesIt_CA_02()
    {
        var response = await _client!.GetAsync(new Uri("/api/v1/oficios", UriKind.Relative));

        var fromResponse = response.Headers.GetValues(CorrelationId.HeaderName).Should().ContainSingle().Subject;
        CorrelationId.IsValid(fromResponse).Should().BeTrue();
        _correlationSeen.Should().ContainSingle().Which.Should().Be(fromResponse);
    }

    [Fact]
    public async Task Request_WithAValidCorrelationId_KeepsItEndToEnd_CA_02()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/v1/oficios", UriKind.Relative));
        request.Headers.Add(CorrelationId.HeaderName, "cliente-abc-123");

        var response = await _client!.SendAsync(request);

        response.Headers.GetValues(CorrelationId.HeaderName).Should().ContainSingle().Which.Should().Be("cliente-abc-123");
        _correlationSeen.Should().ContainSingle().Which.Should().Be("cliente-abc-123");
    }

    [Fact]
    public async Task UnknownRoute_IsNotForwardedToAnyService_R_02()
    {
        var response = await _client!.GetAsync(new Uri("/interno/secreto", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _correlationSeen.Should().BeEmpty();
    }

    [Fact]
    public async Task Health_AnswersWithoutCallingAnyService_CA_01()
    {
        var response = await _client!.GetAsync(new Uri("/health", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _correlationSeen.Should().BeEmpty();
    }
}
