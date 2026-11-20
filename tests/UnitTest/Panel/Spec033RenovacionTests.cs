using AwesomeAssertions;
using Knock.Identity.Api.Modules.Panel.Services;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

namespace Knock.UnitTest.Panel;

/// <summary>Spec 033, T-07: renovación, reuso, vida de 8 horas y cierre (R-17, R-18 y R-19; CA-11 a CA-15 y CA-26).</summary>
public class Spec033RenovacionTests
{
    private const string _ip = "203.0.113.7";
    private static readonly byte[] _secret = System.Text.Encoding.ASCII.GetBytes("12345678901234567890");

    private static async Task<(PanelWorld World, PanelGrant Grant)> StartAsync()
    {
        var world = new PanelWorld();
        world.SeedAccount(secret: _secret);
        var phone = await world.Access.VerifyPhoneAsync(world.TokenFor(), _ip, CancellationToken.None);
        var challenge = ((PhoneOutcome.Challenge)phone).ChallengeId;
        var granted = (TotpOutcome.Granted)await world.Access.VerifyTotpAsync(challenge, world.CurrentCode(_secret), _ip, CancellationToken.None);
        return (world, granted.Grant);
    }

    private static Task<RenewOutcome> RenewAsync(PanelWorld world, string token, string? marker = "1", string? origin = PanelWorld.Origin) =>
        world.Lifecycle.RenewAsync(token, marker, origin, CancellationToken.None);

    [Fact]
    public async Task Renueva_Rota_El_Token_Y_La_Cookie_Anterior_Ya_No_Sirve_CA_033_11()
    {
        var (world, grant) = await StartAsync();
        world.Clock.Advance(TimeSpan.FromMinutes(14));

        var renewed = (await RenewAsync(world, grant.RefreshToken)).Should().BeOfType<RenewOutcome.Renewed>().Subject.Grant;

        renewed.AccessToken.Should().NotBe(grant.AccessToken);
        renewed.RefreshToken.Should().NotBe(grant.RefreshToken);
        renewed.Permisos.Should().Equal(grant.Permisos);
        new JsonWebTokenHandler().ReadJsonWebToken(renewed.AccessToken).GetClaim("sid").Value.Should().Be(world.Sessions.Sessions.Single().Id, "es la misma sesión");
        world.Clock.Advance(TimeSpan.FromSeconds(11));
        (await RenewAsync(world, grant.RefreshToken)).Should().BeOfType<RenewOutcome.InvalidSession>();
        world.AuditStore.Entries.Select(entry => entry.Evento).Should().Contain("renovacion");
    }

    [Theory]
    [InlineData(null, PanelWorld.Origin)]
    [InlineData("0", PanelWorld.Origin)]
    [InlineData("1", "https://otro.example")]
    [InlineData("1", null)]
    [InlineData("1", "https://panel.example/")]
    public async Task Sin_Encabezado_O_Con_Otro_Origen_Es_403_Y_No_Rota_Nada_CA_033_12(string? marker, string? origin)
    {
        var (world, grant) = await StartAsync();
        var before = world.Sessions.Sessions.Single();

        var outcome = await RenewAsync(world, grant.RefreshToken, marker, origin);

        outcome.Should().BeOfType<RenewOutcome.OriginNotAllowed>();
        world.Sessions.Sessions.Single().Should().Be(before);
    }

