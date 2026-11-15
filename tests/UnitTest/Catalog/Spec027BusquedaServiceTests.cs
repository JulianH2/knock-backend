using System.Security.Cryptography;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Market.Api.Modules.Catalog.Data;
using Knock.Market.Api.Modules.Catalog.Domain;
using Knock.Market.Api.Modules.Catalog.Services;
using Xunit;

namespace Knock.UnitTest.Catalog;

public sealed class Spec027BusquedaServiceTests
{
    [Fact]
    public async Task Detecta_Oficio_Y_No_Expone_Datos_CA_027_01_CA_027_13()
    {
        var source = new FakeSource();
        source.Items = [new(new SearchCandidate("id", "Taller demo", ["plo"], 4, 1, "basica", "Zona", null, false, true, 1.12345), new SearchRank("id", 1, true, 4, 1.12345))];
        var result = await Create(source).SearchAsync(new("boiler", "zona", null, false, 20, null), CancellationToken.None);
        result.Error.Should().BeNull();
        result.Response!.Interpretation.State.Should().Be("detectado");
        result.Response.Items.Single().DistanceKm.Should().Be(1);
        JsonSerializer.Serialize(result.Response).Should().NotContain("1.12345").And.NotContain("usuarioId").And.NotContain("radioAtencionKm");
    }

    [Fact]
    public async Task Sin_Resultados_Filtro_No_Disponible_No_Cuenta_CA_027_03()
    {
        var source = new FakeSource();
        var result = await Create(source).SearchAsync(new("quiero pastel", "zona", null, false, 20, null), CancellationToken.None);
        result.Response!.Interpretation.State.Should().Be("no_entendido");
        result.Response.Items.Should().BeEmpty();
        source.CountCalls.Should().Be(0);
    }

    [Fact]
    public async Task Siete_Digitos_No_Se_Cuentan_CA_027_14()
    {
        var source = new FakeSource();
        var service = new BusquedaService(source, new FakeCache(), new AllowAllFilter(), TimeProvider.System, RandomNumberGenerator.GetBytes(32));
        await service.SearchAsync(new("9876543", "zona", null, false, 20, null), CancellationToken.None);
        source.CountCalls.Should().Be(0);
    }

    [Fact]
    public async Task Segunda_Consulta_Reutiliza_Pagina_En_Cache_R_027_13()
    {
        var source = new FakeSource();
        var service = Create(source);
        var input = new SearchInput("boiler", "zona", null, false, 20, null);
        await service.SearchAsync(input, CancellationToken.None);
        await service.SearchAsync(input, CancellationToken.None);
        source.PageCalls.Should().Be(1);
        source.ContextCalls.Should().Be(1);
    }

    [Fact]
    public async Task Fuente_No_Disponible_Retorna_Error_Sin_Detalle_CA_027_17()
    {
        var source = new FakeSource { Available = false };
        var result = await Create(source).SearchAsync(new("boiler", "zona", null, false, 20, null), CancellationToken.None);
        result.Error.Should().Be("search_unavailable");
        result.Response.Should().BeNull();
    }

    private static BusquedaService Create(FakeSource source) => new(source, new FakeCache(), new FiltroDatosPersonalesNoDisponible(), TimeProvider.System, RandomNumberGenerator.GetBytes(32));

    private sealed class FakeSource : IBusquedaSource
    {
        public bool Available { get; init; } = true;
        public IReadOnlyList<RankedCandidate> Items { get; set; } = [];
        public int PageCalls { get; private set; }
        public int CountCalls { get; private set; }
        public int ContextCalls { get; private set; }
        public Task<SearchContext?> GetContextAsync(string colonia, CancellationToken token)
        {
            ContextCalls++;
            return Task.FromResult(Available ? new SearchContext(true, null, null, 1, [new("plo", "Plomería")], [new("boiler", "plo")]) : null);
        }
        public Task<IReadOnlyList<RankedCandidate>?> FindPageAsync(SearchPageRequest query, SearchRank? after, int limit, CancellationToken token)
        {
            PageCalls++;
            return Task.FromResult<IReadOnlyList<RankedCandidate>?>(Items);
        }
        public Task<bool> CountAsync(string term, DateTimeOffset now, CancellationToken token) { CountCalls++; return Task.FromResult(true); }
    }

    private sealed class FakeCache : ISearchResponseCache
    {
        private readonly Dictionary<string, string> _items = new(StringComparer.Ordinal);
        public Task<long> GetSearchGenerationAsync(CancellationToken cancellationToken) => Task.FromResult(0L);
        public Task<string?> GetSearchAsync(byte[] fingerprint, CancellationToken cancellationToken) => Task.FromResult(_items.GetValueOrDefault(Convert.ToHexString(fingerprint)));
        public Task SetSearchAsync(byte[] fingerprint, string response, CancellationToken cancellationToken) { _items[Convert.ToHexString(fingerprint)] = response; return Task.CompletedTask; }
    }

    private sealed class AllowAllFilter : IFiltroDatosPersonales
    {
        public bool CanCount(string originalText) => true;
    }
}
