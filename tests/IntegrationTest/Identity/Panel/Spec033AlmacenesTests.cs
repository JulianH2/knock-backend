extern alias IdentityApi;

using System.Security.Cryptography;
using AwesomeAssertions;
using IdentityApi::Knock.Identity.Api.Modules.Panel.Data;
using IdentityApi::Knock.Identity.Api.Modules.Panel.Domain;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Identity.Panel;

/// <summary>
/// Spec 033, T-03: los almacenes de MongoDB y de Redis del panel contra documentdb-local y Redis reales (contenedores de pruebas). Solo datos ficticios.
/// </summary>
public class Spec033AlmacenesTests
{
    private static readonly string[] _admin = ["administrador"];

    private static NuevaCuentaPanel NewAccount(string sub, byte[]? secret = null) =>
        new(sub, "Ana de prueba", _admin, DateTime.UtcNow, secret, secret is null ? 0 : 1);

    private static string Fingerprint() => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));

    private static NuevaSesionPanel NewSession(string sub, DateTime? createdAt = null)
    {
        var created = createdAt ?? DateTime.UtcNow;
        return new NuevaSesionPanel(Guid.NewGuid().ToString("N"), sub, Fingerprint(), 1, created, created.AddMinutes(20), created.AddHours(8));
    }

    [Fact]
    public async Task Cuenta_Se_Crea_Una_Sola_Vez_Por_Sub_Y_Empieza_Pendiente_R_01()
    {
        using var factory = await IdentityApiFactory.CreateAsync();
        var accounts = factory.Services.GetRequiredService<IPanelCuentasStore>();

        (await accounts.InsertAsync(NewAccount("sub-1"), CancellationToken.None)).Should().BeTrue();
        (await accounts.InsertAsync(NewAccount("sub-1"), CancellationToken.None)).Should().BeFalse("el índice único en sub lo impide");

        var stored = (await accounts.FindBySubAsync("sub-1", CancellationToken.None))!;
        stored.TotpEstado.Should().Be("pendiente");
        stored.TieneTotp.Should().BeFalse();
        stored.PermisosVersion.Should().Be(1);
        stored.EstaActiva.Should().BeTrue();
    }

    [Fact]
    public async Task Cada_Cambio_De_Roles_Estado_O_Totp_Aumenta_Permisos_Version_En_Uno_R_03()
    {
        using var factory = await IdentityApiFactory.CreateAsync();
        var accounts = factory.Services.GetRequiredService<IPanelCuentasStore>();
        await accounts.InsertAsync(NewAccount("sub-1", [1, 2, 3]), CancellationToken.None);

        (await accounts.SetRolesAsync("sub-1", ["verificador"], DateTime.UtcNow, CancellationToken.None))!.PermisosVersion.Should().Be(2);
        (await accounts.SetEstadoAsync("sub-1", "deshabilitada", DateTime.UtcNow, CancellationToken.None))!.PermisosVersion.Should().Be(3);
        var reset = (await accounts.ResetTotpAsync("sub-1", DateTime.UtcNow, CancellationToken.None))!;

        reset.PermisosVersion.Should().Be(4);
        reset.TotpEstado.Should().Be("pendiente");
        reset.TotpSecretoCifrado.Should().BeNull();
        (await accounts.SetRolesAsync("no-existe", ["verificador"], DateTime.UtcNow, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Contador_De_Fallos_Vive_En_Mongo_Y_El_Quinto_Bloquea_Hasta_El_Fin_De_La_Ventana_CA_033_28()
    {
        using var factory = await IdentityApiFactory.CreateAsync();
        var accounts = factory.Services.GetRequiredService<IPanelCuentasStore>();
        await accounts.InsertAsync(NewAccount("sub-1", [1]), CancellationToken.None);
        var now = DateTime.UtcNow;

        for (var failure = 1; failure <= 4; failure++)
        {
            var current = (await accounts.RegisterFailureAsync("sub-1", now, CancellationToken.None))!;
            current.IntentosFallidos.Should().Be(failure);
            current.EstaBloqueada(now).Should().BeFalse();
        }

        // Redis no interviene: el conteo es del documento. El quinto fallo bloquea hasta el final de la ventana.
        var fifth = (await accounts.RegisterFailureAsync("sub-1", now.AddSeconds(5), CancellationToken.None))!;

        fifth.IntentosFallidos.Should().Be(5);
        fifth.EstaBloqueada(now.AddSeconds(6)).Should().BeTrue();
        fifth.BloqueadoHasta.Should().BeCloseTo(fifth.VentanaFallosInicia!.Value.AddMinutes(15), TimeSpan.FromMilliseconds(5));
        (await accounts.FindBySubAsync("sub-1", CancellationToken.None))!.EstaBloqueada(now.AddSeconds(6)).Should().BeTrue("quedó guardado");
    }

    [Fact]
    public async Task Fallos_De_Dos_Hosts_A_La_Vez_Se_Suman_Con_Inc_Atomico_CA_033_08()
    {
        using var first = await IdentityApiFactory.CreateAsync();
        using var second = await IdentityApiFactory.CreateAsync(new Dictionary<string, string?> { ["Mongo:Databases:Identity"] = first.DatabaseName });
        var accountsA = first.Services.GetRequiredService<IPanelCuentasStore>();
        var accountsB = second.Services.GetRequiredService<IPanelCuentasStore>();
        await accountsA.InsertAsync(NewAccount("sub-1", [1]), CancellationToken.None);
        var now = DateTime.UtcNow;

        await Task.WhenAll(Enumerable.Range(0, 5).Select(index => (index % 2 == 0 ? accountsA : accountsB).RegisterFailureAsync("sub-1", now, CancellationToken.None)));

        var stored = (await accountsA.FindBySubAsync("sub-1", CancellationToken.None))!;
        stored.IntentosFallidos.Should().Be(5, "cinco fallos repartidos entre dos réplicas no se pierden");
        stored.EstaBloqueada(now.AddSeconds(1)).Should().BeTrue();
    }

    [Fact]
    public async Task Una_Ventana_Terminada_Abre_Otra_Con_El_Contador_En_Uno_R_11()
    {
        using var factory = await IdentityApiFactory.CreateAsync();
        var accounts = factory.Services.GetRequiredService<IPanelCuentasStore>();
        await accounts.InsertAsync(NewAccount("sub-1", [1]), CancellationToken.None);
        var now = DateTime.UtcNow;
        await accounts.RegisterFailureAsync("sub-1", now, CancellationToken.None);
        await accounts.RegisterFailureAsync("sub-1", now, CancellationToken.None);

        var later = (await accounts.RegisterFailureAsync("sub-1", now.AddMinutes(16), CancellationToken.None))!;

        later.IntentosFallidos.Should().Be(1);
        later.VentanaFallosInicia.Should().BeCloseTo(now.AddMinutes(16), TimeSpan.FromMilliseconds(5));
        await accounts.ResetFailuresAsync("sub-1", CancellationToken.None);
        (await accounts.FindBySubAsync("sub-1", CancellationToken.None))!.IntentosFallidos.Should().Be(0);
    }

    [Fact]
    public async Task Paso_Mayor_Solo_Lo_Acepta_Una_De_Diez_Peticiones_Simultaneas_CA_033_07()
    {
        using var factory = await IdentityApiFactory.CreateAsync();
        var accounts = factory.Services.GetRequiredService<IPanelCuentasStore>();
        await accounts.InsertAsync(NewAccount("sub-1", [1]), CancellationToken.None);

        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => accounts.TryAcceptStepAsync("sub-1", 100, CancellationToken.None)));

        results.Count(accepted => accepted).Should().Be(1);
        (await accounts.TryAcceptStepAsync("sub-1", 100, CancellationToken.None)).Should().BeFalse("el mismo paso ya no se acepta");
        (await accounts.TryAcceptStepAsync("sub-1", 99, CancellationToken.None)).Should().BeFalse("uno menor tampoco");
        (await accounts.TryAcceptStepAsync("sub-1", 101, CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task Maximo_Tres_Sesiones_La_Cuarta_Revoca_La_Mas_Antigua_CA_033_16()
    {
        using var factory = await IdentityApiFactory.CreateAsync();
        var accounts = factory.Services.GetRequiredService<IPanelCuentasStore>();
        var sessions = factory.Services.GetRequiredService<IPanelSesionesStore>();
        await accounts.InsertAsync(NewAccount("sub-1", [1]), CancellationToken.None);
        var start = DateTime.UtcNow;
        var created = new List<NuevaSesionPanel>();
        for (var index = 0; index < 3; index++)
        {
            var session = NewSession("sub-1", start.AddSeconds(index));
            created.Add(session);
            (await sessions.CreateAsync(session, CancellationToken.None)).Should().BeEmpty();
        }

        var revoked = await sessions.CreateAsync(NewSession("sub-1", start.AddSeconds(3)), CancellationToken.None);

        revoked.Should().Equal(created[0].Id);
        (await sessions.FindByIdAsync(created[0].Id, CancellationToken.None))!.Revocada.Should().BeTrue();
        var open = await factory.Database.GetCollection<BsonDocument>("panel_sesiones").CountDocumentsAsync(Builders<BsonDocument>.Filter.Eq("revocada", false));
        open.Should().Be(3);
    }

    [Fact]
    public async Task Rotacion_Condicionada_Gana_Una_Sola_Y_La_Huella_Anterior_Queda_Entre_Las_Usadas_CA_033_13()
    {
        using var factory = await IdentityApiFactory.CreateAsync();
        var sessions = factory.Services.GetRequiredService<IPanelSesionesStore>();
        var session = NewSession("sub-1");
        await sessions.CreateAsync(session, CancellationToken.None);
        var now = DateTime.UtcNow;

        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            sessions.RotateAsync(session.Id, session.HuellaRenovacion, Fingerprint(), now, now.AddMinutes(20), CancellationToken.None)));

        results.Count(rotated => rotated).Should().Be(1);
        var stored = (await sessions.FindByIdAsync(session.Id, CancellationToken.None))!;
        stored.HuellaRenovacion.Should().NotBe(session.HuellaRenovacion);
        stored.HuellasUsadas.Should().Equal(session.HuellaRenovacion);
        (await sessions.FindByUsedFingerprintAsync(session.HuellaRenovacion, CancellationToken.None))!.Id.Should().Be(session.Id);
    }

    [Fact]
    public async Task Solo_Se_Conservan_Las_Ultimas_Cinco_Huellas_Usadas_R_15()
    {
        using var factory = await IdentityApiFactory.CreateAsync();
        var sessions = factory.Services.GetRequiredService<IPanelSesionesStore>();
        var session = NewSession("sub-1");
        await sessions.CreateAsync(session, CancellationToken.None);
        var current = session.HuellaRenovacion;
        var history = new List<string>();
        for (var index = 0; index < 7; index++)
        {
            var next = Fingerprint();
            (await sessions.RotateAsync(session.Id, current, next, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(20), CancellationToken.None)).Should().BeTrue();
            history.Add(current);
            current = next;
        }

        (await sessions.FindByIdAsync(session.Id, CancellationToken.None))!.HuellasUsadas.Should().Equal(history.TakeLast(5));
    }

    [Fact]
    public async Task No_Se_Rota_Una_Sesion_Revocada_Vencida_O_Con_La_Cookie_Vencida_R_17()
    {
        using var factory = await IdentityApiFactory.CreateAsync();
        var sessions = factory.Services.GetRequiredService<IPanelSesionesStore>();
        var revoked = NewSession("sub-1");
        var expiredCookie = NewSession("sub-1", DateTime.UtcNow.AddMinutes(-30));
        await sessions.CreateAsync(revoked, CancellationToken.None);
        await sessions.CreateAsync(expiredCookie, CancellationToken.None);
        await sessions.RevokeAsync(revoked.Id, CancellationToken.None);

        (await sessions.RotateAsync(revoked.Id, revoked.HuellaRenovacion, Fingerprint(), DateTime.UtcNow, DateTime.UtcNow.AddMinutes(20), CancellationToken.None)).Should().BeFalse();
        (await sessions.RotateAsync(expiredCookie.Id, expiredCookie.HuellaRenovacion, Fingerprint(), DateTime.UtcNow, DateTime.UtcNow.AddMinutes(20), CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task Revocar_Todas_Devuelve_Los_Sid_Abiertos_De_La_Cuenta_Y_No_Toca_Otras_CA_033_26()
    {
        using var factory = await IdentityApiFactory.CreateAsync();
        var sessions = factory.Services.GetRequiredService<IPanelSesionesStore>();
        var mine = new[] { NewSession("sub-1"), NewSession("sub-1") };
        var other = NewSession("sub-2");
        foreach (var session in mine.Append(other))
        {
            await sessions.CreateAsync(session, CancellationToken.None);
        }

        var revoked = await sessions.RevokeAllAsync("sub-1", CancellationToken.None);

        revoked.Should().BeEquivalentTo(mine.Select(session => session.Id));
        (await sessions.FindByIdAsync(other.Id, CancellationToken.None))!.Revocada.Should().BeFalse();
    }

    [Fact]
    public async Task Enlace_Un_Solo_Uso_Consume_Una_Vez_Y_Configura_La_Cuenta_CA_033_27()
    {
        using var factory = await IdentityApiFactory.CreateAsync();
        var accounts = factory.Services.GetRequiredService<IPanelCuentasStore>();
        var invitations = factory.Services.GetRequiredService<IPanelActivacionesStore>();
        await accounts.InsertAsync(NewAccount("sub-1"), CancellationToken.None);
        var account = (await accounts.FindBySubAsync("sub-1", CancellationToken.None))!;
        var fingerprint = SHA256.HashData([1, 2, 3]);
        var now = DateTime.UtcNow;
        await invitations.CreateAsync(account.Id, fingerprint, now, now.AddMinutes(15), CancellationToken.None);
        var secret = new ProtectedSecretForStore([9, 9, 9, 9], 1);

        var live = await invitations.FindLiveAsync(fingerprint, now, CancellationToken.None);
        var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => invitations.ConsumeAsync(fingerprint, account.Id, secret, now, CancellationToken.None)));

        live!.Sub.Should().Be("sub-1");
        results.Count(result => result == ActivacionResultado.Consumida).Should().Be(1, "dos consumos simultáneos dejan una sola activación");
        var configured = (await accounts.FindBySubAsync("sub-1", CancellationToken.None))!;
        configured.TotpEstado.Should().Be("configurado");
        configured.PermisosVersion.Should().Be(2);
        (await invitations.FindLiveAsync(fingerprint, now, CancellationToken.None)).Should().BeNull("ya está consumida");
        (await invitations.ConsumeAsync(fingerprint, account.Id, new ProtectedSecretForStore([7], 1), now, CancellationToken.None)).Should().Be(ActivacionResultado.Invalida);
        (await accounts.FindBySubAsync("sub-1", CancellationToken.None))!.TotpSecretoCifrado.Should().Equal(secret.Ciphertext, "un segundo consumo no cambia el secreto");
    }

    [Fact]
    public async Task Enlace_Vencido_O_De_Cuenta_Ya_Configurada_No_Se_Consume_CA_033_27()
    {
        using var factory = await IdentityApiFactory.CreateAsync();
        var accounts = factory.Services.GetRequiredService<IPanelCuentasStore>();
        var invitations = factory.Services.GetRequiredService<IPanelActivacionesStore>();
        await accounts.InsertAsync(NewAccount("sub-1"), CancellationToken.None);
        await accounts.InsertAsync(NewAccount("sub-2", [5, 5, 5]), CancellationToken.None);
        var pending = (await accounts.FindBySubAsync("sub-1", CancellationToken.None))!;
        var configured = (await accounts.FindBySubAsync("sub-2", CancellationToken.None))!;
        var expiredPrint = SHA256.HashData([1]);
        var configuredPrint = SHA256.HashData([2]);
        var now = DateTime.UtcNow;
        await invitations.CreateAsync(pending.Id, expiredPrint, now.AddMinutes(-20), now.AddMinutes(-5), CancellationToken.None);
        await invitations.CreateAsync(configured.Id, configuredPrint, now, now.AddMinutes(15), CancellationToken.None);

        (await invitations.FindLiveAsync(expiredPrint, now, CancellationToken.None)).Should().BeNull("venció a los 15 minutos");
        (await invitations.FindLiveAsync(configuredPrint, now, CancellationToken.None)).Should().BeNull("la cuenta ya no está pendiente");
        (await invitations.ConsumeAsync(configuredPrint, configured.Id, new ProtectedSecretForStore([1], 1), now, CancellationToken.None)).Should().Be(ActivacionResultado.Invalida);
        (await accounts.FindBySubAsync("sub-2", CancellationToken.None))!.TotpSecretoCifrado.Should().Equal((byte)5, (byte)5, (byte)5);
    }

    [Fact]
    public async Task Una_Invitacion_Nueva_Reemplaza_A_La_Anterior_De_La_Cuenta_R_29()
    {
        using var factory = await IdentityApiFactory.CreateAsync();
        var accounts = factory.Services.GetRequiredService<IPanelCuentasStore>();
        var invitations = factory.Services.GetRequiredService<IPanelActivacionesStore>();
        await accounts.InsertAsync(NewAccount("sub-1"), CancellationToken.None);
        var account = (await accounts.FindBySubAsync("sub-1", CancellationToken.None))!;
        var first = SHA256.HashData([1]);
        var second = SHA256.HashData([2]);
        var now = DateTime.UtcNow;

        await invitations.CreateAsync(account.Id, first, now, now.AddMinutes(15), CancellationToken.None);
        await invitations.CreateAsync(account.Id, second, now, now.AddMinutes(15), CancellationToken.None);

        (await invitations.FindLiveAsync(first, now, CancellationToken.None)).Should().BeNull();
        (await invitations.FindLiveAsync(second, now, CancellationToken.None)).Should().NotBeNull();
    }

    [Fact]
    public async Task Secretos_Solo_Cifrados_Huellas_Y_Sin_Celular_En_Claro_CA_033_17()
    {
        using var factory = await IdentityApiFactory.CreateAsync();
        var accounts = factory.Services.GetRequiredService<IPanelCuentasStore>();
        var sessions = factory.Services.GetRequiredService<IPanelSesionesStore>();
        var plainSecret = TotpSecretForTests();
        await accounts.InsertAsync(NewAccount("sub-1", [0xAA, 0xBB, 0xCC, 0xDD]), CancellationToken.None);
        var session = NewSession("sub-1");
        await sessions.CreateAsync(session, CancellationToken.None);
        const string refreshToken = "token-de-renovacion-en-claro";

        var accountJson = (await factory.Database.GetCollection<BsonDocument>("panel_cuentas").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync()).ToJson();
        var sessionJson = (await factory.Database.GetCollection<BsonDocument>("panel_sesiones").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync()).ToJson();

        accountJson.Should().Contain("totpSecretoCifrado").And.NotContain(plainSecret).And.NotContain("celular");
        sessionJson.Should().Contain(session.HuellaRenovacion).And.NotContain(refreshToken).And.NotContain("celular");
        var accountDocument = BsonDocument.Parse(accountJson);
        accountDocument.Names.Should().NotContain("celular").And.NotContain("celularHash").And.NotContain("celularCifrado");
    }

    private static string TotpSecretForTests() => "JBSWY3DPEHPK3PXP";

    [Fact]
    public async Task Los_Indices_De_Las_Cuatro_Colecciones_Quedan_Creados_Con_Su_Ttl_R_03_R_15_R_23()
    {
        using var factory = await IdentityApiFactory.CreateAsync();

        async Task<List<string>> Names(string collection) =>
            [.. (await (await factory.Database.GetCollection<BsonDocument>(collection).Indexes.ListAsync()).ToListAsync()).Select(index => index["name"].AsString).Order(StringComparer.Ordinal)];

        (await Names("panel_cuentas")).Should().Equal("_id_", "sub_1");
        (await Names("panel_sesiones")).Should().Equal("_id_", "huellaRenovacion_1", "huellasUsadas_1", "sub_1_revocada_1_creadaEn_1", "venceEn_1");
        (await Names("panel_activaciones")).Should().Equal("_id_", "creadaEn_1", "cuentaId_1", "huella_1");
        (await Names("panel_auditoria")).Should().Equal("_id_", "evento_1_fecha_-1", "fecha_1", "sub_1_fecha_-1");
        var audit = await (await factory.Database.GetCollection<BsonDocument>("panel_auditoria").Indexes.ListAsync()).ToListAsync();
        audit.Single(index => index["name"] == "fecha_1")["expireAfterSeconds"].ToInt64().Should().Be(365L * 24 * 3600);
    }

    [Fact]
    public async Task Auditoria_Guarda_Solo_Los_Campos_Permitidos_R_23()
    {
        using var factory = await IdentityApiFactory.CreateAsync();
        var audit = factory.Services.GetRequiredService<IPanelAuditoriaStore>();

        await audit.InsertAsync(new PanelAuditEntry(DateTime.UtcNow, "acceso", "acceso_concedido", "permitido", "hmac", "corr-1", Sub: "sub-1", Sid: "sid-1"), CancellationToken.None);

        var stored = await factory.Database.GetCollection<BsonDocument>("panel_auditoria").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        stored.Names.Should().BeEquivalentTo("_id", "fecha", "tipo", "evento", "resultado", "ipHmac", "correlationId", "sub", "sid");
    }

    [Fact]
    public async Task Desafio_Se_Consume_Una_Sola_Vez_Aunque_Lleguen_Diez_A_La_Vez_CA_033_07()
    {
        using var factory = await IdentityApiFactory.CreateAsync();
        var challenges = factory.Services.GetRequiredService<IPanelDesafios>();
        var id = Guid.NewGuid().ToString("N");
        await challenges.CreateAsync(id, "sub-1", TimeSpan.FromMinutes(5), CancellationToken.None);

        (await challenges.PeekAsync(id, CancellationToken.None)).Should().Be("sub-1", "leer no lo gasta");
        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => challenges.ConsumeAsync(id, CancellationToken.None)));

        results.Count(sub => sub == "sub-1").Should().Be(1);
        (await challenges.PeekAsync(id, CancellationToken.None)).Should().BeNull("el desafío ya no existe en Redis");
    }

    [Fact]
    public async Task Desafio_Vence_En_Cinco_Minutos_Con_Su_Ttl_En_Redis_CA_033_09()
    {
        using var factory = await IdentityApiFactory.CreateAsync();
        var challenges = factory.Services.GetRequiredService<IPanelDesafios>();
        var id = Guid.NewGuid().ToString("N");

        await challenges.CreateAsync(id, "sub-1", TimeSpan.FromMinutes(5), CancellationToken.None);

        var ttl = await factory.Redis.KeyTimeToLiveAsync(RedisPanelDesafios.ChallengeKey(id));
        ttl.Should().NotBeNull().And.BeLessThanOrEqualTo(TimeSpan.FromMinutes(5));
        (await challenges.PeekAsync("no-existe", CancellationToken.None)).Should().BeNull();
        (await challenges.ConsumeAsync("no-existe", CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Fallos_Por_Ip_Se_Cuentan_Con_Vencimiento_En_La_Misma_Operacion_CA_033_08()
    {
        using var factory = await IdentityApiFactory.CreateAsync();
        var challenges = factory.Services.GetRequiredService<IPanelDesafios>();
        var ip = "203.0.113." + RandomNumberGenerator.GetInt32(1, 250);

        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => challenges.RegisterIpFailureAsync(ip, TimeSpan.FromMinutes(15), CancellationToken.None)));

        (await challenges.IpFailuresAsync(ip, CancellationToken.None)).Should().Be(10);
        (await factory.Redis.KeyTimeToLiveAsync(RedisPanelDesafios.IpKey(ip))).Should().NotBeNull();
    }

    [Fact]
    public async Task Cache_De_La_Cuenta_Guarda_Solo_Lo_Necesario_Vence_A_Los_60_Segundos_Y_Se_Borra_R_24()
    {
        using var factory = await IdentityApiFactory.CreateAsync();
        var cache = factory.Services.GetRequiredService<IPanelCuentaCache>();

        await cache.SetAsync("sub-1", new CuentaPanelEnCache(["verificador", "moderador"], "activa", 3, "Ana"), CancellationToken.None);

        var hit = (await cache.GetAsync("sub-1", CancellationToken.None))!;
        hit.Roles.Should().Equal("verificador", "moderador");
        hit.PermisosVersion.Should().Be(3);
        (await factory.Redis.KeyTimeToLiveAsync(RedisPanelCuentaCache.Key("sub-1")))!.Value.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(60));
        await cache.DeleteAsync("sub-1", CancellationToken.None);
        (await cache.GetAsync("sub-1", CancellationToken.None)).Should().BeNull();
    }
}
