using AwesomeAssertions;
using Knock.Contracts.Catalog;
using Knock.Market.Api.Modules.Catalog;
using Knock.Market.Api.Modules.Tecnicos.Dtos;
using Knock.Market.Api.Modules.Tecnicos.Services;
using Knock.Market.Api.Modules.Tecnicos.Validators;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using Xunit;

namespace Knock.UnitTest.Tecnicos;

/// <summary>«Llega a mi colonia» (TEC-06) y autocompletado (TEC-03) de la spec 046. Solo datos ficticios.</summary>
public sealed class Spec046BusquedaTests : IDisposable
{
    private const string _base = "6adfe980511e3d878359ca07";
    private const string _near = "6adfe980511e3d878359ca08";
    private const string _far = "6adfe980511e3d878359ca09";
    private static readonly DateTimeOffset _now = new(2026, 11, 17, 18, 0, 0, TimeSpan.Zero);
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public void Dispose() => _cache.Dispose();

    private static TechnicianCoverage Coverage(string atencion = "domicilio", int? radio = 5, string estado = "activo", bool oculto = false) =>
        new(estado, oculto, null, _base, atencion, radio, 20.05, -99.34);

    private (TechnicianLookupService Service, FakeReader Reader) Create(TechnicianCoverage? coverage)
    {
        var reader = new FakeReader(coverage);
        return (new TechnicianLookupService(reader, new FakeCatalog(), new FakeCenters(), _cache, new FixedClock(_now)), reader);
    }

    [Fact]
    public async Task Atiende_Su_Colonia_Base_CA_046_59()
    {
        var (service, _) = Create(Coverage(atencion: "taller", radio: null));
        (await service.CoverageAsync(ObjectId.GenerateNewId(), _base, CancellationToken.None)).Atiende.Should().BeTrue();
    }

    [Fact]
    public async Task A_Domicilio_Atiende_Dentro_De_Su_Radio_Y_No_Fuera_CA_046_59()
    {
        var (service, _) = Create(Coverage(radio: 5));
        (await service.CoverageAsync(ObjectId.GenerateNewId(), _near, CancellationToken.None)).Atiende.Should().BeTrue();
        (await service.CoverageAsync(ObjectId.GenerateNewId(), _far, CancellationToken.None)).Atiende.Should().BeFalse();
    }

    [Fact]
    public async Task Solo_Taller_No_Atiende_Otra_Colonia_CA_046_59()
    {
        var (service, _) = Create(Coverage(atencion: "taller", radio: null));
        (await service.CoverageAsync(ObjectId.GenerateNewId(), _near, CancellationToken.None)).Atiende.Should().BeFalse();
    }

    [Fact]
    public async Task Colonia_Fuera_Del_Catalogo_Responde_422_CA_046_59()
    {
        var (service, _) = Create(Coverage());
        (await service.CoverageAsync(ObjectId.GenerateNewId(), "000000000000000000000000", CancellationToken.None)).Status
            .Should().Be(CoverageStatus.ColoniaNotFound);
    }

    [Theory]
    [InlineData("suspendido", false)]
    [InlineData("activo", true)]
    public async Task Tecnico_Oculto_O_No_Activo_Es_No_Encontrado_CA_046_23(string estado, bool oculto)
    {
        var (service, _) = Create(Coverage(estado: estado, oculto: oculto));
        (await service.CoverageAsync(ObjectId.GenerateNewId(), _base, CancellationToken.None)).Status.Should().Be(CoverageStatus.TechnicianNotFound);
    }

    [Fact]
    public async Task Consulta_Repetida_Sale_De_La_Cache_CA_046_24()
    {
        var (service, reader) = Create(Coverage());
        var id = ObjectId.GenerateNewId();
        await service.CoverageAsync(id, _near, CancellationToken.None);
        await service.CoverageAsync(id, _near.ToUpperInvariant(), CancellationToken.None);
        reader.CoverageCalls.Should().Be(1);
    }