    [Fact]
    public async Task Dos_Renovaciones_Simultaneas_Dan_Un_200_Y_Un_401_Sin_Revocar_Ni_Borrar_La_Cookie_CA_033_13()
    {
        var (world, grant) = await StartAsync();

        var outcomes = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => RenewAsync(world, grant.RefreshToken)));

        outcomes.OfType<RenewOutcome.Renewed>().Should().ContainSingle();
        var loser = outcomes.OfType<RenewOutcome.InvalidSession>().Should().ContainSingle().Subject;
        loser.ClearCookie.Should().BeFalse();
        world.Sessions.Sessions.Single().Revocada.Should().BeFalse();
        world.AuditStore.Entries.Select(entry => entry.Evento).Should().Contain("renovacion_concurrente");
    }

    [Fact]
    public async Task La_Huella_Anterior_Dentro_De_Diez_Segundos_Es_Concurrente_Y_Pasada_La_Gracia_Es_Robo_CA_033_26()
    {
        var (world, grant) = await StartAsync();
        var renewed = ((RenewOutcome.Renewed)await RenewAsync(world, grant.RefreshToken)).Grant;
        world.Clock.Advance(TimeSpan.FromSeconds(5));

        var tolerated = await RenewAsync(world, grant.RefreshToken);

        tolerated.Should().Be(new RenewOutcome.InvalidSession(false));
        world.Sessions.Sessions.Single().Revocada.Should().BeFalse();
        world.Clock.Advance(TimeSpan.FromSeconds(6));
        var stolen = await RenewAsync(world, grant.RefreshToken);
        stolen.Should().Be(new RenewOutcome.InvalidSession(true));
        world.Sessions.Sessions.Should().OnlyContain(session => session.Revocada);
        world.Cancelled.Entries.Should().ContainSingle();
        world.AuditStore.Entries.Select(entry => entry.Evento).Should().Contain("reuso_detectado");
        (await RenewAsync(world, renewed.RefreshToken)).Should().BeOfType<RenewOutcome.InvalidSession>("la sesión ya está revocada");
    }

    [Fact]
    public async Task Una_Huella_Mas_Vieja_Que_La_Anterior_Es_Robo_Aunque_Sea_Reciente_CA_033_26()
    {
        var (world, grant) = await StartAsync();
        var second = ((RenewOutcome.Renewed)await RenewAsync(world, grant.RefreshToken)).Grant;
        world.Clock.Advance(TimeSpan.FromSeconds(1));
        await RenewAsync(world, second.RefreshToken);
        world.Clock.Advance(TimeSpan.FromSeconds(1));

        var outcome = await RenewAsync(world, grant.RefreshToken);

        outcome.Should().Be(new RenewOutcome.InvalidSession(true));
        world.Sessions.Sessions.Single().Revocada.Should().BeTrue();
    }

    [Fact]
    public async Task Un_Token_Desconocido_Es_401_Invalid_Session_Y_Borra_La_Cookie_R_17()
    {
        var (world, _) = await StartAsync();

        (await RenewAsync(world, "desconocido")).Should().Be(new RenewOutcome.InvalidSession(true));
        (await world.Lifecycle.RenewAsync(null, "1", PanelWorld.Origin, CancellationToken.None)).Should().Be(new RenewOutcome.InvalidSession(true));
        world.Sessions.Sessions.Single().Revocada.Should().BeFalse();
    }

    [Fact]
    public async Task Una_Sesion_De_Mas_De_Ocho_Horas_No_Renueva_Aunque_La_Cookie_Sea_Reciente_CA_033_14()
    {
        var (world, grant) = await StartAsync();
        var token = grant.RefreshToken;
        for (var minutes = 0; minutes < 7 * 60 + 50; minutes += 10)
        {
            world.Clock.Advance(TimeSpan.FromMinutes(10));
            token = ((RenewOutcome.Renewed)await RenewAsync(world, token)).Grant.RefreshToken;
        }

        world.Clock.Advance(TimeSpan.FromMinutes(11));

        (await RenewAsync(world, token)).Should().Be(new RenewOutcome.InvalidSession(true));
    }

    [Fact]
    public async Task La_Cookie_Vencida_A_Los_20_Minutos_No_Renueva_R_16()
    {
        var (world, grant) = await StartAsync();
        world.Clock.Advance(TimeSpan.FromMinutes(21));

        (await RenewAsync(world, grant.RefreshToken)).Should().BeOfType<RenewOutcome.InvalidSession>();
    }

    [Fact]
    public async Task Si_Cambiaron_Los_Roles_La_Renovacion_Es_401_Permissions_Changed_Y_Revoca_La_Sesion_CA_033_05()
    {
        var (world, grant) = await StartAsync();
        // La herramienta ya revoca las sesiones al cambiar roles; aquí se simula un cambio que no alcanzó a revocar (otra réplica, otro camino).
        world.Accounts.Accounts[PanelWorld.AppSub] = world.Accounts.Accounts[PanelWorld.AppSub] with { PermisosVersion = 2 };

        var outcome = await RenewAsync(world, grant.RefreshToken);

        outcome.Should().BeOfType<RenewOutcome.PermissionsChanged>();
        world.Sessions.Sessions.Single().Revocada.Should().BeTrue();
        (await RenewAsync(world, grant.RefreshToken)).Should().BeOfType<RenewOutcome.InvalidSession>("la renovación con su cookie ya responde 401");
    }

    [Fact]
    public async Task Tras_Cambiar_Roles_Con_La_Herramienta_La_Cookie_Anterior_Responde_401_CA_033_05()
    {
        var (world, grant) = await StartAsync();
        await world.Admin.SetRolesAsync(PanelWorld.Phone, ["moderador"], CancellationToken.None);

        (await RenewAsync(world, grant.RefreshToken)).Should().BeOfType<RenewOutcome.InvalidSession>();
        world.Sessions.Sessions.Single().Revocada.Should().BeTrue();
    }

    [Fact]
    public async Task Una_Cuenta_Deshabilitada_No_Renueva_R_18()
    {
        var (world, grant) = await StartAsync();
        world.Accounts.Accounts[PanelWorld.AppSub] = world.Accounts.Accounts[PanelWorld.AppSub] with { Estado = "deshabilitada" };

        (await RenewAsync(world, grant.RefreshToken)).Should().BeOfType<RenewOutcome.PermissionsChanged>();
        world.Cancelled.Entries.Should().ContainSingle();
    }

    [Fact]
    public async Task Cerrar_Revoca_La_Sesion_Cancela_El_Sid_Y_Audita_CA_033_15()
    {
        var (world, grant) = await StartAsync();
        var sid = world.Sessions.Sessions.Single().Id;

        await world.Lifecycle.CloseAsync(sid, PanelWorld.AppSub, TimeSpan.FromMinutes(10), CancellationToken.None);

        world.Sessions.Sessions.Single().Revocada.Should().BeTrue();
        world.Cancelled.Entries.Should().ContainSingle().Which.Should().Be((sid, TimeSpan.FromMinutes(10)));
        world.AuditStore.Entries.Select(entry => entry.Evento).Should().Contain("cierre");
        (await RenewAsync(world, grant.RefreshToken)).Should().BeOfType<RenewOutcome.InvalidSession>();
    }

    [Fact]
    public async Task Cerrar_Dos_Veces_Cancela_Igual_Y_No_Falla_Cerrar_Repetido_Lo_Rechaza_La_Autenticacion_CA_033_15()
    {
        var (world, _) = await StartAsync();
        var sid = world.Sessions.Sessions.Single().Id;

        await world.Lifecycle.CloseAsync(sid, PanelWorld.AppSub, TimeSpan.FromMinutes(1), CancellationToken.None);
        await world.Lifecycle.CloseAsync(sid, PanelWorld.AppSub, TimeSpan.Zero, CancellationToken.None);

        world.Cancelled.Entries.Should().HaveCount(2);
        world.Cancelled.Entries[1].Lifetime.Should().BeGreaterThan(TimeSpan.Zero);
    }
}
