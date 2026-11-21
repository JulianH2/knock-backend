using AwesomeAssertions;
using Knock.Identity.Api.Common.Errors;
using Knock.Identity.Api.Modules.Trust.Data;
using Knock.Identity.Api.Modules.Trust.Dtos;
using Knock.Identity.Api.Modules.Trust.Services;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Xunit;

namespace Knock.UnitTest.Identity;

/// <summary>Spec 025, T-05 (versión sin fotos): reglas de creación y consulta con un almacén de mentira. CURP ficticia.</summary>
public sealed class Spec025ServicioTests
{
    private const string _curp = "GODE561231HDFRRN09";
    private static readonly DateTimeOffset _now = new(2026, 11, 20, 18, 0, 0, TimeSpan.Zero);

    private static CreateVerificationRequest Request(string level = "basica", bool? consent = true, string version = "1.0", string curp = _curp) =>
        new(level, consent, version, curp);

    private static (VerificationService Service, FakeStore Store) Create(string state = "activa", StoredVerification? latest = null, string? device = "dispositivo-1")
    {
        var store = new FakeStore(state, latest, device);
        var options = Options.Create(new VerificationOptions { AvisoVersion = "1.0", AvisoTexto = "Texto del aviso" });
        return (new VerificationService(store, options, new FixedClock(_now)), store);
    }

    private static StoredVerification Latest(string state, string? granted = null, string? reason = null) =>
        new(ObjectId.GenerateNewId(), "basica", state, granted, reason, [new StoredPhoto("ine_frente", "cifrada"), new StoredPhoto("selfie", "cifrada")],
            _now.UtcDateTime.AddDays(-1), state == "en_revision" ? _now.UtcDateTime : null, null, state == "en_revision" ? _now.UtcDateTime.AddDays(7) : null);

    private static async Task<DomainException> FailsAsync(VerificationService service, CreateVerificationRequest request) =>
        (await ((Func<Task>)(() => service.CreateAsync("sub-1", "sid-1", request, CancellationToken.None))).Should().ThrowAsync<DomainException>()).Which;

    [Theory]
    [InlineData(false)]
    [InlineData(null)]
    public async Task Sin_Consentimiento_Responde_422_Y_No_Crea_CA_025_01(bool? consent)
    {
        var (service, store) = Create();
        (await FailsAsync(service, Request(consent: consent))).Code.Should().Be("consent_required");
        store.Created.Should().BeNull();
    }

    [Fact]
    public async Task Aviso_De_Otra_Version_Es_Consentimiento_Invalido_CA_025_01()
    {
        var (service, store) = Create();
        (await FailsAsync(service, Request(version: "0.9"))).Code.Should().Be("consent_required");
        store.Created.Should().BeNull();
    }

    [Fact]
    public async Task Registra_Consentimiento_Con_El_Dispositivo_De_La_Sesion_CA_025_02()
    {
        var (service, store) = Create(device: "dispositivo-de-la-sesion");
        await service.CreateAsync("sub-1", "sid-1", Request(), CancellationToken.None);
        store.Created!.DeviceId.Should().Be("dispositivo-de-la-sesion");
        store.Created.ConsentVersion.Should().Be("1.0");
        store.Created.ConsentText.Should().Be("Texto del aviso");
        store.Created.CreatedAt.Should().Be(_now.UtcDateTime);
    }

    [Fact]
    public async Task Nivel_Basico_Devuelve_Dos_Fotos_En_Captura_CA_025_03()
    {
        var (service, _) = Create();
        var created = await service.CreateAsync("sub-1", "sid-1", Request(), CancellationToken.None);
        created!.Estado.Should().Be("en_captura");
        created.Fotos.Select(f => f.Tipo).Should().Equal("ine_frente", "selfie");
        created.Fotos.Should().OnlyContain(f => f.Estado == "pendiente" && f.UrlSubida == null);
        created.CapturaVenceEn.Should().Be(_now.UtcDateTime.AddHours(24));
    }

    [Fact]
    public async Task Nivel_Completo_Devuelve_Tres_Fotos_CA_025_04()
    {
        var (service, _) = Create();
        (await service.CreateAsync("sub-1", "sid-1", Request("completa"), CancellationToken.None))!.Fotos.Should().HaveCount(3);
    }

