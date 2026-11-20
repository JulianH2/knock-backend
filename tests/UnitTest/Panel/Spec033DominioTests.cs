using AwesomeAssertions;
using Knock.Identity.Api.Modules.Panel.Domain;
using Xunit;

namespace Knock.UnitTest.Panel;

/// <summary>Spec 033, T-02: roles, permisos, vidas de la sesión, ventana de fallos y clasificación del reuso (R-02, R-11, R-13, R-15, R-17).</summary>
public class Spec033DominioTests
{
    private static readonly DateTime _now = new(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);

    private static SesionPanel Session(DateTime? createdAt = null, string[]? used = null, DateTime? rotatedAt = null, bool revoked = false)
    {
        var created = createdAt ?? _now;
        return new SesionPanel(
            "sid", "sub", "actual", used ?? [], rotatedAt ?? created, 1, created, created,
            PanelSesionReglas.RenewalExpiry(created, PanelSesionReglas.SessionExpiry(created)), PanelSesionReglas.SessionExpiry(created), revoked);
    }

    [Fact]
    public void Administrador_Tiene_Los_Cinco_Permisos_En_El_Orden_De_La_Lista_R_02() =>
        MapaDeRoles.PermisosDe([PanelRoles.Administrador]).Should().Equal("verification:review", "reports:resolve", "reviews:moderate", "catalog:manage", "metrics:read");

    [Theory]
    [InlineData(PanelRoles.Verificador, "verification:review")]
    [InlineData(PanelRoles.Catalogo, "catalog:manage")]
    [InlineData(PanelRoles.Analista, "metrics:read")]
    public void Roles_De_Un_Permiso_R_02(string role, string permission) =>
        MapaDeRoles.PermisosDe(role).Should().Equal(permission);

    [Fact]
    public void Moderador_Resuelve_Reportes_Y_Modera_Resenas_Pero_No_Verifica_R_02()
    {
        var permissions = MapaDeRoles.PermisosDe([PanelRoles.Moderador]);

        permissions.Should().Equal("reports:resolve", "reviews:moderate");
        permissions.Should().NotContain(PanelPermisos.VerificationReview);
    }

    [Fact]
    public void Los_Permisos_De_Varios_Roles_Son_La_Union_Sin_Repetidos_Y_En_Orden_R_02() =>
        MapaDeRoles.PermisosDe([PanelRoles.Analista, PanelRoles.Moderador, PanelRoles.Verificador, PanelRoles.Moderador])
            .Should().Equal("verification:review", "reports:resolve", "reviews:moderate", "metrics:read");

    [Fact]
    public void Un_Rol_Desconocido_No_Aporta_Permisos_R_02() =>
        MapaDeRoles.PermisosDe(["superusuario"]).Should().BeEmpty();

    [Theory]
    [InlineData(new string[0], false)]
    [InlineData(new[] { "administrador" }, true)]
    [InlineData(new[] { "administrador", "administrador" }, false)]
    [InlineData(new[] { "superusuario" }, false)]
    [InlineData(new[] { "administrador", "verificador", "moderador", "catalogo", "analista" }, true)]
    public void Una_Cuenta_Tiene_De_Uno_A_Cinco_Roles_De_La_Lista_Cerrada_R_02(string[] roles, bool valid) =>
        MapaDeRoles.AreValid(roles).Should().Be(valid);

    [Fact]
    public void La_Sesion_Vence_A_Las_Ocho_Horas_Y_No_Se_Renueva_CA_033_14()
    {
        var session = Session(createdAt: _now);

        PanelSesionReglas.CannotRenew(session with { RenovacionVenceEn = _now.AddHours(9) }, _now.AddHours(7).AddMinutes(59)).Should().BeFalse("la hora 7:59 todavía renueva");
        PanelSesionReglas.CannotRenew(session with { RenovacionVenceEn = _now.AddHours(9) }, _now.AddHours(8).AddMinutes(1))
            .Should().BeTrue("la hora 8:01 no renueva aunque la cookie sea reciente");
    }

    [Fact]
    public void La_Cookie_De_Renovacion_Vence_A_Los_20_Minutos_R_16()
    {
        var session = Session();

        PanelSesionReglas.CannotRenew(session, _now.AddMinutes(19)).Should().BeFalse();
        PanelSesionReglas.CannotRenew(session, _now.AddMinutes(21)).Should().BeTrue();
    }

