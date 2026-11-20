using AwesomeAssertions;
using Knock.Identity.Api.Modules.Panel.Cli;
using Knock.Identity.Api.Modules.Panel.Domain;
using Knock.Identity.Api.Modules.Panel.Services;
using Xunit;

namespace Knock.UnitTest.Panel;

/// <summary>Spec 033, T-04: herramienta interna del panel (R-01, R-24, R-25 y R-26; CA-21 y la parte de CA-05 que ejecuta la herramienta).</summary>
public class Spec033HerramientaTests
{
    private const string _testSecret = "JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP";

    private static string[] Alta(string phone = PanelWorld.Phone, string name = "Ana de prueba", string roles = "administrador", string? testSecret = null) =>
        testSecret is null
            ? ["alta", "--celular", phone, "--nombre", name, "--roles", roles]
            : ["alta", "--celular", phone, "--nombre", name, "--roles", roles, "--secreto-de-prueba", testSecret];

    private static void NothingCreated(PanelWorld world)
    {
        world.Accounts.Accounts.Should().BeEmpty("falla sin crear nada");
        world.Invitations.Invitations.Should().BeEmpty();
        world.AuditStore.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Alta_Valida_Crea_La_Cuenta_Pendiente_E_Imprime_Solo_El_Enlace_De_Activacion_CA_033_21()
    {
        var world = new PanelWorld("Production");
        var console = new FakePanelConsole();

        var exit = await world.Cli(console).RunAsync(Alta(), CancellationToken.None);

        exit.Should().Be(0, console.Errors);
        var account = world.Accounts.Accounts.Values.Single();
        account.Sub.Should().Be(PanelWorld.AppSub);
        account.TotpEstado.Should().Be("pendiente");
        account.TotpSecretoCifrado.Should().BeNull();
        account.Roles.Should().Equal("administrador");
        account.PermisosVersion.Should().Be(1);
        console.Output.Should().Contain($"{PanelWorld.Origin}/activacion#token=");
        console.Output.Should().NotContainEquivalentOf("otpauth").And.NotContainEquivalentOf("secret").And.NotContain(PanelWorld.Phone);
        var token = console.Output.Split("#token=")[1].Trim();
        token.Should().HaveLength(43);
        var invitation = world.Invitations.Invitations.Single();
        invitation.Fingerprint.Should().Equal(ActivationFingerprint.Compute(token), "solo se guarda la huella del token");
        invitation.ExpiresAt.Should().Be(world.Clock.GetUtcNow().UtcDateTime.AddMinutes(15));
        world.AuditStore.Entries.Single().Evento.Should().Be("alta");
        world.AuditStore.Entries.Single().Tipo.Should().Be("administracion");
    }

    [Fact]
    public async Task Alta_Con_Un_Celular_Sin_Cuenta_De_La_App_Falla_Sin_Crear_Nada_CA_033_21()
    {
        var world = new PanelWorld();
        var console = new FakePanelConsole();

        var exit = await world.Cli(console).RunAsync(Alta(PanelWorld.OtherPhone), CancellationToken.None);

        exit.Should().Be(1);
        NothingCreated(world);
        console.Errors.Should().Contain("cuenta de la app").And.NotContain(PanelWorld.OtherPhone);
    }

    [Theory]
    [InlineData("superusuario")]
    [InlineData("administrador,administrador")]
    [InlineData("")]
    public async Task Alta_Con_Un_Rol_Fuera_De_La_Lista_Falla_Sin_Crear_Nada_CA_033_21(string roles)
    {
        var world = new PanelWorld();

        var exit = await world.Cli(new FakePanelConsole()).RunAsync(Alta(roles: roles), CancellationToken.None);

        exit.Should().NotBe(0);
        NothingCreated(world);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Alta_Con_Nombre_Vacio_O_De_Mas_De_60_Caracteres_Falla_Sin_Crear_Nada_R_25(string name)
    {
        var world = new PanelWorld();

        (await world.Cli(new FakePanelConsole()).RunAsync(Alta(name: name), CancellationToken.None)).Should().Be(1);
        (await world.Cli(new FakePanelConsole()).RunAsync(Alta(name: new string('a', 61)), CancellationToken.None)).Should().Be(1);
        NothingCreated(world);
    }

    [Fact]
    public async Task Alta_Con_Secreto_De_Prueba_Fuera_De_Development_Falla_Sin_Crear_Nada_CA_033_21()
    {
        var world = new PanelWorld("Production");
        var console = new FakePanelConsole();

        var exit = await world.Cli(console).RunAsync(Alta(testSecret: _testSecret), CancellationToken.None);

        exit.Should().Be(1);
        NothingCreated(world);
        console.Errors.Should().Contain("Development").And.NotContain(_testSecret);
    }

    [Fact]
    public async Task Alta_Con_Secreto_De_Prueba_En_Development_Crea_Cuenta_Configurada_Con_El_Secreto_Cifrado_R_26()
    {
        var world = new PanelWorld("Development");
        var console = new FakePanelConsole();

        var exit = await world.Cli(console).RunAsync(Alta(testSecret: _testSecret), CancellationToken.None);

        exit.Should().Be(0, console.Errors);
        var account = world.Accounts.Accounts.Values.Single();
        account.TotpEstado.Should().Be("configurado");
        account.TotpSecretoCifrado.Should().NotBeEmpty();
        var secret = world.Totp.Unprotect(account.TotpSecretoCifrado, account.TotpVersionLlave, account.Sub);
        OtpNet.Base32Encoding.ToString(secret).Should().Be(_testSecret);
        console.Output.Should().NotContain(_testSecret).And.NotContain("activacion#");
        world.Invitations.Invitations.Should().BeEmpty("la cuenta de prueba no necesita invitación");
    }

    [Fact]
    public async Task Alta_Con_Secreto_De_Prueba_Invalido_Falla_Sin_Crear_Nada_R_26()
    {
        var world = new PanelWorld("Development");

        (await world.Cli(new FakePanelConsole()).RunAsync(Alta(testSecret: "no es base32 !!"), CancellationToken.None)).Should().Be(1);
        (await world.Cli(new FakePanelConsole()).RunAsync(Alta(testSecret: "ABC"), CancellationToken.None)).Should().Be(1);

        NothingCreated(world);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public async Task Con_La_Salida_Redirigida_O_Sin_Terminal_Interactiva_Falla_Sin_Crear_Nada_CA_033_21(bool redirected, bool interactive)
    {
        var world = new PanelWorld("Production");
        var console = new FakePanelConsole(interactive, redirected);

        var exit = await world.Cli(console).RunAsync(Alta(), CancellationToken.None);

        exit.Should().Be(1);
        NothingCreated(world);
        console.Output.Should().BeEmpty("no se imprime ningún enlace");
    }

    [Fact]
    public async Task Sin_Origen_Del_Panel_Configurado_No_Se_Crea_Nada_R_25()
    {
        var world = new PanelWorld("Production");
        var settings = new PanelSettings("knock-api", "knock-panel", world.Settings.SigningKey, null, true);
        var cli = new PanelCli(world.Admin, settings, world.Environment, new FakePanelConsole());

        (await cli.RunAsync(Alta(), CancellationToken.None)).Should().Be(1);

        NothingCreated(world);
    }

    [Fact]
    public async Task Una_Segunda_Alta_De_La_Misma_Persona_Falla_Y_No_Cambia_La_Cuenta_R_01()
    {
        var world = new PanelWorld();
        await world.Cli(new FakePanelConsole()).RunAsync(Alta(), CancellationToken.None);

        var exit = await world.Cli(new FakePanelConsole()).RunAsync(Alta(roles: "analista"), CancellationToken.None);

        exit.Should().Be(1);
        world.Accounts.Accounts.Values.Single().Roles.Should().Equal("administrador");
    }

    [Fact]
    public async Task Cambiar_Roles_Aumenta_La_Version_Borra_La_Cache_Revoca_Sesiones_Cancela_Sid_Y_Audita_CA_033_05()
    {
        var world = new PanelWorld();
        var account = world.SeedAccount();
        await world.Sessions.CreateAsync(new Knock.Identity.Api.Modules.Panel.Data.NuevaSesionPanel("sid-1", account.Sub, "fp1", 1, world.Clock.GetUtcNow().UtcDateTime, world.Clock.GetUtcNow().UtcDateTime.AddMinutes(20), world.Clock.GetUtcNow().UtcDateTime.AddHours(8)), CancellationToken.None);
        world.Cache.Entries[account.Sub] = new Knock.Identity.Api.Modules.Panel.Data.CuentaPanelEnCache(["administrador"], "activa", 1, "Ana");

        var exit = await world.Cli(new FakePanelConsole()).RunAsync(["roles", "--celular", PanelWorld.Phone, "--roles", "verificador"], CancellationToken.None);

        exit.Should().Be(0);
        var changed = world.Accounts.Accounts[account.Sub];
        changed.Roles.Should().Equal("verificador");
        changed.PermisosVersion.Should().Be(2);
        world.Cache.Entries.Should().BeEmpty("la clave de la caché se borra antes de terminar");
        world.Sessions.Sessions.Single().Revocada.Should().BeTrue();
        world.Cancelled.Entries.Should().ContainSingle().Which.SessionId.Should().Be("sid-1");
        world.AuditStore.Entries.Single().Evento.Should().Be("roles_cambiados");
    }

    [Fact]
    public async Task Cambiar_Roles_A_Una_Lista_Invalida_No_Cambia_Nada_R_02()
    {
        var world = new PanelWorld();
        var account = world.SeedAccount();

        var exit = await world.Cli(new FakePanelConsole()).RunAsync(["roles", "--celular", PanelWorld.Phone, "--roles", "dios"], CancellationToken.None);

        exit.Should().Be(1);
        world.Accounts.Accounts[account.Sub].Should().Be(account);
        world.AuditStore.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Deshabilitar_Y_Habilitar_Cambian_El_Estado_Aumentan_La_Version_Y_Cierran_Sesiones_R_03()
    {
        var world = new PanelWorld();
        var account = world.SeedAccount();

        (await world.Cli(new FakePanelConsole()).RunAsync(["deshabilitar", "--celular", PanelWorld.Phone], CancellationToken.None)).Should().Be(0);
        world.Accounts.Accounts[account.Sub].Estado.Should().Be("deshabilitada");
        world.Accounts.Accounts[account.Sub].PermisosVersion.Should().Be(2);
        (await world.Cli(new FakePanelConsole()).RunAsync(["habilitar", "--celular", PanelWorld.Phone], CancellationToken.None)).Should().Be(0);

        world.Accounts.Accounts[account.Sub].Estado.Should().Be("activa");
        world.Accounts.Accounts[account.Sub].PermisosVersion.Should().Be(3);
        world.AuditStore.Entries.Select(entry => entry.Evento).Should().Equal("deshabilitada", "habilitada");
    }

    [Fact]
    public async Task Reiniciar_Totp_Deja_La_Cuenta_Pendiente_Revoca_Sesiones_Y_Crea_Una_Invitacion_Nueva_R_25()
    {
        var world = new PanelWorld("Production");
        var account = world.SeedAccount();
        var console = new FakePanelConsole();

        var exit = await world.Cli(console).RunAsync(["reiniciar-totp", "--celular", PanelWorld.Phone], CancellationToken.None);

        exit.Should().Be(0, console.Errors);
        var reset = world.Accounts.Accounts[account.Sub];
        reset.TotpEstado.Should().Be("pendiente");
        reset.TotpSecretoCifrado.Should().BeNull();
        reset.PermisosVersion.Should().Be(2);
        world.Invitations.Invitations.Should().ContainSingle();
        console.Output.Should().Contain("activacion#token=").And.NotContainEquivalentOf("otpauth");
        world.AuditStore.Entries.Single().Evento.Should().Be("totp_reiniciado");
    }

    [Fact]
    public async Task Reiniciar_Totp_Con_La_Salida_Redirigida_No_Cambia_Nada_R_25()
    {
        var world = new PanelWorld("Production");
        var account = world.SeedAccount();

        var exit = await world.Cli(new FakePanelConsole(redirected: true)).RunAsync(["reiniciar-totp", "--celular", PanelWorld.Phone], CancellationToken.None);

        exit.Should().Be(1);
        world.Accounts.Accounts[account.Sub].Should().Be(account);
        world.Invitations.Invitations.Should().BeEmpty();
    }

    [Fact]
    public async Task Un_Comando_Sobre_Una_Persona_Sin_Cuenta_Del_Panel_Falla_R_25()
    {
        var world = new PanelWorld();

        var exit = await world.Cli(new FakePanelConsole()).RunAsync(["deshabilitar", "--celular", PanelWorld.Phone], CancellationToken.None);

        exit.Should().Be(1);
        world.AuditStore.Entries.Should().BeEmpty();
    }

    [Theory]
    [InlineData("desconocido")]
    [InlineData("alta", "--celular")]
    [InlineData("alta", "--celular", "+525555550211")]
    [InlineData("roles", "--celular", "+525555550211", "--celular", "+525555550211")]
    [InlineData("deshabilitar", "celular", "+525555550211")]
    public async Task Un_Uso_Incorrecto_Imprime_La_Ayuda_Y_Sale_Con_2_Sin_Tocar_Nada(params string[] args)
    {
        var world = new PanelWorld();
        var console = new FakePanelConsole();

        var exit = await world.Cli(console).RunAsync(args, CancellationToken.None);

        exit.Should().Be(2);
        console.Errors.Should().Contain("Uso:");
        NothingCreated(world);
    }

    [Fact]
    public async Task El_Alta_Falla_Cerrada_Si_La_Auditoria_No_Se_Puede_Escribir_Y_Lo_Dice_R_24()
    {
        var world = new PanelWorld();
        world.AuditStore.Fails = true;

        var act = () => world.Cli(new FakePanelConsole()).RunAsync(Alta(), CancellationToken.None);

        await act.Should().ThrowAsync<Knock.Identity.Api.Common.Errors.DomainException>();
    }

    [Fact]
    public void Los_Roles_Y_Permisos_Del_Alta_Salen_De_La_Lista_Cerrada_R_02() =>
        PanelRoles.All.Should().Equal("administrador", "verificador", "moderador", "catalogo", "analista");
}
