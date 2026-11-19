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
}
