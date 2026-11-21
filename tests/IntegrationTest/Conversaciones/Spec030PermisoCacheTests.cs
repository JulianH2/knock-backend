extern alias MessagingApi;

using AwesomeAssertions;
using Knock.Contracts.Conversations;
using Knock.IntegrationTest.Support;
using MessagingApi::Knock.Messaging.Api.Modules.Conversaciones.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit;

namespace Knock.IntegrationTest.Conversaciones;

/// <summary>
/// Spec 030, T-04 (R-09, CA-09) con Redis real: el permiso se guarda 60 segundos por conversación y por persona, y con Mercado caído responde mientras dure. Mercado es un servidor
/// de mentira que se apaga a mitad de la prueba. Solo datos ficticios.
/// </summary>
public sealed class Spec030PermisoCacheTests
{
    private const string _request = "507f1f77bcf86cd799439011";
    private const string _technician = "507f1f77bcf86cd799439012";

    private static PermisoConversacion Permission(string role = "cliente") =>
        new(_request, _technician, "22222222-2222-4222-8222-222222222222", "33333333-3333-4333-8333-333333333333", role, true, null, false, null, "Plomería", "Colonia Centro", new TecnicoPublicoConversacion("Técnico de Prueba", null));

    private static string AddressOf(WebApplication app)
    {
        var feature = app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>().Features.Get<IServerAddressesFeature>()
            ?? throw new InvalidOperationException("The test server has no addresses.");
        return feature.Addresses.First();
    }

    private static string NewId() => Guid.NewGuid().ToString("N")[..24];

    private static async Task<IConnectionMultiplexer> ConnectAsync()
    {
        var fixture = await RedisTestFixture.SharedAsync();
        return await ConnectionMultiplexer.ConnectAsync(fixture.ConnectionString);
    }

    [Fact]
    public async Task SetAsync_StoresThePermissionFor60Seconds_AndGetAsyncReturnsIt_CA_030_09()
    {
        await using var redis = (ConnectionMultiplexer)await ConnectAsync();
        var cache = new PermisoRedisCache(redis);
        var conversation = NewId();
        var subject = Guid.NewGuid().ToString("D");

        await cache.SetAsync(conversation, subject, Permission(), CancellationToken.None);

        var key = PermisoRedisCache.Key(conversation, subject);
        var ttl = await redis.GetDatabase().KeyTimeToLiveAsync(key);
        ttl.Should().NotBeNull();
        ttl.Value.Should().BeGreaterThan(TimeSpan.FromSeconds(55)).And.BeLessThanOrEqualTo(TimeSpan.FromSeconds(60));
        (await redis.GetDatabase().StringGetAsync(key)).ToString().Should().NotContain(subject, "el valor es el permiso, no la persona que preguntó");
        var read = await cache.GetAsync(conversation, subject, CancellationToken.None);
        read.Should().BeEquivalentTo(Permission());
    }

