using AwesomeAssertions;
using Knock.Contracts.Catalog;
using Knock.Market.Api.Common.Crypto;
using Knock.Market.Api.Modules.Catalog;
using Knock.Market.Api.Modules.Tecnicos.Domain;
using Knock.Market.Api.Modules.Tecnicos.Dtos;
using Knock.Market.Api.Modules.Tecnicos.Services;
using Knock.Market.Api.Modules.Tecnicos.Validators;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Xunit;

namespace Knock.UnitTest.Tecnicos;

/// <summary>Rutas del propio técnico (spec 046, TEC-08 a TEC-11): disponibilidad, horarios y edición del perfil. Solo datos ficticios.</summary>
public sealed class Spec046PropioTests : IDisposable
{
    // 2026-11-17 18:00 UTC = 12:00 en Ciudad de México (UTC-6).
    private static readonly DateTimeOffset _now = new(2026, 11, 17, 18, 0, 0, TimeSpan.Zero);
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public void Dispose() => _cache.Dispose();

    private static DayScheduleDto Open(string day, params (string Start, string End)[] ranges) =>
        new(day, true, [.. ranges.Select(r => new TimeRangeDto(r.Start, r.End))]);

    private static List<DayScheduleDto> Week() =>
        [.. ScheduleRules.Days.Select(d => d == "lunes" ? Open(d, ("09:00", "13:00"), ("15:00", "19:00")) : new DayScheduleDto(d, false, []))];

