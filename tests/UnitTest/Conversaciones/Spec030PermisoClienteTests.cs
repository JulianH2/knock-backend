extern alias messaging;

using System.Net;
using System.Text;
using AwesomeAssertions;
using Knock.Contracts.Conversations;
using messaging::Knock.Messaging.Api.Modules.Conversaciones.Data;
using Xunit;

namespace Knock.UnitTest.Conversaciones;

/// <summary>
/// Spec 030, T-04 (R-09, CA-09): el cliente de Mensajería hacia la ruta de permiso de Mercado, con la caché de 60 segundos y sin asumir nunca permiso cuando Mercado falla.
/// Mercado y la caché son de mentira; la caché real con Redis se prueba en <c>Spec030PermisoCacheTests</c>. Solo datos ficticios.
/// </summary>
public sealed class Spec030PermisoClienteTests : IDisposable
{
    private readonly List<HttpClient> _clients = [];

    private const string _request = "507f1f77bcf86cd799439011";
    private const string _technician = "507f1f77bcf86cd799439012";
    private const string _conversation = "507f1f77bcf86cd799439013";
    private const string _subject = "11111111-1111-4111-8111-111111111111";
    private const string _token = "token-ficticio";

    private const string _okBody = """
        {"solicitudId":"507f1f77bcf86cd799439011","tecnicoId":"507f1f77bcf86cd799439012","clienteId":"22222222-2222-4222-8222-222222222222",
         "tecnicoUsuarioId":"33333333-3333-4333-8333-333333333333","rol":"cliente","abierta":true,"elegido":false,"servicio":"Plomería",
         "coloniaNombre":"Colonia Centro","tecnicoPublico":{"nombre":"Técnico de Prueba"}}
        """;