    [Fact]
    public async Task GetAsync_APermissionStoredForOnePersonIsNotReturnedForAnother()
    {
        await using var redis = (ConnectionMultiplexer)await ConnectAsync();
        var cache = new PermisoRedisCache(redis);
        var conversation = NewId();
        var client = Guid.NewGuid().ToString("D");
        var technician = Guid.NewGuid().ToString("D");

        await cache.SetAsync(conversation, client, Permission("cliente"), CancellationToken.None);

        (await cache.GetAsync(conversation, technician, CancellationToken.None)).Should().BeNull("cada participante tiene su propio rol en el permiso");
        (await cache.GetAsync(NewId(), client, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_AnExpiredOrCorruptEntry_IsAMissNeverAnError()
    {
        await using var redis = (ConnectionMultiplexer)await ConnectAsync();
        var cache = new PermisoRedisCache(redis);
        var conversation = NewId();
        var subject = Guid.NewGuid().ToString("D");
        await redis.GetDatabase().StringSetAsync(PermisoRedisCache.Key(conversation, subject), "{ not json", TimeSpan.FromSeconds(30));

        (await cache.GetAsync(conversation, subject, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task RedisDown_TheCacheNeverThrows_CA_030_09()
    {
        var options = ConfigurationOptions.Parse("127.0.0.1:1,abortConnect=false,connectTimeout=200,syncTimeout=200,asyncTimeout=200,connectRetry=1");
        await using var broken = await ConnectionMultiplexer.ConnectAsync(options);
        var cache = new PermisoRedisCache(broken);

        var read = await cache.GetAsync(NewId(), "sub", CancellationToken.None);
        var write = async () => await cache.SetAsync(NewId(), "sub", Permission(), CancellationToken.None);

        read.Should().BeNull();
        await write.Should().NotThrowAsync();
    }

    [Fact]
    public async Task TheClient_WithMarketStoppedAfterTheFirstAnswer_KeepsGrantingForTheLifeOfTheCache_CA_030_09()
    {
        await using var redis = (ConnectionMultiplexer)await ConnectAsync();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var market = builder.Build();
        var calls = 0;
        market.MapGet("/api/v1/solicitudes/{id}/conversacion", (string id) =>
        {
            Interlocked.Increment(ref calls);
            return Results.Json(Permission());
        });
        await market.StartAsync();
        var baseUrl = AddressOf(market);
        using var http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
        var client = new MarketPermisoDeConversacion(http, new PermisoRedisCache(redis));
        var conversation = NewId();
        var subject = Guid.NewGuid().ToString("D");

        var first = await client.ConsultarAsync(_request, _technician, conversation, subject, "token-ficticio", CancellationToken.None);
        await market.StopAsync();
        var second = await client.ConsultarAsync(_request, _technician, conversation, subject, "token-ficticio", CancellationToken.None);
        var otherPerson = await client.ConsultarAsync(_request, _technician, conversation, Guid.NewGuid().ToString("D"), "token-ficticio", CancellationToken.None);
        var newConversation = await client.ConsultarAsync(_request, _technician, NewId(), subject, "token-ficticio", CancellationToken.None);

        first.Estado.Should().Be(EstadoPermisoConversacion.Concedido);
        Volatile.Read(ref calls).Should().Be(1, "la segunda salió de la caché");
        second.Estado.Should().Be(EstadoPermisoConversacion.Concedido);
        second.Permiso.Should().BeEquivalentTo(first.Permiso);
        otherPerson.Estado.Should().Be(EstadoPermisoConversacion.NoDisponible, "otra persona no hereda el permiso en caché y Mercado está caído");
        newConversation.Estado.Should().Be(EstadoPermisoConversacion.NoDisponible, "sin permiso en caché y con Mercado caído nunca se asume");
    }

    [Fact]
    public async Task TheClient_SendsTheSessionAndTheTechnician_AndMapsTheMarketAnswers()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var market = builder.Build();
        string? seenAuthorization = null;
        string? seenQuery = null;
        market.MapGet("/api/v1/solicitudes/{id}/conversacion", (string id, HttpContext context) =>
        {
            seenAuthorization = context.Request.Headers.Authorization.ToString();
            seenQuery = context.Request.Query["tecnicoId"].ToString();
            return id == _request ? Results.Json(Permission()) : Results.StatusCode(StatusCodes.Status404NotFound);
        });
        await market.StartAsync();
        var baseUrl = AddressOf(market);
        using var http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
        await using var redis = (ConnectionMultiplexer)await ConnectAsync();
        var client = new MarketPermisoDeConversacion(http, new PermisoRedisCache(redis));

        var found = await client.ConsultarAsync(_request, _technician, null, "sub-ficticio", "token-ficticio", CancellationToken.None);
        var missing = await client.ConsultarAsync("507f1f77bcf86cd7994390ff", _technician, null, "sub-ficticio", "token-ficticio", CancellationToken.None);

        found.Estado.Should().Be(EstadoPermisoConversacion.Concedido);
        seenAuthorization.Should().Be("Bearer token-ficticio");
        seenQuery.Should().Be(_technician);
        missing.Estado.Should().Be(EstadoPermisoConversacion.NoEncontrado);
    }
}
