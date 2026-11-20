using System.Text.Json;
using AwesomeAssertions;
using Knock.Identity.Api.Common.Errors;
using Knock.Identity.Api.Modules.Panel.Services;
using Knock.Identity.Api.Modules.Sesiones.Domain;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

namespace Knock.UnitTest.Panel;

/// <summary>Spec 033, T-06: acceso en dos pasos (R-04 a R-12, R-24 y R-28), activación del TOTP (R-29) y su auditoría, con dobles de MongoDB, Redis y Firebase.</summary>
public class Spec033AccesoServicioTests
{
    private const string _ip = "203.0.113.7";

    private static readonly byte[] _secret = System.Text.Encoding.ASCII.GetBytes("12345678901234567890");

    private static async Task<string> ChallengeAsync(PanelWorld world, string uid = PanelWorld.Uid)
    {
        var outcome = await world.Access.VerifyPhoneAsync(world.TokenFor(uid), _ip, CancellationToken.None);
        return outcome.Should().BeOfType<PhoneOutcome.Challenge>().Subject.ChallengeId;
    }

    private static async Task<PanelGrant> LoginAsync(PanelWorld world, string uid = PanelWorld.Uid)
    {
        var challenge = await ChallengeAsync(world, uid);
        var outcome = await world.Access.VerifyTotpAsync(challenge, world.CurrentCode(_secret), _ip, CancellationToken.None);
        return outcome.Should().BeOfType<TotpOutcome.Granted>().Subject.Grant;
    }

    [Fact]
    public async Task Acceso_Con_Totp_Entrega_Token_Del_Panel_Con_Los_Cinco_Permisos_Y_Audita_Dos_Pasos_CA_033_01()
    {
        var world = new PanelWorld();
        world.SeedAccount(secret: _secret);

        var grant = await LoginAsync(world);

        grant.Permisos.Should().Equal("verification:review", "reports:resolve", "reviews:moderate", "catalog:manage", "metrics:read");
        grant.Nombre.Should().Be("Ana de prueba");
        grant.ExpiraEn.Should().Be(world.Clock.GetUtcNow().UtcDateTime.AddMinutes(15));
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(grant.AccessToken);
        jwt.Audiences.Should().Equal("knock-panel");
        jwt.Issuer.Should().Be("knock-api");
        jwt.GetClaim("sub").Value.Should().Be(PanelWorld.AppSub);
        jwt.GetClaim("pv").Value.Should().Be("1");
        jwt.GetClaim("sid").Value.Should().Be(world.Sessions.Sessions.Single().Id);
        jwt.Claims.Select(claim => claim.Type).Should().NotContain(["permisos", "permissions", "roles", "role"]);
        world.Sessions.Sessions.Should().ContainSingle();
        world.AuditStore.Entries.Select(entry => entry.Evento).Should().Equal("celular_verificado", "acceso_concedido");
    }