    [Fact]
    public async Task Curp_Mal_Formada_Responde_422_Sin_Repetirla_CA_025_05()
    {
        var (service, store) = Create();
        var error = await FailsAsync(service, Request(curp: "NO-ES-UNA-CURP"));
        error.Code.Should().Be("curp_invalid");
        error.Message.Should().NotContain("NO-ES-UNA-CURP");
        store.Created.Should().BeNull();
    }

    [Fact]
    public async Task La_Curp_No_Llega_Al_Almacen_CA_025_06()
    {
        var (service, store) = Create();
        await service.CreateAsync("sub-1", "sid-1", Request(), CancellationToken.None);
        store.Created!.ToString().Should().NotContain(_curp);
    }

    [Fact]
    public async Task Otra_En_Revision_Responde_409_CA_025_08()
    {
        var (service, store) = Create(latest: Latest("en_revision"));
        (await FailsAsync(service, Request())).Code.Should().Be("verification_under_review");
        store.Created.Should().BeNull();
    }

    [Fact]
    public async Task Ya_Verificado_Con_El_Mismo_Nivel_O_Completo_Responde_409_CA_025_19()
    {
        var (basicService, _) = Create(latest: Latest("verificada", granted: "basica"));
        var (fullService, _) = Create(latest: Latest("verificada", granted: "completa"));
        (await FailsAsync(basicService, Request("basica"))).Code.Should().Be("already_verified");
        (await FailsAsync(fullService, Request("completa"))).Code.Should().Be("already_fully_verified");
        (await basicService.CreateAsync("sub-1", "sid-1", Request("completa"), CancellationToken.None)).Should().NotBeNull("subir de nivel sí se permite");
    }

    [Fact]
    public async Task Cuenta_En_Eliminacion_Programada_Responde_409_CA_025_21()
    {
        var (service, store) = Create(state: "eliminacion_programada");
        (await FailsAsync(service, Request())).Code.Should().Be("deletion_scheduled");
        store.Created.Should().BeNull();
    }

    [Fact]
    public async Task Sesion_Cerrada_Es_No_Autorizado_CA_025_17()
    {
        var (service, store) = Create(device: null);
        (await service.CreateAsync("sub-1", "sid-1", Request(), CancellationToken.None)).Should().BeNull();
        store.Created.Should().BeNull();
    }

    [Fact]
    public async Task Sin_Ninguna_Verificacion_El_Estado_Es_Sin_Verificar_CA_025_14()
    {
        var (service, _) = Create();
        var status = await service.GetAsync("sub-1", CancellationToken.None);
        status.Estado.Should().Be("sin_verificar");
        status.PuedeReintentar.Should().BeTrue();
        status.Id.Should().BeNull();
    }

    [Fact]
    public async Task En_Revision_Muestra_Nivel_Fotos_Y_Vencimiento_Sin_Reintento_CA_025_14()
    {
        var (service, _) = Create(latest: Latest("en_revision"));
        var status = await service.GetAsync("sub-1", CancellationToken.None);
        status.Fotos.Should().HaveCount(2);
        status.ExpiraEn.Should().Be(_now.UtcDateTime.AddDays(7));
        status.PuedeReintentar.Should().BeFalse();
        status.Motivo.Should().BeNull();
    }

    [Fact]
    public async Task Rechazo_Muestra_Motivo_Y_Consejos_CA_025_15()
    {
        var (service, _) = Create(latest: Latest("no_verificada", reason: "foto_ilegible"));
        var status = await service.GetAsync("sub-1", CancellationToken.None);
        status.Estado.Should().Be("no_verificada");
        status.Motivo.Should().Be("foto_ilegible");
        status.Consejos.Should().NotBeEmpty();
        status.PuedeReintentar.Should().BeTrue();
    }

    private sealed class FakeStore(string state, StoredVerification? latest, string? device) : IVerificationStore
    {
        public NewVerification? Created { get; private set; }

        public Task<VerificationAccount?> FindAccountAsync(string sub, CancellationToken cancellationToken) =>
            Task.FromResult<VerificationAccount?>(new VerificationAccount(ObjectId.GenerateNewId(), state));

        public Task<string?> FindSessionDeviceAsync(ObjectId userId, string sessionId, CancellationToken cancellationToken) => Task.FromResult(device);

        public Task<StoredVerification?> FindLatestAsync(string sub, CancellationToken cancellationToken) => Task.FromResult(latest);

        public Task CreateAsync(NewVerification verification, CancellationToken cancellationToken)
        {
            Created = verification;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
