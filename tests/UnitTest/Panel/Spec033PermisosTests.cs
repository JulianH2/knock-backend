using System.Security.Claims;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Identity.Api.Common.Errors;
using Knock.Identity.Api.Modules.Panel.Authorization;
using Knock.Identity.Api.Modules.Panel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Knock.UnitTest.Panel;

/// <summary>Spec 033, T-08: política RequierePermiso, comparación de pv y códigos 401 y 403 (R-20 a R-22; CA-04, CA-05 y CA-20).</summary>
public class Spec033PermisosTests
{
    private static readonly byte[] _secret = System.Text.Encoding.ASCII.GetBytes("12345678901234567890");

    private sealed class Holder : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }

    private static (PermisoPanelHandler Handler, PanelWorld World, DefaultHttpContext Http) Create(string[]? roles = null)
    {
        var world = new PanelWorld();
        world.SeedAccount(roles: roles, secret: _secret);
        var http = new DefaultHttpContext();
        var handler = new PermisoPanelHandler(world.Cache, world.Accounts, world.Sessions, world.Cancelled, world.Audit, new Holder { HttpContext = http });
        return (handler, world, http);
    }

    private static async Task<(AuthorizationHandlerContext Context, PermisoPanelRequirement Requirement)> RunAsync(
        PermisoPanelHandler handler, string permission, string sub, string sid, int version)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", sub), new Claim("sid", sid), new Claim("pv", version.ToString(System.Globalization.CultureInfo.InvariantCulture))], "PanelBearer"));
        var requirement = new PermisoPanelRequirement(permission);
        var context = new AuthorizationHandlerContext([requirement], user, null);
        await handler.HandleAsync(context);
        return (context, requirement);
    }

    private static async Task<string> OpenSessionAsync(PanelWorld world)
    {
        var phone = (PhoneOutcome.Challenge)await world.Access.VerifyPhoneAsync(world.TokenFor(), "203.0.113.7", CancellationToken.None);
        await world.Access.VerifyTotpAsync(phone.ChallengeId, world.CurrentCode(_secret), "203.0.113.7", CancellationToken.None);
        return world.Sessions.Sessions.Single().Id;
    }

    [Fact]
    public async Task Un_Moderador_No_Pasa_En_Un_Endpoint_De_Verificacion_Y_Queda_Auditoria_Denegado_CA_033_04()
    {
        var (handler, world, http) = Create(["moderador"]);
        var sid = await OpenSessionAsync(world);

        var (context, _) = await RunAsync(handler, "verification:review", PanelWorld.AppSub, sid, 1);

        context.HasSucceeded.Should().BeFalse();
        http.Items[PermisoPanelHandler.DenialItem].Should().Be(new PanelDenial(403, "insufficient_permission"));
        var entry = world.AuditStore.Entries[^1];
        (entry.Resultado, entry.Evento, entry.Permiso, entry.Sub, entry.Sid).Should().Be(("denegado", "accion", "verification:review", PanelWorld.AppSub, sid));
    }

    [Fact]
    public async Task Un_Moderador_Si_Pasa_En_Un_Endpoint_De_Moderar_Resenas_Y_Queda_Auditoria_Permitido_CA_033_04()
    {
        var (handler, world, http) = Create(["moderador"]);
        var sid = await OpenSessionAsync(world);

        var (context, _) = await RunAsync(handler, "reviews:moderate", PanelWorld.AppSub, sid, 1);

        context.HasSucceeded.Should().BeTrue();
        http.Items.Should().NotContainKey(PermisoPanelHandler.DenialItem);
        world.AuditStore.Entries[^1].Resultado.Should().Be("permitido");
    }

    [Fact]
    public async Task Con_Un_Pv_Distinto_Es_401_Permissions_Changed_Revoca_La_Sesion_Y_Cancela_El_Sid_CA_033_05()
    {
        var (handler, world, http) = Create();
        var sid = await OpenSessionAsync(world);
        await world.Admin.SetRolesAsync(PanelWorld.Phone, ["verificador"], CancellationToken.None);

        var (context, _) = await RunAsync(handler, "verification:review", PanelWorld.AppSub, sid, 1);

        context.HasSucceeded.Should().BeFalse();
        http.Items[PermisoPanelHandler.DenialItem].Should().Be(new PanelDenial(401, "permissions_changed"));
        world.Sessions.Sessions.Single().Revocada.Should().BeTrue();
        world.Cancelled.Entries.Select(entry => entry.SessionId).Should().Contain(sid);
        world.AuditStore.Entries[^1].Evento.Should().Be("permisos_cambiados");
    }

    [Fact]
    public async Task Una_Cuenta_Deshabilitada_O_Inexistente_Es_401_Permissions_Changed_R_21()
    {
        var (handler, world, http) = Create();
        var sid = await OpenSessionAsync(world);
        world.Accounts.Accounts[PanelWorld.AppSub] = world.Accounts.Accounts[PanelWorld.AppSub] with { Estado = "deshabilitada" };
        world.Cache.Entries.Clear();

        await RunAsync(handler, "metrics:read", PanelWorld.AppSub, sid, 1);
        http.Items[PermisoPanelHandler.DenialItem].Should().Be(new PanelDenial(401, "permissions_changed"));

        http.Items.Clear();
        await RunAsync(handler, "metrics:read", "sub-que-no-existe", sid, 1);
        http.Items[PermisoPanelHandler.DenialItem].Should().Be(new PanelDenial(401, "permissions_changed"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("otro:permiso")]
    [InlineData("VERIFICATION:REVIEW")]
    public async Task Un_Permiso_Fuera_De_La_Lista_Cerrada_Se_Deniega_Hasta_A_Un_Administrador_CA_033_20(string permission)
    {
        var (handler, world, http) = Create();
        var sid = await OpenSessionAsync(world);

        var (context, _) = await RunAsync(handler, permission, PanelWorld.AppSub, sid, 1);

        context.HasSucceeded.Should().BeFalse();
        http.Items[PermisoPanelHandler.DenialItem].Should().Be(new PanelDenial(403, "insufficient_permission"));
    }

    [Fact]
    public async Task La_Cuenta_Se_Lee_De_La_Cache_En_La_Segunda_Peticion_R_24()
    {
        var (handler, world, _) = Create();
        var sid = await OpenSessionAsync(world);
        var readsBefore = world.Accounts.Reads;

        await RunAsync(handler, "metrics:read", PanelWorld.AppSub, sid, 1);
        var afterFirst = world.Accounts.Reads;
        await RunAsync(handler, "metrics:read", PanelWorld.AppSub, sid, 1);

        afterFirst.Should().Be(readsBefore + 1);
        world.Accounts.Reads.Should().Be(afterFirst, "la segunda petición sale de la caché");
        world.Cache.Entries.Should().ContainKey(PanelWorld.AppSub);
    }

    [Fact]
    public async Task Sin_Poder_Auditar_La_Accion_Se_Niega_Con_503_Audit_Unavailable_CA_033_18()
    {
        var (handler, world, _) = Create();
        var sid = await OpenSessionAsync(world);
        world.AuditStore.Fails = true;

        var thrown = await FluentActions.Awaiting(() => RunAsync(handler, "metrics:read", PanelWorld.AppSub, sid, 1)).Should().ThrowAsync<DomainException>();

        thrown.Which.Code.Should().Be("audit_unavailable");
        thrown.Which.Kind.Should().Be(DomainErrorKind.ServiceUnavailable);
    }

    [Fact]
    public async Task Con_La_Cache_O_La_Base_Caidas_Es_503_Unavailable_Y_No_Se_Concede_R_28()
    {
        var (handler, world, _) = Create();
        var sid = await OpenSessionAsync(world);
        world.Cache.Unavailable = true;

        var thrown = await FluentActions.Awaiting(() => RunAsync(handler, "metrics:read", PanelWorld.AppSub, sid, 1)).Should().ThrowAsync<DomainException>();

        thrown.Which.Code.Should().Be("unavailable");
    }

    [Fact]
    public async Task Un_Token_Sin_Claims_Del_Panel_No_Autoriza_R_13()
    {
        var (handler, _, _) = Create();
        var context = new AuthorizationHandlerContext([new PermisoPanelRequirement("metrics:read")], new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "x")], "PanelBearer")), null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task La_Politica_Se_Arma_Con_El_Esquema_Del_Panel_Y_El_Requisito_Del_Permiso_R_20()
    {
        var provider = new PanelPolicyProvider(Options.Create(new AuthorizationOptions()));

        var policy = await provider.GetPolicyAsync(new RequierePermisoAttribute("metrics:read").Policy!);

        policy!.AuthenticationSchemes.Should().Equal("PanelBearer");
        policy.Requirements.OfType<PermisoPanelRequirement>().Should().ContainSingle().Which.Permission.Should().Be("metrics:read");
        (await provider.GetPolicyAsync("otra-politica")).Should().BeNull();
    }

    [Fact]
    public void El_Atributo_Declara_Un_Solo_Permiso_Y_El_Esquema_Del_Panel_R_20()
    {
        var attribute = new RequierePermisoAttribute("reports:resolve");

        attribute.Permission.Should().Be("reports:resolve");
        attribute.AuthenticationSchemes.Should().Be("PanelBearer");
        typeof(RequierePermisoAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), true).Cast<AttributeUsageAttribute>().Single().AllowMultiple.Should().BeFalse();
    }

    [Theory]
    [InlineData(401, "permissions_changed")]
    [InlineData(403, "insufficient_permission")]
    public async Task El_Resultado_Http_Lleva_El_Estado_Y_El_Code_De_La_Denegacion_Y_No_Se_Cachea_R_22(int status, string code)
    {
        var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services };
        http.Response.Body = new MemoryStream();
        http.Items[PermisoPanelHandler.DenialItem] = new PanelDenial(status, code);
        var handler = new PanelAuthorizationResultHandler();

        await handler.HandleAsync(_ => Task.CompletedTask, http, new AuthorizationPolicyBuilder().RequireAssertion(_ => false).Build(), PolicyAuthorizationResult.Forbid());

        http.Response.StatusCode.Should().Be(status);
        http.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(http.Response.Body);
        body.RootElement.GetProperty("code").GetString().Should().Be(code);
    }

    [Fact]
    public async Task Si_El_Manejador_Del_Panel_No_Dejo_Denegacion_Se_Sigue_El_Camino_Normal_Y_Pasa_Lo_Autorizado()
    {
        var http = new DefaultHttpContext();
        var called = false;
        var handler = new PanelAuthorizationResultHandler();

        await handler.HandleAsync(_ => { called = true; return Task.CompletedTask; }, http, new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build(), PolicyAuthorizationResult.Success());

        called.Should().BeTrue();
    }
}
