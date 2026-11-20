using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Identity.Api.Common.Errors;
using Knock.Identity.Api.Modules.Panel.Data;
using Knock.Identity.Api.Modules.Panel.Services;
using Knock.UnitTest.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Knock.UnitTest.Panel;

/// <summary>Spec 033, T-05: auditoría de solo inserción con falla cerrada y campos prohibidos (R-23 y R-24).</summary>
public class Spec033AuditoriaTests
{
    private sealed class FakeAuditStore : IPanelAuditoriaStore
    {
        public List<PanelAuditEntry> Entries { get; } = [];

        public bool Fails { get; set; }

        public Task InsertAsync(PanelAuditEntry entry, CancellationToken cancellationToken)
        {
            if (Fails)
            {
                throw new TimeoutException("mongodb://usuario:clave@host cadena que no debe salir");
            }

            Entries.Add(entry);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }

    private static (AuditoriaPanel Audit, FakeAuditStore Store, FakeAccessor Accessor, Spec039LoggingTests.Capture<AuditoriaPanel> Logger) Create()
    {
        var store = new FakeAuditStore();
        var accessor = new FakeAccessor();
        var logger = new Spec039LoggingTests.Capture<AuditoriaPanel>();
        var hasher = new PanelIpHasher(PhoneProtectionTestKeys.Keys());
        return (new AuditoriaPanel(store, hasher, accessor, TimeProvider.System, logger), store, accessor, logger);
    }

    private static DefaultHttpContext RequestFrom(string ip)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        return context;
    }

    [Fact]
    public async Task Sin_Poder_Escribir_La_Auditoria_Se_Lanza_503_Audit_Unavailable_Y_No_Se_Filtra_Nada_CA_033_18()
    {
        var (audit, store, accessor, logger) = Create();
        accessor.HttpContext = RequestFrom("203.0.113.9");
        store.Fails = true;

        var thrown = await FluentActions.Awaiting(() => audit.WriteAsync("acceso", "acceso_concedido", "permitido", sub: "sub-1"))
            .Should().ThrowAsync<DomainException>();

        thrown.Which.Kind.Should().Be(DomainErrorKind.ServiceUnavailable);
        thrown.Which.Code.Should().Be("audit_unavailable");
        thrown.Which.Message.Should().NotContain("usuario").And.NotContain("clave");
        logger.Lines.Should().ContainSingle().Which.Should().Contain("acceso_concedido").And.NotContain("sub-1").And.NotContain("clave").And.NotContain("203.0.113.9");
    }

    [Fact]
    public async Task La_Ip_Se_Guarda_Como_Hmac_Estable_Y_Nunca_En_Claro_R_23()
    {
        var (audit, store, accessor, _) = Create();
        accessor.HttpContext = RequestFrom("203.0.113.9");

        await audit.WriteAsync("acceso", "celular_verificado", "permitido");
        await audit.WriteAsync("acceso", "celular_verificado", "permitido");
        accessor.HttpContext = RequestFrom("203.0.113.10");
        await audit.WriteAsync("acceso", "celular_verificado", "permitido");

        store.Entries[0].IpHmac.Should().HaveLength(64).And.NotContain("203.0.113").And.Be(store.Entries[1].IpHmac);
        store.Entries[2].IpHmac.Should().NotBe(store.Entries[0].IpHmac);
    }

    [Fact]
    public async Task La_Herramienta_Interna_Escribe_Sin_Contexto_Http_Con_Origen_Cli_R_23()
    {
        var (audit, store, _, _) = Create();

        await audit.WriteAsync("administracion", "alta", "permitido", sub: "sub-1");

        store.Entries.Should().ContainSingle();
        store.Entries[0].IpHmac.Should().Be("cli");
        store.Entries[0].CorrelationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Lleva_Los_Campos_De_La_Spec_Y_Solo_Esos_R_23()
    {
        var (audit, store, accessor, _) = Create();
        accessor.HttpContext = RequestFrom("203.0.113.9");

        await audit.WriteAsync("accion", "accion", "denegado", "sub-1", "sid-1", "verification:review", "api/v1/panel/prueba/{id}", "abc-123");

        var json = JsonSerializer.Serialize(store.Entries.Single());
        using var document = JsonDocument.Parse(json);
        document.RootElement.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(
            "Fecha", "Tipo", "Evento", "Resultado", "IpHmac", "CorrelationId", "Sub", "Sid", "Permiso", "Ruta", "RecursoId");
        store.Entries[0].Fecha.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData("abc-123", true)]
    [InlineData("665f1b2c3d4e5f6071829304", true)]
    [InlineData("+525555550211", false)]
    [InlineData("Ana Pérez", false)]
    [InlineData("{\"nombre\":\"x\"}", false)]
    [InlineData("", false)]
    public async Task El_Recurso_Solo_Se_Guarda_Si_Parece_Un_Identificador_R_23(string resource, bool kept)
    {
        var (audit, store, _, _) = Create();

        await audit.WriteAsync("accion", "accion", "permitido", resourceId: resource);

        store.Entries.Single().RecursoId.Should().Be(kept ? resource : null);
    }

    [Fact]
    public async Task Un_Recurso_De_Mas_De_64_Caracteres_Se_Descarta_R_23()
    {
        var (audit, store, _, _) = Create();

        await audit.WriteAsync("accion", "accion", "permitido", resourceId: new string('a', 65));

        store.Entries.Single().RecursoId.Should().BeNull();
    }

    [Fact]
    public void La_Entrada_De_Auditoria_No_Tiene_Campos_De_Datos_Personales_Ni_Secretos_CA_033_19()
    {
        var names = typeof(PanelAuditEntry).GetProperties().Select(property => property.Name).ToList();

        names.Should().NotContain(name => name.Contains("Celular", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Token", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Codigo", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Secreto", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Body", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Ip", StringComparison.OrdinalIgnoreCase));
    }
}