    [Theory]
    [InlineData("x")]
    [InlineData(" a ")]
    [InlineData("frenos!")]
    [InlineData("a$b")]
    public void Texto_Invalido_Responde_422_CA_046_57(string q) =>
        new AutocompleteQueryValidator().Validate(new AutocompleteQuery { Q = q }).IsValid.Should().BeFalse();

    [Fact]
    public void Texto_De_41_Caracteres_Responde_422_CA_046_57() =>
        new AutocompleteQueryValidator().Validate(new AutocompleteQuery { Q = new string('a', 41) }).IsValid.Should().BeFalse();

    [Fact]
    public async Task Sugerencias_Sin_Acentos_Primero_Oficios_Y_Maximo_Ocho_CA_046_57()
    {
        var (service, reader) = Create(null);
        reader.Businesses = [.. Enumerable.Range(1, 10).Select(i => new BusinessName(ObjectId.GenerateNewId().ToString(), $"Plomería {i}"))];

        var response = await service.AutocompleteAsync("plomeria", CancellationToken.None);

        response.Sugerencias.Should().HaveCount(8);
        response.Sugerencias[0].Should().Be(new Suggestion("oficio", "Plomería", "plo", null));
        response.Sugerencias.Skip(1).Should().OnlyContain(s => s.Tipo == "negocio");
    }

    [Fact]
    public async Task Autocompletado_Repetido_Sale_De_La_Cache_CA_046_12()
    {
        var (service, reader) = Create(null);
        await service.AutocompleteAsync("frenos", CancellationToken.None);
        await service.AutocompleteAsync(" FRENOS ", CancellationToken.None);
        reader.BusinessCalls.Should().Be(1);
    }

    [Fact]
    public void Distancia_Entre_Dos_Puntos_Conocidos_CA_046_59() =>
        TechnicianLookupService.DistanceKm(20.05, -99.34, 20.05, -99.30).Should().BeApproximately(4.18, 0.05);

    private sealed class FakeReader(TechnicianCoverage? coverage) : ITechnicianLookupReader
    {
        public int CoverageCalls { get; private set; }

        public int BusinessCalls { get; private set; }

        public IReadOnlyList<BusinessName> Businesses { get; set; } = [];

        public Task<TechnicianCoverage?> FindCoverageAsync(ObjectId id, CancellationToken cancellationToken)
        {
            CoverageCalls++;
            return Task.FromResult(coverage);
        }

        public Task<IReadOnlyList<BusinessName>> FindBusinessNamesAsync(string text, int limit, CancellationToken cancellationToken)
        {
            BusinessCalls++;
            return Task.FromResult<IReadOnlyList<BusinessName>>([.. Businesses.Take(limit)]);
        }

        public Task<TechnicianListPage> FindListAsync(TechnicianListQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(new TechnicianListPage([], null));
    }

    private sealed class FakeCenters : INeighborhoodCenterCatalog
    {
        public Task<(double Latitude, double Longitude)?> FindNeighborhoodCenterAsync(string coloniaId, CancellationToken cancellationToken) =>
            Task.FromResult<(double, double)?>(coloniaId == _near ? (20.05, -99.32) : (20.50, -99.90));
    }

    private sealed class FakeCatalog : ICatalogApi
    {
        public string ModuleName => "Catalog";

        public Task<(string Nombre, string Modalidad)?> FindActiveTradeAsync(string clave, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<(string Nombre, string Cp)?> FindNeighborhoodAsync(string coloniaId, CancellationToken cancellationToken) =>
            Task.FromResult<(string, string)?>(coloniaId is _base or _near or _far ? ("Colonia", "42800") : null);

        public Task<string?> FindNeighborhoodAtAsync(double latitude, double longitude, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<string>> FindNeighborhoodsByPostalCodeAsync(string postalCode, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> NeighborhoodExistsAsync(string coloniaId, string cp, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<(string Clave, string Nombre)>> ListActiveTradesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<(string, string)>>([("ele", "Electricidad"), ("plo", "Plomería"), ("mec", "Mecánica")]);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
