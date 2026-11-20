using System.Reflection;
using AwesomeAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Knock.ArchitectureTest;

/// <summary>Spec 033, reglas de arquitectura del módulo Panel de Identity: auditoría de solo inserción, TOTP sin HTTP ni MongoDB y modo de herramienta sin Kestrel.</summary>
public class Spec033ArquitecturaTests
{
    private const string _panel = "Knock.Identity.Api.Modules.Panel";

    private static Assembly Identity => Assembly.Load("Knock.Identity.Api");

    private static Type Find(string fullName) =>
        Identity.GetType(fullName) ?? throw new InvalidOperationException($"{fullName} no existe: la regla pasaría en vacío.");

    [Fact]
    public void Auditoria_Es_De_Solo_Insercion_Sin_Lectura_Actualizacion_Ni_Borrado_CA_033_18()
    {
        var store = Find($"{_panel}.Data.IPanelAuditoriaStore");
        var implementation = Find($"{_panel}.Data.PanelAuditoriaStore");

        store.GetMethods().Select(method => method.Name).Should().Equal("InsertAsync");
        var forbidden = new[] { "Update", "Delete", "Replace", "Find", "Remove", "Set", "Get", "Read", "Query" };
        implementation.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .Where(name => forbidden.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .Should().BeEmpty("la colección es de solo inserción para el servicio");
        implementation.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Should().BeEmpty("no se expone la colección de MongoDB: con ella se podría actualizar o borrar");
    }

    [Fact]
    public void Ningun_Otro_Tipo_Del_Panel_Toca_La_Coleccion_De_Auditoria_CA_033_18()
    {
        // Un tipo del panel distinto del almacén que declare el nombre de la colección de auditoría podría escribirla por su cuenta.
        var offenders = Identity.GetTypes()
            .Where(type => type.Namespace?.StartsWith(_panel, StringComparison.Ordinal) == true && type.Name != "PanelAuditoriaStore")
            .Where(type => type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Any(field => field.IsLiteral && field.GetRawConstantValue() is "panel_auditoria"))
            .Select(type => type.FullName)
            .ToList();

        offenders.Should().BeEmpty();
    }

    [Fact]
    public void TotpService_No_Depende_De_Http_Ni_De_MongoDb_Ni_De_Redis_Spec033_Seccion7()
    {
        var types = Types.InAssembly(Identity).That().HaveName("TotpService");

        types.GetTypes().Should().ContainSingle();
        types.ShouldNot().HaveDependencyOnAny("Microsoft.AspNetCore", "MongoDB.Driver", "StackExchange.Redis").GetResult().IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void La_Herramienta_Interna_No_Registra_Kestrel_Ni_Controllers_Spec033_Seccion7()
    {
        var cli = Types.InAssembly(Identity).That().ResideInNamespace($"{_panel}.Cli");

        cli.GetTypes().Should().NotBeEmpty("si no hay tipos la regla pasaría en vacío");
        cli.ShouldNot().HaveDependencyOnAny("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel", "Microsoft.AspNetCore.Mvc", "Microsoft.AspNetCore.Builder")
            .GetResult().IsSuccessful.Should().BeTrue();
    }

    private static bool HasAttribute(MemberInfo member, string name) =>
        member.GetCustomAttributes(inherit: true).Any(attribute => attribute.GetType().Name == name);

    /// <summary>Controllers bajo <c>/api/v1/panel/</c> que ni son de sesión ni declaran un permiso en cada acción (R-20).</summary>
    private static List<string> PanelControllersWithoutPermission(IEnumerable<Type> types) =>
        [.. types
            .Where(type => type.IsClass && type.Name.EndsWith("Controller", StringComparison.Ordinal))
            .Where(type => type.GetCustomAttributes(inherit: true).Any(attribute => attribute.GetType().Name == "RouteAttribute"
                && attribute.GetType().GetProperty("Template")?.GetValue(attribute) is string template
                && template.Contains("/panel", StringComparison.OrdinalIgnoreCase)))
            .Where(type => !HasAttribute(type, "PanelSesionEndpointAttribute"))
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(method => method.GetCustomAttributes(inherit: true).Any(attribute => attribute.GetType().Name.StartsWith("Http", StringComparison.Ordinal) && attribute.GetType().Name.EndsWith("Attribute", StringComparison.Ordinal)))
                .Where(method => !HasAttribute(method, "RequierePermisoAttribute") && !HasAttribute(type, "RequierePermisoAttribute"))
                .Select(method => $"{type.FullName}.{method.Name}"))];

    [Fact]
    public void Todo_Endpoint_Declara_Permiso_CA_033_20()
    {
        var panelControllers = Identity.GetTypes().Where(type => type.Name.EndsWith("Controller", StringComparison.Ordinal) && type.Namespace?.StartsWith(_panel, StringComparison.Ordinal) == true).ToList();

        panelControllers.Should().HaveCountGreaterThanOrEqualTo(3, "si no hay controllers la regla pasaría en vacío");
        PanelControllersWithoutPermission(Identity.GetTypes()).Should().BeEmpty("los de sesión llevan PanelSesionEndpoint y el resto debe declarar RequierePermiso");
    }

    [Fact]
    public void La_Regla_Puede_Fallar_Para_Un_Controller_Del_Panel_Sin_Permiso_CA_033_20()
    {
        PanelControllersWithoutPermission([typeof(SinPermisoController), typeof(ConPermisoController)])
            .Should().ContainSingle().Which.Should().Contain(nameof(SinPermisoController));
    }

    // Controllers de muestra: solo se inspeccionan por reflexión, nunca se crean.
    [Microsoft.AspNetCore.Mvc.Route("api/v1/panel/prueba")]
    private static class SinPermisoController
    {
        [Microsoft.AspNetCore.Mvc.HttpGet]
        public static void Leer() => throw new NotSupportedException();
    }

    [Microsoft.AspNetCore.Mvc.Route("api/v1/panel/prueba")]
    private static class ConPermisoController
    {
        [Microsoft.AspNetCore.Mvc.HttpGet]
        [RequierePermiso]
        public static void Leer() => throw new NotSupportedException();
    }

    [AttributeUsage(AttributeTargets.Method)]
    private sealed class RequierePermisoAttribute : Attribute;
}