    private (MyTechnicianService Service, FakeSelfStore Store) Create(OwnTechnician? own, string[]? photoHosts = null)
    {
        var store = new FakeSelfStore(own);
        var cipher = new FieldCipher(Options.Create(new FieldEncryptionOptions
        {
            ActiveVersion = "1",
            Keys = new Dictionary<string, string> { ["1"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)) },
        }));
        var clock = new FixedClock(_now);
        var profiles = new TechnicianProfileService(new NoReader(), _cache, clock);
        var service = new MyTechnicianService(store, new FakeCatalog(), cipher, profiles,
            Options.Create(new TecnicosOptions { HostsFoto = photoHosts ?? ["files.knock.test"] }), clock);
        return (service, store);
    }

    [Fact]
    public void Disponible_Hoy_Vence_A_Las_23_59_59_De_La_Ciudad_CA_046_62() =>
        ScheduleRules.EndOfCityDayUtc(_now.UtcDateTime).Should().Be(new DateTime(2026, 11, 18, 5, 59, 59, DateTimeKind.Utc));

    [Fact]
    public void Cerca_De_La_Medianoche_UTC_Toma_El_Dia_De_La_Ciudad_CA_046_62() =>
        ScheduleRules.EndOfCityDayUtc(new DateTime(2026, 11, 18, 3, 0, 0, DateTimeKind.Utc)).Should().Be(new DateTime(2026, 11, 18, 5, 59, 59, DateTimeKind.Utc));

    [Fact]
    public async Task Encender_Y_Apagar_Disponible_Hoy_CA_046_62()
    {
        var (service, store) = Create(new OwnTechnician(ObjectId.GenerateNewId(), "domicilio", false, 5));

        var on = await service.SetAvailabilityAsync("sub-1", true, CancellationToken.None);
        on!.DisponibleHoy.Should().BeTrue();
        on.DisponibleHasta.Should().Be(new DateTime(2026, 11, 18, 5, 59, 59, DateTimeKind.Utc));

        var off = await service.SetAvailabilityAsync("sub-1", false, CancellationToken.None);
        off!.DisponibleHoy.Should().BeFalse();
        store.AvailableUntil.Should().Be(_now.UtcDateTime);
    }

    [Fact]
    public async Task Disponibilidad_De_Quien_No_Es_Tecnico_No_Cambia_Nada_CA_046_38()
    {
        var (service, store) = Create(null);
        (await service.SetAvailabilityAsync("sub-1", true, CancellationToken.None)).Should().BeNull();
        store.AvailableUntil.Should().BeNull();
    }

    [Fact]
    public void Disponibilidad_Sin_Valor_Responde_422_CA_046_39() =>
        new AvailabilityBodyValidator().Validate(new AvailabilityBody(null)).IsValid.Should().BeFalse();

    [Fact]
    public void Horario_Valido_De_Siete_Dias_CA_046_43() => ScheduleRules.IsValid(Week()).Should().BeTrue();

    [Fact]
    public void Franjas_Traslapadas_O_Al_Reves_Responden_422_CA_046_63()
    {
        var overlapped = Week();
        overlapped[0] = Open("lunes", ("09:00", "13:00"), ("12:00", "18:00"));
        var reversed = Week();
        reversed[0] = Open("lunes", ("18:00", "09:00"));
        ScheduleRules.IsValid(overlapped).Should().BeFalse();
        ScheduleRules.IsValid(reversed).Should().BeFalse();
    }

    [Fact]
    public void Horario_Incompleto_Repetido_O_Mal_Escrito_Responde_422_CA_046_46()
    {
        var repeated = Week();
        repeated[1] = repeated[0];
        var badTime = Week();
        badTime[0] = Open("lunes", ("9:00", "13:00"));
        var threeRanges = Week();
        threeRanges[0] = Open("lunes", ("08:00", "09:00"), ("10:00", "11:00"), ("12:00", "13:00"));
        var closedWithRanges = Week();
        closedWithRanges[2] = new DayScheduleDto("miercoles", false, [new TimeRangeDto("09:00", "10:00")]);

        ScheduleRules.IsValid([.. Week().Take(6)]).Should().BeFalse();
        ScheduleRules.IsValid(repeated).Should().BeFalse();
        ScheduleRules.IsValid(badTime).Should().BeFalse();
        ScheduleRules.IsValid(threeRanges).Should().BeFalse();
        ScheduleRules.IsValid(closedWithRanges).Should().BeFalse();
        ScheduleRules.IsValid(null).Should().BeFalse();
    }

    [Fact]
    public async Task Sin_Horario_Guardado_Todos_Cerrados_CA_046_40()
    {
        var (service, _) = Create(new OwnTechnician(ObjectId.GenerateNewId(), "domicilio", false, 5));
        var schedule = await service.GetScheduleAsync("sub-1", CancellationToken.None);
        schedule!.Should().HaveCount(7).And.OnlyContain(d => d.Abierto == false);
    }

    [Fact]
    public async Task Horarios_De_Quien_No_Es_Tecnico_Es_No_Encontrado_CA_046_42()
    {
        var (service, _) = Create(null);
        (await service.GetScheduleAsync("sub-1", CancellationToken.None)).Should().BeNull();
        (await service.SetScheduleAsync("sub-1", Week(), CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Cambiar_Solo_La_Descripcion_No_Toca_Lo_Demas_CA_046_61()
    {
        var (service, store) = Create(new OwnTechnician(ObjectId.GenerateNewId(), "domicilio", false, 5));
        var outcome = await service.UpdateAsync("sub-1", new UpdateTechnicianBody(null, null, null, null, null, null, null, null, "Nueva descripción", null),
            CancellationToken.None);

        outcome.Status.Should().Be(UpdateTechnicianStatus.Updated);
        store.Changes!.Keys.Should().BeEquivalentTo("descripcion", "actualizadoEn");
    }

    [Fact]
    public async Task Foto_Ajena_Responde_422_Y_No_Guarda_CA_046_61()
    {
        var (service, store) = Create(new OwnTechnician(ObjectId.GenerateNewId(), "domicilio", false, 5));
        var outcome = await service.UpdateAsync("sub-1",
            new UpdateTechnicianBody(null, null, null, null, null, null, null, null, null, "https://otro.sitio.test/foto.jpg"), CancellationToken.None);
        outcome.Status.Should().Be(UpdateTechnicianStatus.InvalidField);
        outcome.Field.Should().Be("fotoUrl");
        store.Changes.Should().BeNull();
    }

    [Fact]
    public async Task Pasar_A_Taller_Sin_Direccion_Responde_422_CA_046_34()
    {
        var (service, store) = Create(new OwnTechnician(ObjectId.GenerateNewId(), "domicilio", false, 5));
        var outcome = await service.UpdateAsync("sub-1", new UpdateTechnicianBody(null, null, null, null, null, "taller", null, null, null, null),
            CancellationToken.None);
        outcome.Field.Should().Be("direccionTaller");
        store.Changes.Should().BeNull();
    }

    [Fact]
    public async Task Pasar_A_Domicilio_Quita_La_Direccion_Del_Taller_CA_046_61()
    {
        var (service, store) = Create(new OwnTechnician(ObjectId.GenerateNewId(), "taller", true, null));
        var outcome = await service.UpdateAsync("sub-1", new UpdateTechnicianBody(null, null, null, null, null, "domicilio", null, 8, null, null),
            CancellationToken.None);
        outcome.Status.Should().Be(UpdateTechnicianStatus.Updated);
        store.Changes!["tallerDireccion"].Should().BeNull();
        store.Changes["radioAtencionKm"]!.AsInt32.Should().Be(8);
    }

    [Fact]
    public async Task Nueva_Direccion_Del_Taller_Se_Guarda_Cifrada_CA_046_67()
    {
        var (service, store) = Create(new OwnTechnician(ObjectId.GenerateNewId(), "taller", true, null));
        await service.UpdateAsync("sub-1", new UpdateTechnicianBody(null, null, null, null, null, null, "Calle Ficticia 9, Colonia Piloto", null, null, null),
            CancellationToken.None);
        store.Changes!["tallerDireccion"]!.AsString.Should().NotContain("Ficticia");
    }

    [Fact]
    public async Task Edicion_De_Quien_No_Es_Tecnico_Es_No_Encontrado_CA_046_33()
    {
        var (service, store) = Create(null);
        (await service.UpdateAsync("sub-1", new UpdateTechnicianBody("Ana", null, null, null, null, null, null, null, null, null), CancellationToken.None))
            .Status.Should().Be(UpdateTechnicianStatus.NotATechnician);
        store.Changes.Should().BeNull();
    }

    [Fact]
    public async Task Oficio_Inactivo_En_La_Edicion_Responde_422_CA_046_34()
    {
        var (service, store) = Create(new OwnTechnician(ObjectId.GenerateNewId(), "domicilio", false, 5));
        (await service.UpdateAsync("sub-1", new UpdateTechnicianBody(null, null, null, ["zzz"], null, null, null, null, null, null), CancellationToken.None))
            .Status.Should().Be(UpdateTechnicianStatus.TradeInvalid);
        store.Changes.Should().BeNull();
    }

    [Theory]
    [InlineData(61, null, null)]
    [InlineData(null, 4, null)]
    [InlineData(null, null, "http://files.knock.test/a.jpg")]
    public void Edicion_Con_Valores_Invalidos_Responde_422_CA_046_34(int? anios, int? radio, string? foto) =>
        new UpdateTechnicianBodyValidator().Validate(new UpdateTechnicianBody(null, null, anios, null, null, null, null, radio, null, foto)).IsValid.Should().BeFalse();

    private sealed class FakeSelfStore(OwnTechnician? own) : ITechnicianSelfStore
    {
        public DateTime? AvailableUntil { get; private set; }

        public IReadOnlyDictionary<string, BsonValue?>? Changes { get; private set; }

        public Task<OwnTechnician?> FindOwnAsync(string sub, CancellationToken cancellationToken) => Task.FromResult(own);

        public Task<TechnicianResponse?> ReadOwnAsync(string sub, CancellationToken cancellationToken) =>
            Task.FromResult<TechnicianResponse?>(own is null ? null : new TechnicianResponse(own.Id.ToString(), "Ana", ["plo"], null, own.Atencion ?? "", own.RadioKm,
                "6adfe980511e3d878359ca07", null, null, null, "activo", "ninguno", false, _now.UtcDateTime));

        public Task<ObjectId?> SetAvailableUntilAsync(string sub, DateTime untilUtc, CancellationToken cancellationToken)
        {
            if (own is null) return Task.FromResult<ObjectId?>(null);
            AvailableUntil = untilUtc;
            return Task.FromResult<ObjectId?>(own.Id);
        }

        public Task<IReadOnlyList<DayScheduleDto>?> ReadScheduleAsync(string sub, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DayScheduleDto>?>(own is null ? null : []);

        public Task<ObjectId?> SetScheduleAsync(string sub, IReadOnlyList<DayScheduleDto> schedule, CancellationToken cancellationToken) =>
            Task.FromResult(own?.Id);

        public Task UpdateAsync(ObjectId id, IReadOnlyDictionary<string, BsonValue?> changes, CancellationToken cancellationToken)
        {
            Changes = changes;
            return Task.CompletedTask;
        }
    }

    private sealed class NoReader : ITechnicianProfileReader
    {
        public Task<TechnicianProfileRecord?> FindByIdAsync(ObjectId id, CancellationToken cancellationToken) => Task.FromResult<TechnicianProfileRecord?>(null);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeCatalog : ICatalogApi
    {
        public string ModuleName => "Catalog";

        public Task<(string Nombre, string Modalidad)?> FindActiveTradeAsync(string clave, CancellationToken cancellationToken) =>
            Task.FromResult<(string, string)?>(clave is "plo" ? ("Plomería", "domicilio") : null);

        public Task<(string Nombre, string Cp)?> FindNeighborhoodAsync(string coloniaId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<string?> FindNeighborhoodAtAsync(double latitude, double longitude, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<string>> FindNeighborhoodsByPostalCodeAsync(string postalCode, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> NeighborhoodExistsAsync(string coloniaId, string cp, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
