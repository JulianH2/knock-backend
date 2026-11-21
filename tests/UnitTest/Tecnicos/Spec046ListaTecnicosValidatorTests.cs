using AwesomeAssertions;
using FluentValidation;
using Knock.Market.Api.Modules.Tecnicos.Domain;
using Knock.Market.Api.Modules.Tecnicos.Dtos;
using Knock.Market.Api.Modules.Tecnicos.Services;
using Knock.Market.Api.Modules.Tecnicos.Validators;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using Xunit;

namespace Knock.UnitTest.Tecnicos;

/// <summary>Validación de la consulta de TEC-02 (spec 046, R-04 y R-05): valores inválidos responden 422 sin repetirlos.</summary>
public sealed class Spec046ListaTecnicosValidatorTests
{
    private static readonly TechnicianListQueryValidator _validator = new();

    private static IReadOnlyList<string> Fields(TechnicianListQuery query) =>
        [.. _validator.Validate(new ValidationContext<TechnicianListQuery>(query)).Errors.Select(e => e.PropertyName)];

    [Fact]
    public void Orden_Invalido_Lista_Errores_CA_046_07() => Fields(new TechnicianListQuery { Orden = "masCerca" }).Should().Contain("Orden");

    [Fact]
    public void Cursor_MalFormado_Lista_Errores_CA_046_07() => Fields(new TechnicianListQuery { Cursor = "no-es-id" }).Should().Contain("Cursor");

    [Fact]
    public void RadioKm_Invalido_Lista_Errores_CA_046_07()
    {
        Fields(new TechnicianListQuery { RadioKm = "7" }).Should().Contain("RadioKm");
        Fields(new TechnicianListQuery { RadioKm = "abc" }).Should().Contain("RadioKm");
    }

    [Fact]
    public void LatLng_FueraDeRango_Lista_Errores_CA_046_07()
    {
        Fields(new TechnicianListQuery { Lat = "91" }).Should().Contain("Lat");
        Fields(new TechnicianListQuery { Lng = "-181" }).Should().Contain("Lng");
    }

    [Fact]
    public void DisponibleHoy_Y_SoloVerificados_MalFormados_Listan_Errores_CA_046_07()
    {
        Fields(new TechnicianListQuery { DisponibleHoy = "si" }).Should().Contain("DisponibleHoy");
        Fields(new TechnicianListQuery { SoloVerificados = "yes" }).Should().Contain("SoloVerificados");
    }

    [Fact]
    public void Limite_Invalido_Lista_Errores_CA_046_07()
    {
        Fields(new TechnicianListQuery { Limite = "51" }).Should().Contain("Limite");
        Fields(new TechnicianListQuery { Limite = "0" }).Should().Contain("Limite");
    }

    [Fact]
    public void Distancia_SinLatLng_Lista_Error_CA_046_07() => Fields(new TechnicianListQuery { Orden = "distancia" }).Should().Contain("Lat");

    [Fact]
    public void Consulta_Valida_NoTieneErrores_CA_046_06() => _validator.Validate(new ValidationContext<TechnicianListQuery>(new TechnicianListQuery())).IsValid.Should().BeTrue();

    private sealed class StubReader : ITechnicianLookupReader
    {
        public int FindListCalls;

        public Task<TechnicianCoverage?> FindCoverageAsync(ObjectId id, CancellationToken cancellationToken) => Task.FromResult<TechnicianCoverage?>(null);

        public Task<IReadOnlyList<BusinessName>> FindBusinessNamesAsync(string text, int limit, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BusinessName>>([]);

        public Task<TechnicianListPage> FindListAsync(TechnicianListQuery query, CancellationToken cancellationToken)
        {
            FindListCalls++;
            var record = new TechnicianProfileRecord(ObjectId.GenerateNewId(), "Tecnico", null, null, TechnicianRules.NoVerification, null, null, null, [], null, null, null, TechnicianRules.ActiveState, false, null, null);
            return Task.FromResult(new TechnicianListPage([record], null));
        }
    }

    [Fact]
    public async Task SegundaBusqueda_SaleDeLaCaché_CA_046_09()
    {
        var reader = new StubReader();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new TechnicianLookupService(reader, null!, null!, cache, TimeProvider.System);
        var query = new TechnicianListQuery();

        var first = await service.SearchAsync(query, default);
        var second = await service.SearchAsync(query, default);

        reader.FindListCalls.Should().Be(1);
        first.Status.Should().Be(TechnicianListStatus.Ok);
        second.Status.Should().Be(TechnicianListStatus.Ok);
        second.Response!.Tecnicos.Count.Should().Be(1);

        service.ForgetListCache();
        await service.SearchAsync(query, default);
        reader.FindListCalls.Should().Be(2);
    }
}
