using AwesomeAssertions;
using Knock.Market.Api.Modules.Tecnicos.Data;
using Knock.Market.Api.Modules.Tecnicos.Dtos;
using Knock.Market.Api.Modules.Tecnicos.Services;
using Knock.Market.Api.Modules.Tecnicos.Validators;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using Xunit;

namespace Knock.UnitTest.Tecnicos;

/// <summary>Perfil público del técnico (spec 046, TEC-05): resumen o detalle, visibilidad, caché de 1 minuto y lectura del documento. Solo datos ficticios.</summary>
public sealed class Spec046PerfilTests : IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    private static readonly DateTimeOffset _now = new(2026, 11, 17, 18, 0, 0, TimeSpan.Zero);

    private static TechnicianProfileRecord Record(
        ObjectId? id = null, string estado = "activo", bool oculto = false, DateTime? ocultoHasta = null, DateTime? disponibleHasta = null,
        string? descripcion = "Arreglo fugas y calentadores", double? calificacion = 4.66) =>
        new(id ?? ObjectId.GenerateNewId(), "Ana Pruebas", "Plomería Ana", "https://files.test/ana.jpg", "basica", calificacion, 7, descripcion,
            ["plo"], "domicilio", 5, "6adfe980511e3d878359ca07", estado, oculto, ocultoHasta, disponibleHasta);

    public void Dispose() => _cache.Dispose();

    private (TechnicianProfileService Service, CountingReader Reader) Create(TechnicianProfileRecord? record, DateTimeOffset? now = null)
    {
        var reader = new CountingReader(record);
        var service = new TechnicianProfileService(reader, _cache, new FixedClock(now ?? _now));
        return (service, reader);
    }

    [Fact]
    public async Task Sin_Sesion_Solo_Resumen_Y_Con_Sesion_Detalle_CA_046_58()
    {
        var record = Record(disponibleHasta: _now.UtcDateTime.AddHours(3));
        var (service, _) = Create(record);

        var summary = await service.GetAsync(record.Id, withDetail: false, CancellationToken.None);
        var detail = await service.GetAsync(record.Id, withDetail: true, CancellationToken.None);

        summary!.Detalle.Should().BeNull();
        summary.Nombre.Should().Be("Ana Pruebas");
        summary.Calificacion.Should().Be(4.7);
        summary.Verificacion.Should().Be("basica");
        detail!.Detalle.Should().NotBeNull();
        detail.Detalle.DisponibleHoy.Should().BeTrue();
        detail.Detalle.RadioKm.Should().Be(5);
    }

    [Fact]
    public async Task Respuesta_Sin_Telefono_Direccion_Ni_Ubicacion_CA_046_58()
    {
        var record = Record();
        var (service, _) = Create(record);
        var json = System.Text.Json.JsonSerializer.Serialize(await service.GetAsync(record.Id, withDetail: true, CancellationToken.None));
        json.Should().NotContainAny("telefono", "whatsapp", "tallerDireccion", "direccion", "ubicacion", "coordinates", "usuarioId");
    }

    [Theory]
    [InlineData("suspendido", false, false)]
    [InlineData("activo", true, false)]
    [InlineData("activo", false, true)]
    public async Task Tecnico_Oculto_O_No_Activo_Es_No_Encontrado_CA_046_18(string estado, bool oculto, bool ocultoEnElFuturo)
    {
        var record = Record(estado: estado, oculto: oculto, ocultoHasta: ocultoEnElFuturo ? _now.UtcDateTime.AddDays(1) : null);
        var (service, _) = Create(record);
        (await service.GetAsync(record.Id, withDetail: true, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Ocultamiento_Vencido_Vuelve_A_Mostrarlo_CA_046_18()
    {
        var record = Record(ocultoHasta: _now.UtcDateTime.AddMinutes(-1));
        var (service, _) = Create(record);
        (await service.GetAsync(record.Id, withDetail: false, CancellationToken.None)).Should().NotBeNull();
    }

    [Fact]
    public async Task Tecnico_Inexistente_Es_No_Encontrado_CA_046_18()
    {
        var (service, _) = Create(null);
        (await service.GetAsync(ObjectId.GenerateNewId(), withDetail: false, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Consulta_Repetida_Sale_De_La_Cache_Con_Una_Sola_Lectura_CA_046_19()
    {
        var record = Record();
        var (service, reader) = Create(record);

        await service.GetAsync(record.Id, withDetail: false, CancellationToken.None);
        await service.GetAsync(record.Id, withDetail: true, CancellationToken.None);

        reader.Calls.Should().Be(1);
        TechnicianProfileService.CacheKey(record.Id).Should().Be($"tecnicos:perfil:{record.Id}");
    }

    [Fact]
    public async Task Olvidar_El_Perfil_Obliga_A_Leerlo_De_Nuevo_CA_046_19()
    {
        var record = Record();
        var (service, reader) = Create(record);
        await service.GetAsync(record.Id, withDetail: false, CancellationToken.None);
        service.Forget(record.Id);
        await service.GetAsync(record.Id, withDetail: false, CancellationToken.None);
        reader.Calls.Should().Be(2);
    }

    [Fact]
    public async Task Disponible_Hoy_Vencido_Sale_Apagado_CA_046_58()
    {
        var record = Record(disponibleHasta: _now.UtcDateTime.AddSeconds(-1));
        var (service, _) = Create(record);
        (await service.GetAsync(record.Id, withDetail: true, CancellationToken.None))!.Detalle!.DisponibleHoy.Should().BeFalse();
    }

    [Fact]
    public void Descripcion_Corta_Recorta_A_140_CA_046_58()
    {
        TechnicianProfileService.Shorten(new string('a', 300))!.Length.Should().Be(140);
        TechnicianProfileService.Shorten(new string('a', 300))!.Should().EndWith("…");
        TechnicianProfileService.Shorten("  corta  ").Should().Be("corta");
        TechnicianProfileService.Shorten("   ").Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzz")]
    [InlineData("6adfe980511e3d878359ca07x")]
    public void Identificador_Mal_Formado_Responde_422_CA_046_17(string id) =>
        new TechnicianIdRouteValidator().Validate(new TechnicianIdRoute { Id = id }).Errors.Should().Contain(e => e.PropertyName == "Id");

    [Fact]
    public void Lectura_Del_Documento_Trata_Oculto_Ausente_Como_Oculto_CA_046_18()
    {
        var document = new BsonDocument { ["_id"] = ObjectId.GenerateNewId(), ["nombre"] = "Ana", ["estado"] = "activo" };
        TechnicianProfileStore.ToProfile(document).Oculto.Should().BeTrue();
        document["oculto"] = false;
        TechnicianProfileStore.ToProfile(document).Oculto.Should().BeFalse();
    }

    private sealed class CountingReader(TechnicianProfileRecord? record) : ITechnicianProfileReader
    {
        public int Calls { get; private set; }

        public Task<TechnicianProfileRecord?> FindByIdAsync(ObjectId id, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(record?.Id == id ? record : null);
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
