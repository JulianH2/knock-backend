using AwesomeAssertions;
using Knock.Contracts.Catalog;
using Knock.Market.Api.Common.Crypto;
using Knock.Market.Api.Modules.Catalog;
using Knock.Market.Api.Modules.Tecnicos.Data;
using Knock.Market.Api.Modules.Tecnicos.Dtos;
using Knock.Market.Api.Modules.Tecnicos.Services;
using Knock.Market.Api.Modules.Tecnicos.Validators;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Xunit;

namespace Knock.UnitTest.Tecnicos;

/// <summary>Registro como técnico (spec 046, TEC-01): reglas de forma, reglas de negocio y documento guardado. Solo datos ficticios.</summary>
public sealed class Spec046RegistroTests
{
    private const string _colonia = "6adfe980511e3d878359ca07";
    private const string _workshop = "Calle Ficticia 123, Colonia Piloto";

    internal static RegisterTechnicianBody Valid(
        string atencion = "domicilio", string? taller = null, int? radio = 5, IReadOnlyList<string>? oficios = null, bool? terminos = true,
        TechnicianProfileBody? perfil = null, string nombre = "Ana Pruebas", string? otro = null, string colonia = _colonia) =>
        new(nombre, oficios ?? ["plo"], otro, atencion, taller, radio, colonia, perfil, terminos);