    [Fact]
    public void Una_Sesion_Revocada_No_Se_Renueva_R_17() =>
        PanelSesionReglas.CannotRenew(Session(revoked: true), _now).Should().BeTrue();

    [Fact]
    public void Los_Tokens_No_Pasan_De_La_Vida_De_La_Sesion_R_13_R_15()
    {
        var expiry = PanelSesionReglas.SessionExpiry(_now);

        PanelSesionReglas.AccessExpiry(_now, expiry).Should().Be(_now.AddMinutes(15));
        PanelSesionReglas.AccessExpiry(expiry.AddMinutes(-5), expiry).Should().Be(expiry);
        PanelSesionReglas.RenewalExpiry(_now, expiry).Should().Be(_now.AddMinutes(20));
        PanelSesionReglas.RenewalExpiry(expiry.AddMinutes(-5), expiry).Should().Be(expiry);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 1)]
    [InlineData(4, 2)]
    public void Al_Abrir_La_Cuarta_Sesion_Se_Revoca_La_Mas_Antigua_CA_033_16(int open, int revoked) =>
        PanelSesionReglas.SessionsToRevoke(open).Should().Be(revoked);

    [Fact]
    public void La_Ventana_De_Fallos_Es_Fija_De_15_Minutos_R_11()
    {
        PanelSesionReglas.FailureWindowEnded(null, _now).Should().BeTrue();
        PanelSesionReglas.FailureWindowEnded(_now.AddMinutes(-14), _now).Should().BeFalse();
        PanelSesionReglas.FailureWindowEnded(_now.AddMinutes(-15), _now).Should().BeTrue();
        PanelSesionReglas.LockUntil(_now).Should().Be(_now.AddMinutes(15));
    }

    [Fact]
    public void La_Huella_Anterior_Dentro_De_Diez_Segundos_Es_Renovacion_Concurrente_CA_033_13()
    {
        var session = Session(used: ["vieja", "anterior"], rotatedAt: _now);

        PanelReuseClassifier.Classify(session, "anterior", _now.AddSeconds(10)).Should().Be(PanelReuse.Concurrent);
    }

    [Fact]
    public void La_Huella_Anterior_Pasados_Diez_Segundos_Es_Robo_CA_033_26()
    {
        var session = Session(used: ["vieja", "anterior"], rotatedAt: _now);

        PanelReuseClassifier.Classify(session, "anterior", _now.AddSeconds(11)).Should().Be(PanelReuse.Stolen);
    }

    [Fact]
    public void Cualquier_Otra_Huella_Usada_Es_Robo_Aunque_Sea_Reciente_CA_033_26()
    {
        var session = Session(used: ["vieja", "anterior"], rotatedAt: _now);

        PanelReuseClassifier.Classify(session, "vieja", _now.AddSeconds(1)).Should().Be(PanelReuse.Stolen);
    }

    [Fact]
    public void Una_Huella_Desconocida_No_Es_Reuso_R_17() =>
        PanelReuseClassifier.Classify(Session(used: ["anterior"]), "otra", _now).Should().Be(PanelReuse.Unknown);

    [Fact]
    public void Una_Cuenta_Solo_Tiene_Totp_Si_Esta_Configurado_Y_Con_Secreto_R_06()
    {
        var pending = new CuentaPanel("1", "s", "Ana", ["administrador"], "activa", 1, null, 0, 0, "pendiente", 0, null, null);

        pending.TieneTotp.Should().BeFalse();
        (pending with { TotpEstado = "configurado", TotpSecretoCifrado = [1] }).TieneTotp.Should().BeTrue();
        (pending with { TotpEstado = "configurado" }).TieneTotp.Should().BeFalse();
    }

    [Fact]
    public void Una_Cuenta_Bloqueada_Hasta_El_Futuro_Esta_Bloqueada_R_11()
    {
        var account = new CuentaPanel("1", "s", "Ana", ["administrador"], "activa", 1, null, 0, 0, "pendiente", 5, _now, _now.AddMinutes(15));

        account.EstaBloqueada(_now).Should().BeTrue();
        account.EstaBloqueada(_now.AddMinutes(15)).Should().BeFalse();
    }
}