    [Fact]
    public async Task El_Token_De_Renovacion_Solo_Se_Guarda_Como_Huella_R_15()
    {
        var world = new PanelWorld();
        world.SeedAccount(secret: _secret);

        var grant = await LoginAsync(world);

        var session = world.Sessions.Sessions.Single();
        session.HuellaRenovacion.Should().Be(PanelRefreshToken.Fingerprint(grant.RefreshToken)).And.NotContain(grant.RefreshToken);
        grant.RefreshToken.Should().HaveLength(43);
        session.VenceEn.Should().Be(world.Clock.GetUtcNow().UtcDateTime.AddHours(8));
        session.RenovacionVenceEn.Should().Be(world.Clock.GetUtcNow().UtcDateTime.AddMinutes(20));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-2)]
    [InlineData(0)]
    public async Task Un_Codigo_Incorrecto_O_Fuera_De_La_Ventana_Es_Invalid_Code_Sin_Sesion_CA_033_02(int wrongOffset)
    {
        var world = new PanelWorld();
        world.SeedAccount(secret: _secret);
        var challenge = await ChallengeAsync(world);
        var code = wrongOffset == 0 ? "000000" : world.CurrentCode(_secret, wrongOffset);
        if (wrongOffset == 0 && code == world.CurrentCode(_secret))
        {
            code = "000001";
        }

        var outcome = await world.Access.VerifyTotpAsync(challenge, code == world.CurrentCode(_secret, -1) ? "999999" : code, _ip, CancellationToken.None);

        outcome.Should().BeOfType<TotpOutcome.InvalidCode>();
        world.Sessions.Sessions.Should().BeEmpty();
    }

    [Fact]
    public async Task Un_Codigo_Incorrecto_No_Gasta_El_Desafio_Y_Se_Cuenta_Por_Cuenta_R_11()
    {
        var world = new PanelWorld();
        world.SeedAccount(secret: _secret);
        var challenge = await ChallengeAsync(world);

        await world.Access.VerifyTotpAsync(challenge, "000000" == world.CurrentCode(_secret) ? "000001" : "000000", _ip, CancellationToken.None);
        var outcome = await world.Access.VerifyTotpAsync(challenge, world.CurrentCode(_secret), _ip, CancellationToken.None);

        outcome.Should().BeOfType<TotpOutcome.Granted>();
        world.Accounts.Accounts[PanelWorld.AppSub].IntentosFallidos.Should().Be(0, "un acceso exitoso reinicia el contador de la cuenta");
    }

    [Fact]
    public async Task Sin_Cuenta_Del_Panel_O_Deshabilitada_Es_403_Igual_Y_No_Se_Crea_Desafio_CA_033_03()
    {
        var world = new PanelWorld();
        var withoutAccount = await world.Access.VerifyPhoneAsync(world.TokenFor(), _ip, CancellationToken.None);
        world.SeedAccount(secret: _secret);
        await world.Admin.DisableAsync(PanelWorld.Phone, CancellationToken.None);

        var disabled = await world.Access.VerifyPhoneAsync(world.TokenFor(), _ip, CancellationToken.None);

        withoutAccount.Should().Be(new PhoneOutcome.NoPanelAccess());
        disabled.Should().Be(new PhoneOutcome.NoPanelAccess());
        world.Challenges.Created.Should().Be(0);
    }

    [Fact]
    public async Task Una_Cuenta_Sin_Totp_Configurado_Es_403_R_06()
    {
        var world = new PanelWorld();
        await world.Admin.AltaAsync(new PanelAltaRequest(PanelWorld.Phone, "Ana", [PanelRolesAdmin], null, false), CancellationToken.None);

        (await world.Access.VerifyPhoneAsync(world.TokenFor(), _ip, CancellationToken.None)).Should().BeOfType<PhoneOutcome.NoPanelAccess>();
        world.Challenges.Created.Should().Be(0);
    }

    private const string PanelRolesAdmin = "administrador";

    [Fact]
    public async Task Una_Cuenta_Suspendida_En_La_App_Es_403_No_Panel_Access_CA_033_25()
    {
        var world = new PanelWorld();
        world.SeedAccount(secret: _secret);
        var user = world.AppAccounts.Users.Single();
        world.AppAccounts.Users[0] = user with { State = AccountState.Suspended };

        var outcome = await world.Access.VerifyPhoneAsync(world.TokenFor(), _ip, CancellationToken.None);

        outcome.Should().BeOfType<PhoneOutcome.NoPanelAccess>();
        world.Challenges.Created.Should().Be(0);
    }

    [Fact]
    public async Task Un_IdToken_Invalido_Es_401_Invalid_Credentials_Sin_Llamar_A_La_Cuenta_R_05()
    {
        var world = new PanelWorld();
        world.SeedAccount(secret: _secret);

        var outcome = await world.Access.VerifyPhoneAsync("no-registrado", _ip, CancellationToken.None);

        outcome.Should().BeOfType<PhoneOutcome.InvalidCredentials>();
        world.Accounts.Reads.Should().Be(0);
        world.AuditStore.Entries.Single().Resultado.Should().Be("fallido");
    }

    [Fact]
    public async Task Un_Codigo_Ya_Aceptado_Se_Rechaza_Aunque_Venga_Con_Un_Desafio_Nuevo_CA_033_06()
    {
        var world = new PanelWorld();
        world.SeedAccount(secret: _secret);
        await LoginAsync(world);
        var challenge = await ChallengeAsync(world);

        var repeated = await world.Access.VerifyTotpAsync(challenge, world.CurrentCode(_secret), _ip, CancellationToken.None);

        repeated.Should().BeOfType<TotpOutcome.InvalidCode>();
        world.Sessions.Sessions.Should().ContainSingle();
    }

    [Fact]
    public async Task Dos_Peticiones_Con_El_Mismo_Desafio_Y_Codigo_Dan_Un_200_Y_Una_Sola_Sesion_CA_033_07()
    {
        var world = new PanelWorld();
        world.SeedAccount(secret: _secret);
        var challenge = await ChallengeAsync(world);
        var code = world.CurrentCode(_secret);

        var outcomes = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => world.Access.VerifyTotpAsync(challenge, code, _ip, CancellationToken.None)));

        outcomes.OfType<TotpOutcome.Granted>().Should().ContainSingle();
        outcomes.Where(outcome => outcome is not TotpOutcome.Granted).Should().ContainSingle();
        world.Sessions.Sessions.Should().ContainSingle();
    }

    [Fact]
    public async Task Cinco_Fallos_Bloquean_La_Cuenta_Y_El_Sexto_Responde_Locked_Aunque_Sea_Correcto_CA_033_08()
    {
        var world = new PanelWorld();
        world.SeedAccount(secret: _secret);
        var wrong = world.CurrentCode(_secret) == "000000" ? "000001" : "000000";
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var challenge = await ChallengeAsync(world);
            (await world.Access.VerifyTotpAsync(challenge, wrong, _ip, CancellationToken.None)).Should().BeOfType<TotpOutcome.InvalidCode>();
        }

        var outcome = await world.Access.VerifyPhoneAsync(world.TokenFor(), _ip, CancellationToken.None);

        outcome.Should().BeOfType<PhoneOutcome.Locked>().Which.RetryAfter.Should().BeGreaterThan(TimeSpan.Zero).And.BeLessThanOrEqualTo(TimeSpan.FromMinutes(15));
        world.AuditStore.Entries.Select(entry => entry.Evento).Should().Contain("bloqueo");
    }

    [Fact]
    public async Task Con_La_Cuenta_Bloqueada_El_Segundo_Paso_Tambien_Responde_Locked_Con_El_Codigo_Correcto_CA_033_08()
    {
        var world = new PanelWorld();
        world.SeedAccount(secret: _secret);
        var challenge = await ChallengeAsync(world);
        var wrong = world.CurrentCode(_secret) == "000000" ? "000001" : "000000";
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await world.Access.VerifyTotpAsync(challenge, wrong, _ip, CancellationToken.None);
        }

        var outcome = await world.Access.VerifyTotpAsync(challenge, world.CurrentCode(_secret), _ip, CancellationToken.None);

        outcome.Should().BeOfType<TotpOutcome.Locked>();
        world.Sessions.Sessions.Should().BeEmpty();
    }

    [Fact]
    public async Task Despues_Del_Bloqueo_Un_Desafio_Nuevo_No_Reinicia_El_Conteo_Y_Al_Terminar_La_Ventana_Se_Puede_Entrar_CA_033_28()
    {
        var world = new PanelWorld();
        world.SeedAccount(secret: _secret);
        var challenge = await ChallengeAsync(world);
        var wrong = world.CurrentCode(_secret) == "000000" ? "000001" : "000000";
        for (var attempt = 0; attempt < 4; attempt++)
        {
            await world.Access.VerifyTotpAsync(challenge, wrong, _ip, CancellationToken.None);
        }

        var secondChallenge = await ChallengeAsync(world);
        await world.Access.VerifyTotpAsync(secondChallenge, wrong, _ip, CancellationToken.None);

        world.Accounts.Accounts[PanelWorld.AppSub].IntentosFallidos.Should().Be(5, "el conteo es de la cuenta, no del desafío");
        world.Clock.Advance(TimeSpan.FromMinutes(15));
        var outcome = await world.Access.VerifyPhoneAsync(world.TokenFor(), _ip, CancellationToken.None);
        outcome.Should().BeOfType<PhoneOutcome.Challenge>("la ventana terminó");
    }

    [Fact]
    public async Task Diez_Fallos_De_Una_Ip_Con_Cuentas_Distintas_Dan_429_Limit_Exceeded_Al_Undecimo_CA_033_08()
    {
        var world = new PanelWorld();
        world.SeedAccount(secret: _secret);
        world.Challenges.SetIpFailures(_ip, 10);

        var phone = await world.Access.VerifyPhoneAsync(world.TokenFor(), _ip, CancellationToken.None);
        var totp = await world.Access.VerifyTotpAsync("AAAAAAAAAAAAAAAAAAAAAA", "123456", _ip, CancellationToken.None);

        phone.Should().BeOfType<PhoneOutcome.LimitExceeded>();
        totp.Should().BeOfType<TotpOutcome.LimitExceeded>();
        (await world.Access.VerifyPhoneAsync(world.TokenFor(), "203.0.113.8", CancellationToken.None)).Should().BeOfType<PhoneOutcome.Challenge>("otra IP no se ve afectada");
    }

    [Fact]
    public async Task Cada_Fallo_De_Totp_Suma_Uno_Al_Contador_De_La_Ip_R_11()
    {
        var world = new PanelWorld();
        world.SeedAccount(secret: _secret);
        var challenge = await ChallengeAsync(world);

        await world.Access.VerifyTotpAsync(challenge, world.CurrentCode(_secret) == "000000" ? "000001" : "000000", _ip, CancellationToken.None);

        (await world.Challenges.IpFailuresAsync(_ip, CancellationToken.None)).Should().Be(1);
    }

    [Fact]
    public async Task Un_Desafio_Vencido_O_Ya_Usado_Es_Challenge_Expired_Y_Ya_No_Existe_CA_033_09()
    {
        var world = new PanelWorld();
        world.SeedAccount(secret: _secret);
        var expired = await ChallengeAsync(world);
        world.Challenges.Expire(expired);
        var used = await ChallengeAsync(world);
        await world.Access.VerifyTotpAsync(used, world.CurrentCode(_secret), _ip, CancellationToken.None);

        (await world.Access.VerifyTotpAsync(expired, world.CurrentCode(_secret, 1), _ip, CancellationToken.None)).Should().BeOfType<TotpOutcome.ChallengeExpired>();
        (await world.Access.VerifyTotpAsync(used, world.CurrentCode(_secret, 1), _ip, CancellationToken.None)).Should().BeOfType<TotpOutcome.ChallengeExpired>();
        (await world.Challenges.PeekAsync(used, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Un_Cambio_De_Roles_Entre_Los_Dos_Pasos_Se_Refleja_En_La_Sesion_Emitida_Seccion_11()
    {
        var world = new PanelWorld();
        world.SeedAccount(secret: _secret);
        var challenge = await ChallengeAsync(world);
        await world.Admin.SetRolesAsync(PanelWorld.Phone, ["moderador"], CancellationToken.None);

        var grant = ((TotpOutcome.Granted)await world.Access.VerifyTotpAsync(challenge, world.CurrentCode(_secret), _ip, CancellationToken.None)).Grant;

        grant.Permisos.Should().Equal("reports:resolve", "reviews:moderate");
        new JsonWebTokenHandler().ReadJsonWebToken(grant.AccessToken).GetClaim("pv").Value.Should().Be("2");
    }

    [Fact]
    public async Task Una_Cuenta_Deshabilitada_Entre_Los_Pasos_Es_403_Y_Gasta_El_Desafio_R_06()
    {
        var world = new PanelWorld();
        world.SeedAccount(secret: _secret);
        var challenge = await ChallengeAsync(world);
        await world.Admin.DisableAsync(PanelWorld.Phone, CancellationToken.None);

        var outcome = await world.Access.VerifyTotpAsync(challenge, world.CurrentCode(_secret), _ip, CancellationToken.None);

        outcome.Should().BeOfType<TotpOutcome.NoPanelAccess>();
        (await world.Challenges.PeekAsync(challenge, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task La_Cuarta_Sesion_Revoca_La_Mas_Antigua_Y_Cancela_Su_Sid_CA_033_16()
    {
        var world = new PanelWorld();
        world.SeedAccount(secret: _secret);
        for (var index = 0; index < 4; index++)
        {
            world.Clock.Advance(TimeSpan.FromSeconds(31));
            await LoginAsync(world);
        }

        world.Sessions.Sessions.Count(session => !session.Revocada).Should().Be(3);
        world.Sessions.Sessions.OrderBy(session => session.CreadaEn).First().Revocada.Should().BeTrue();
        world.Cancelled.Entries.Should().ContainSingle().Which.SessionId.Should().Be(world.Sessions.Sessions.OrderBy(session => session.CreadaEn).First().Id);
    }

    [Theory]
    [InlineData("firebase")]
    [InlineData("mongo")]
    [InlineData("redis")]
    public async Task Con_Una_Dependencia_Caida_Responde_503_Unavailable_Sin_Conceder_Acceso_CA_033_24(string failing)
    {
        var world = new PanelWorld();
        world.SeedAccount(secret: _secret);
        var challenge = await ChallengeAsync(world);
        var token = world.TokenFor();
        world.Verifier.Unavailable = failing == "firebase";
        world.Accounts.Unavailable = failing == "mongo";
        world.Challenges.Unavailable = failing == "redis";

        var phone = await FluentActions.Awaiting(() => world.Access.VerifyPhoneAsync(token, _ip, CancellationToken.None)).Should().ThrowAsync<DomainException>();
        phone.Which.Code.Should().Be("unavailable");
        phone.Which.Kind.Should().Be(DomainErrorKind.ServiceUnavailable);
        phone.Which.Message.Should().NotContain("clave").And.NotContain("googleapis");
        if (failing != "firebase")
        {
            // El segundo paso no usa Firebase: solo MongoDB y Redis lo pueden dejar sin servicio.
            var totp = await FluentActions.Awaiting(() => world.Access.VerifyTotpAsync(challenge, world.CurrentCode(_secret), _ip, CancellationToken.None)).Should().ThrowAsync<DomainException>();
            totp.Which.Code.Should().Be("unavailable");
        }

        world.Sessions.Sessions.Should().BeEmpty();
    }

    [Fact]
    public async Task Sin_Poder_Escribir_La_Auditoria_No_Hay_Desafio_Ni_Sesion_CA_033_18()
    {
        var world = new PanelWorld();
        world.SeedAccount(secret: _secret);
        world.AuditStore.Fails = true;

        var phone = await FluentActions.Awaiting(() => world.Access.VerifyPhoneAsync(world.TokenFor(), _ip, CancellationToken.None)).Should().ThrowAsync<DomainException>();

        phone.Which.Code.Should().Be("audit_unavailable");
        world.Challenges.Created.Should().Be(0);
        world.AuditStore.Fails = false;
        var challenge = await ChallengeAsync(world);
        world.AuditStore.Fails = true;
        var totp = await FluentActions.Awaiting(() => world.Access.VerifyTotpAsync(challenge, world.CurrentCode(_secret), _ip, CancellationToken.None)).Should().ThrowAsync<DomainException>();
        totp.Which.Code.Should().Be("audit_unavailable");
        world.Sessions.Sessions.Should().BeEmpty("el acceso no se concede");
    }

    [Fact]
    public async Task Con_El_Panel_Sin_Llave_Configurada_Los_Pasos_Responden_503_R_28()
    {
        var world = new PanelWorld();
        world.SeedAccount(secret: _secret);
        var off = new PanelSettings("knock-api", "knock-panel", world.Settings.SigningKey, PanelWorld.Origin, false);
        var access = new AccesoPanelService(world.Verifier, world.Accounts, world.AppApi, world.Challenges, world.Totp, world.SessionService, world.Audit, off, world.Clock);

        var thrown = await FluentActions.Awaiting(() => access.VerifyPhoneAsync(world.TokenFor(), _ip, CancellationToken.None)).Should().ThrowAsync<DomainException>();

        thrown.Which.Code.Should().Be("unavailable");
    }

    [Fact]
    public async Task Acceso_Completo_Fallido_Y_Alta_No_Dejan_Datos_Sensibles_En_La_Auditoria_CA_033_19()
    {
        var world = new PanelWorld();
        const string testSecret = "JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP";
        await world.Admin.AltaAsync(new PanelAltaRequest(PanelWorld.Phone, "Ana", [PanelRolesAdmin], testSecret, true), CancellationToken.None);
        var account = world.Accounts.Accounts.Values.Single();
        var plain = OtpNet.Base32Encoding.ToBytes(testSecret);
        var idToken = world.TokenFor();
        var challenge = await ChallengeAsync(world);
        await world.Access.VerifyTotpAsync(challenge, world.CurrentCode(plain) == "000000" ? "000001" : "000000", _ip, CancellationToken.None);
        var code = world.CurrentCode(plain);
        var grant = ((TotpOutcome.Granted)await world.Access.VerifyTotpAsync(challenge, code, _ip, CancellationToken.None)).Grant;

        var everything = JsonSerializer.Serialize(world.AuditStore.Entries);

        foreach (var forbidden in new[] { PanelWorld.Phone, idToken, code, challenge, grant.AccessToken, grant.RefreshToken, testSecret, "otpauth", account.Id })
        {
            everything.Should().NotContain(forbidden);
        }
    }

    [Fact]
    public async Task Enlace_Un_Solo_Uso_Muestra_El_Secreto_Y_Configura_La_Cuenta_CA_033_27()
    {
        var world = new PanelWorld("Production");
        var token = await ActivationTokenAsync(world);

        var outcome = await world.Activation.ActivateAsync(token, CancellationToken.None);

        var activated = outcome.Should().BeOfType<ActivationOutcome.Activated>().Subject;
        activated.OtpauthUri.Should().StartWith("otpauth://totp/").And.Contain("secret=");
        activated.QrSvg.Should().StartWith("<svg").And.Contain("</svg>");
        var account = world.Accounts.Accounts[PanelWorld.AppSub];
        account.TotpEstado.Should().Be("configurado");
        account.TotpSecretoCifrado.Should().NotBeEmpty();
        world.AuditStore.Entries.Select(entry => entry.Evento).Should().Contain("totp_activado");
        JsonSerializer.Serialize(world.AuditStore.Entries).Should().NotContain(token).And.NotContain("secret=");
        var secretFromLink = SecretOf(activated.OtpauthUri);
        var login = await ChallengeAndCodeAsync(world, secretFromLink);
        login.Should().BeOfType<TotpOutcome.Granted>("el secreto del enlace sirve para entrar");
    }

    [Fact]
    public async Task Enlace_Ya_Consumido_Vencido_O_De_Cuenta_Configurada_Es_410_Y_No_Cambia_El_Secreto_CA_033_27()
    {
        var world = new PanelWorld("Production");
        var token = await ActivationTokenAsync(world);
        await world.Activation.ActivateAsync(token, CancellationToken.None);
        var secretAfterFirst = world.Accounts.Accounts[PanelWorld.AppSub].TotpSecretoCifrado;

        var again = await world.Activation.ActivateAsync(token, CancellationToken.None);

        again.Should().BeOfType<ActivationOutcome.Invalid>();
        world.Accounts.Accounts[PanelWorld.AppSub].TotpSecretoCifrado.Should().Equal(secretAfterFirst);

        var expiredWorld = new PanelWorld("Production");
        var expiredToken = await ActivationTokenAsync(expiredWorld);
        expiredWorld.Clock.Advance(TimeSpan.FromMinutes(16));
        (await expiredWorld.Activation.ActivateAsync(expiredToken, CancellationToken.None)).Should().BeOfType<ActivationOutcome.Invalid>();
        (await world.Activation.ActivateAsync("desconocido", CancellationToken.None)).Should().BeOfType<ActivationOutcome.Invalid>();
    }

    [Fact]
    public async Task Dos_Consumos_Simultaneos_Dejan_Una_Sola_Activacion_CA_033_27()
    {
        var world = new PanelWorld("Production");
        var token = await ActivationTokenAsync(world);

        var outcomes = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => world.Activation.ActivateAsync(token, CancellationToken.None)));

        outcomes.OfType<ActivationOutcome.Activated>().Should().ContainSingle();
    }

    private static async Task<string> ActivationTokenAsync(PanelWorld world)
    {
        var console = new FakePanelConsole();
        var exit = await world.Cli(console).RunAsync(["alta", "--celular", PanelWorld.Phone, "--nombre", "Ana", "--roles", "administrador"], CancellationToken.None);
        exit.Should().Be(0, console.Errors);
        return console.Output.Split("#token=")[1].Trim();
    }

    private static byte[] SecretOf(string otpauthUri)
    {
        var secret = otpauthUri.Split("secret=")[1].Split('&')[0];
        return OtpNet.Base32Encoding.ToBytes(secret);
    }

    private static async Task<TotpOutcome> ChallengeAndCodeAsync(PanelWorld world, byte[] secret)
    {
        var challenge = await ChallengeAsync(world);
        return await world.Access.VerifyTotpAsync(challenge, world.CurrentCode(secret), _ip, CancellationToken.None);
    }
}