    private static Dictionary<string, string[]> Errors(RegisterTechnicianBody body) =>
        new RegisterTechnicianBodyValidator().Validate(body).Errors.GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray(), StringComparer.Ordinal);

    [Theory]
    [InlineData("domicilio", null, 5)]
    [InlineData("taller", _workshop, null)]
    [InlineData("ambos", _workshop, 12)]
    public void Cuerpo_Valido_Por_Forma_De_Atencion_CA_046_01(string atencion, string? taller, int? radio) =>
        Errors(Valid(atencion, taller, radio)).Should().BeEmpty();

    [Theory]
    [InlineData(4)]
    [InlineData(0)]
    [InlineData(13)]
    public void Radio_Fuera_De_La_Lista_Responde_422_CA_046_55(int radio) =>
        Errors(Valid(radio: radio)).Should().ContainKey("RadioKm");

    [Fact]
    public void Oficio_Repetido_O_Vacio_O_Mal_Formado_Responde_422_CA_046_55()
    {
        Errors(Valid(oficios: ["plo", "plo"])).Should().ContainKey("Oficios");
        Errors(Valid(oficios: [])).Should().ContainKey("Oficios");
        Errors(Valid(oficios: ["PLO$"])).Should().ContainKey("Oficios");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(null)]
    public void Sin_Terminos_Responde_422_CA_046_55(bool? terminos) =>
        Errors(Valid(terminos: terminos)).Should().ContainKey("AceptaTerminos");

    [Fact]
    public void Taller_Sin_Direccion_O_Domicilio_Con_Direccion_Responde_422_CA_046_03()
    {
        Errors(Valid("taller", taller: null, radio: null)).Should().ContainKey("DireccionTaller");
        Errors(Valid("taller", taller: "corta", radio: null)).Should().ContainKey("DireccionTaller");
        Errors(Valid("domicilio", taller: _workshop)).Should().ContainKey("DireccionTaller");
        Errors(Valid("taller", taller: _workshop, radio: 5)).Should().ContainKey("RadioKm");
        Errors(Valid("domicilio", radio: null)).Should().ContainKey("RadioKm");
    }

    [Fact]
    public void Valores_Invalidos_No_Se_Repiten_En_El_Error_CA_046_03()
    {
        var body = Valid(atencion: "<script>", nombre: "1", colonia: "no-es-id", perfil: new TechnicianProfileBody(new string('x', 61), new string('y', 301), 61));
        var errors = Errors(body);
        errors.Keys.Should().Contain(["Atencion", "Nombre", "ColoniaId", "perfil.nombreNegocio", "perfil.descripcion", "perfil.anios"]);
        errors.Values.SelectMany(m => m).Should().AllSatisfy(m => m.Should().Be("The value is not valid."));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(61)]
    public void Anios_Fuera_De_Rango_Responde_422_CA_046_03(int anios) =>
        Errors(Valid(perfil: new TechnicianProfileBody(null, null, anios))).Should().ContainKey("perfil.anios");

    [Fact]
    public async Task Registro_Valido_Crea_Perfil_Activo_Sin_Verificacion_CA_046_55()
    {
        var harness = new Harness();
        var outcome = await harness.Service.RegisterAsync("sub-1", Valid(perfil: new TechnicianProfileBody(" Plomería Ana ", "  ", 4)), CancellationToken.None);

        outcome.Status.Should().Be(RegisterTechnicianStatus.Created);
        outcome.Technician!.Estado.Should().Be("activo");
        outcome.Technician.Verificacion.Should().Be("ninguno");
        outcome.Technician.DisponibleHoy.Should().BeFalse();
        outcome.Technician.NombreNegocio.Should().Be("Plomería Ana");
        outcome.Technician.Descripcion.Should().BeNull();
        var saved = harness.Store.Saved.Should().ContainSingle().Subject;
        saved.Sub.Should().Be("sub-1");
        saved.TallerDireccionCifrada.Should().BeNull();
        saved.RadioKm.Should().Be(5);
        saved.Centro.Should().Be((20.05, -99.34));
    }

    [Fact]
    public async Task Persona_Que_Ya_Es_Tecnico_Responde_409_Y_No_Guarda_CA_046_05()
    {
        var harness = new Harness();
        harness.Store.Existing.Add("sub-1");
        (await harness.Service.RegisterAsync("sub-1", Valid(), CancellationToken.None)).Status.Should().Be(RegisterTechnicianStatus.AlreadyTechnician);
        harness.Store.Saved.Should().BeEmpty();
    }

    [Fact]
    public async Task Registro_Repetido_Que_Choca_Con_El_Indice_Unico_Responde_409_CA_046_69()
    {
        var harness = new Harness();
        harness.Store.FailAsDuplicate = true;
        (await harness.Service.RegisterAsync("sub-1", Valid(), CancellationToken.None)).Status.Should().Be(RegisterTechnicianStatus.AlreadyTechnician);
    }

    [Fact]
    public async Task Oficio_Inactivo_Responde_422_Y_No_Guarda_CA_046_55()
    {
        var harness = new Harness();
        (await harness.Service.RegisterAsync("sub-1", Valid(oficios: ["plo", "zzz"]), CancellationToken.None)).Status.Should().Be(RegisterTechnicianStatus.TradeInvalid);
        harness.Store.Saved.Should().BeEmpty();
    }

    [Fact]
    public async Task Colonia_Inexistente_Responde_422_Y_No_Guarda_CA_046_03()
    {
        var harness = new Harness();
        (await harness.Service.RegisterAsync("sub-1", Valid(colonia: "000000000000000000000000"), CancellationToken.None)).Status
            .Should().Be(RegisterTechnicianStatus.ColoniaNotFound);
        harness.Store.Saved.Should().BeEmpty();
    }

    [Fact]
    public async Task Direccion_Del_Taller_Se_Guarda_Cifrada_Y_Ligada_Al_Documento_CA_046_67()
    {
        var harness = new Harness();
        await harness.Service.RegisterAsync("sub-1", Valid("ambos", _workshop, 8), CancellationToken.None);
        var saved = harness.Store.Saved.Single();
        var document = TechnicianProfileStore.ToDocument(saved);

        document["tallerDireccion"].AsString.Should().NotContain("Ficticia");
        WorkshopAddressCipher.Read(harness.Cipher, saved.Id, document["tallerDireccion"].AsString, demo: false).Should().Be(_workshop);
        WorkshopAddressCipher.Read(harness.Cipher, ObjectId.GenerateNewId(), document["tallerDireccion"].AsString, demo: false).Should().BeNull();
        document.ToJson().Should().NotContain(_workshop);
    }

    [Fact]
    public void Documento_Usa_Los_Campos_Que_Lee_La_Elegibilidad_CA_046_55()
    {
        var now = new DateTime(2026, 11, 17, 18, 0, 0, DateTimeKind.Utc);
        var technician = new NewTechnician(ObjectId.GenerateNewId(), "sub-9", "Ana Pruebas", ["plo", "ele"], null, "domicilio", null, 5, _colonia,
            (20.05, -99.34), null, now);
        var document = TechnicianProfileStore.ToDocument(technician);

        document["usuarioId"].AsString.Should().Be("sub-9");
        document["estado"].AsString.Should().Be("activo");
        document["oculto"].AsBoolean.Should().BeFalse();
        document["coloniaBaseId"].AsString.Should().Be(_colonia);
        document["radioAtencionKm"].AsInt32.Should().Be(5);
        document["ubicacionBase"]["coordinates"].AsBsonArray.Select(v => v.AsDouble).Should().Equal(-99.34, 20.05);
        document["disponibleHasta"].ToUniversalTime().Should().Be(now);
        document.Names.Should().NotContain(["tallerDireccion", "telefono", "lat", "lng"]);
    }

    [Fact]
    public void Direccion_De_Taller_De_Demostracion_Se_Lee_En_Claro_CA_046_67()
    {
        var cipher = Harness.NewCipher();
        WorkshopAddressCipher.Read(cipher, ObjectId.GenerateNewId(), _workshop, demo: true).Should().Be(_workshop);
        WorkshopAddressCipher.Read(cipher, ObjectId.GenerateNewId(), "texto en claro", demo: false).Should().BeNull();
        WorkshopAddressCipher.Read(cipher, ObjectId.GenerateNewId(), null, demo: false).Should().BeNull();
    }

    private sealed class Harness
    {
        public Harness()
        {
            Cipher = NewCipher();
            Service = new TechnicianRegistrationService(Store, new FakeCatalog(), new FakeCenters(), Cipher,
                new FixedClock(new DateTimeOffset(2026, 11, 17, 18, 0, 0, TimeSpan.Zero)));
        }

        public FakeStore Store { get; } = new();

        public FieldCipher Cipher { get; }

        public TechnicianRegistrationService Service { get; }

        public static FieldCipher NewCipher() => new(Options.Create(new FieldEncryptionOptions
        {
            ActiveVersion = "1",
            Keys = new Dictionary<string, string> { ["1"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)) },
        }));
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeStore : ITechnicianProfileStore
    {
        public HashSet<string> Existing { get; } = new(StringComparer.Ordinal);

        public List<NewTechnician> Saved { get; } = [];

        public bool FailAsDuplicate { get; set; }

        public Task<bool> ExistsAsync(string sub, CancellationToken cancellationToken) => Task.FromResult(Existing.Contains(sub));

        public Task<bool> InsertAsync(NewTechnician technician, CancellationToken cancellationToken)
        {
            if (FailAsDuplicate) return Task.FromResult(false);
            Saved.Add(technician);
            return Task.FromResult(true);
        }
    }

    private sealed class FakeCenters : INeighborhoodCenterCatalog
    {
        public Task<(double Latitude, double Longitude)?> FindNeighborhoodCenterAsync(string coloniaId, CancellationToken cancellationToken) =>
            Task.FromResult<(double, double)?>((20.05, -99.34));
    }

    private sealed class FakeCatalog : ICatalogApi
    {
        public string ModuleName => "Catalog";

        public Task<(string Nombre, string Modalidad)?> FindActiveTradeAsync(string clave, CancellationToken cancellationToken) =>
            Task.FromResult<(string, string)?>(clave is "plo" or "ele" ? ("Oficio", "domicilio") : null);

        public Task<(string Nombre, string Cp)?> FindNeighborhoodAsync(string coloniaId, CancellationToken cancellationToken) =>
            Task.FromResult<(string, string)?>(coloniaId == _colonia ? ("Centro", "42800") : null);

        public Task<string?> FindNeighborhoodAtAsync(double latitude, double longitude, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<string>> FindNeighborhoodsByPostalCodeAsync(string postalCode, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> NeighborhoodExistsAsync(string coloniaId, string cp, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