    private sealed class FakeMarketHandler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(answer(request));
        }
    }

    private sealed class DictionaryCache : IPermisoCache
    {
        public Dictionary<(string Conversation, string Subject), PermisoConversacion> Entries { get; } = [];

        public Task<PermisoConversacion?> GetAsync(string conversacionId, string subject, CancellationToken cancellationToken) =>
            Task.FromResult(Entries.TryGetValue((conversacionId, subject), out var permission) ? permission : null);

        public Task SetAsync(string conversacionId, string subject, PermisoConversacion permiso, CancellationToken cancellationToken)
        {
            Entries[(conversacionId, subject)] = permiso;
            return Task.CompletedTask;
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    public void Dispose()
    {
        foreach (var client in _clients)
        {
            client.Dispose();
        }
    }

    private (MarketPermisoDeConversacion Client, FakeMarketHandler Market, DictionaryCache Cache) Build(Func<HttpRequestMessage, HttpResponseMessage> answer)
    {
        var market = new FakeMarketHandler(answer);
        var cache = new DictionaryCache();
        var http = new HttpClient(market) { BaseAddress = new Uri("http://market.invalid/") };
        _clients.Add(http);
        return (new MarketPermisoDeConversacion(http, cache), market, cache);
    }

    [Fact]
    public async Task Consultar_Granted_ReturnsThePermissionAndCachesItPerConversation_CA_030_09()
    {
        var (client, market, cache) = Build(_ => Json(HttpStatusCode.OK, _okBody));

        var result = await client.ConsultarAsync(_request, _technician, _conversation, _subject, _token, CancellationToken.None);

        result.Estado.Should().Be(EstadoPermisoConversacion.Concedido);
        result.Permiso!.Rol.Should().Be("cliente");
        result.Permiso.Abierta.Should().BeTrue();
        result.Permiso.TecnicoPublico.Nombre.Should().Be("Técnico de Prueba");
        market.Requests.Should().ContainSingle();
        cache.Entries.Should().ContainSingle().Which.Key.Should().Be((_conversation, _subject));
    }

    [Fact]
    public async Task Consultar_WithAFreshCacheEntry_DoesNotAskMarket_CA_030_09()
    {
        var (client, market, cache) = Build(_ => Json(HttpStatusCode.OK, _okBody));
        await client.ConsultarAsync(_request, _technician, _conversation, _subject, _token, CancellationToken.None);

        var second = await client.ConsultarAsync(_request, _technician, _conversation, _subject, _token, CancellationToken.None);

        second.Estado.Should().Be(EstadoPermisoConversacion.Concedido);
        second.Permiso.Should().BeEquivalentTo(cache.Entries[(_conversation, _subject)]);
        market.Requests.Should().ContainSingle("la segunda se resuelve con la caché");
    }

    [Fact]
    public async Task Consultar_MarketDownWithCachedPermission_StillGrants_CA_030_09()
    {
        var healthy = true;
        var (client, _, _) = Build(_ => healthy ? Json(HttpStatusCode.OK, _okBody) : throw new HttpRequestException("market down"));
        await client.ConsultarAsync(_request, _technician, _conversation, _subject, _token, CancellationToken.None);
        healthy = false;

        var result = await client.ConsultarAsync(_request, _technician, _conversation, _subject, _token, CancellationToken.None);

        result.Estado.Should().Be(EstadoPermisoConversacion.Concedido);
    }

    [Fact]
    public async Task Consultar_CachedPermissionOfAnotherPerson_IsNotUsed_CA_030_09()
    {
        var (client, market, cache) = Build(_ => Json(HttpStatusCode.NotFound, "{}"));
        await cache.SetAsync(_conversation, "otra-persona", new PermisoConversacion(_request, _technician, "c", "t", "cliente", true, null, false, null, "s", "c", new TecnicoPublicoConversacion("n", null)), CancellationToken.None);

        var result = await client.ConsultarAsync(_request, _technician, _conversation, _subject, _token, CancellationToken.None);

        result.Estado.Should().Be(EstadoPermisoConversacion.NoEncontrado, "el permiso en caché es de otra persona y no vale para esta");
        market.Requests.Should().ContainSingle();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task Consultar_MarketFailsWithoutCache_IsUnavailableAndNothingIsCached_CA_030_09(HttpStatusCode status)
    {
        var (client, _, cache) = Build(_ => Json(status, "{}"));

        var result = await client.ConsultarAsync(_request, _technician, _conversation, _subject, _token, CancellationToken.None);

        result.Estado.Should().Be(EstadoPermisoConversacion.NoDisponible);
        result.Permiso.Should().BeNull();
        cache.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Consultar_MarketDoesNotAnswerWithoutCache_IsUnavailable_CA_030_09()
    {
        var (client, _, cache) = Build(_ => throw new HttpRequestException("market down"));

        var result = await client.ConsultarAsync(_request, _technician, _conversation, _subject, _token, CancellationToken.None);

        result.Estado.Should().Be(EstadoPermisoConversacion.NoDisponible);
        cache.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Consultar_MarketTimesOut_IsUnavailable_CA_030_09()
    {
        var (client, _, _) = Build(_ => throw new TaskCanceledException("timeout"));

        var result = await client.ConsultarAsync(_request, _technician, _conversation, _subject, _token, CancellationToken.None);

        result.Estado.Should().Be(EstadoPermisoConversacion.NoDisponible);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("""{"solicitudId":"s","tecnicoId":"t","clienteId":"c","tecnicoUsuarioId":"u","rol":"administrador","abierta":true,"elegido":false,"servicio":"x","coloniaNombre":"y","tecnicoPublico":{"nombre":"n"}}""")]
    [InlineData("""{"solicitudId":"s","tecnicoId":"t","clienteId":"c","tecnicoUsuarioId":"u","rol":"cliente","abierta":true,"elegido":false,"servicio":"x","coloniaNombre":"y"}""")]
    public async Task Consultar_AnUnreadableOrIncompleteBody_IsUnavailableNeverGranted_CA_030_09(string body)
    {
        var (client, _, cache) = Build(_ => Json(HttpStatusCode.OK, body));

        var result = await client.ConsultarAsync(_request, _technician, _conversation, _subject, _token, CancellationToken.None);

        result.Estado.Should().Be(EstadoPermisoConversacion.NoDisponible);
        result.Permiso.Should().BeNull();
        cache.Entries.Should().BeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task Consultar_MarketRefuses_IsNotFoundAndNotCached(HttpStatusCode status)
    {
        var (client, _, cache) = Build(_ => Json(status, "{}"));

        var result = await client.ConsultarAsync(_request, _technician, _conversation, _subject, _token, CancellationToken.None);

        result.Estado.Should().Be(EstadoPermisoConversacion.NoEncontrado);
        result.Permiso.Should().BeNull();
        cache.Entries.Should().BeEmpty("un 404 puede cambiar en cuanto el técnico cotice");
    }

    [Fact]
    public async Task Consultar_WithoutAConversationYet_AlwaysAsksMarketAndCachesNothing()
    {
        var (client, market, cache) = Build(_ => Json(HttpStatusCode.OK, _okBody));

        await client.ConsultarAsync(_request, _technician, null, _subject, _token, CancellationToken.None);
        await client.ConsultarAsync(_request, _technician, null, _subject, _token, CancellationToken.None);

        market.Requests.Should().HaveCount(2);
        cache.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Consultar_ForwardsTheSessionAndEscapesTheRoute()
    {
        var (client, market, _) = Build(_ => Json(HttpStatusCode.OK, _okBody));

        await client.ConsultarAsync("a b/../c", "x&y=1", null, _subject, _token, CancellationToken.None);
        await client.ConsultarAsync(_request, null, null, _subject, _token, CancellationToken.None);

        var withTechnician = market.Requests[0];
        withTechnician.Method.Should().Be(HttpMethod.Get);
        withTechnician.Headers.Authorization!.ToString().Should().Be($"Bearer {_token}");
        withTechnician.RequestUri!.OriginalString.Should().Be("http://market.invalid/api/v1/solicitudes/a%20b%2F..%2Fc/conversacion?tecnicoId=x%26y%3D1");
        market.Requests[1].RequestUri!.Query.Should().BeEmpty("el técnico no manda tecnicoId");
    }

    [Fact]
    public async Task Consultar_ACanceledCallerIsNotTurnedIntoUnavailable()
    {
        var (client, _, _) = Build(_ => throw new OperationCanceledException());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var act = () => client.ConsultarAsync(_request, _technician, null, _subject, _token, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void CacheKey_IsPerConversationAndPerson_AndNeverCarriesTheSubject()
    {
        var key = PermisoRedisCache.Key(_conversation, _subject);

        key.Should().StartWith($"conv:permiso:{_conversation}:");
        key.Should().NotContain(_subject);
        key.Should().NotBe(PermisoRedisCache.Key(_conversation, "otra-persona"));
        key.Should().NotBe(PermisoRedisCache.Key("507f1f77bcf86cd799439099", _subject));
        key.Should().Be(PermisoRedisCache.Key(_conversation, _subject));
        PermisoRedisCache.Lifetime.Should().Be(TimeSpan.FromSeconds(60));
    }
}
