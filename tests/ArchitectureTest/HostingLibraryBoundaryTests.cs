using System.Reflection;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;
using Xunit;

namespace Knock.ArchitectureTest;

/// <summary>
/// Fronteras de la biblioteca técnica compartida (spec 055): no conoce a ningún servicio ni a <c>Knock.Contracts</c>, no tiene controladores ni módulos de negocio,
/// y los servicios ya no definen las piezas que se movieron en la etapa 1.
/// </summary>
public class HostingLibraryBoundaryTests
{
    private const string _library = "Knock.Hosting";

    private static readonly string[] _projects = ["Knock.Gateway", "Knock.Identity.Api", "Knock.Messaging.Api", "Knock.Market.Api"];

    /// <summary>Piezas de la etapa 1 (R-10 y R-11): si vuelven a aparecer en un servicio, es una copia nueva (R-09).</summary>
    private static readonly string[] _stage1Types =
        ["ForwardedHeadersExtensions", "TelemetryRedactor", "CoordinatePairs", "TelemetryRedactionProcessor", "LogRedactionProcessor", "RedactingJsonFormatter"];

    public static TheoryData<string> Projects
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var project in _projects)
            {
                data.Add(project);
            }

            return data;
        }
    }

    [Fact]
    public void No_Referencia_Servicios_Ni_Contratos_CA_055_01()
    {
        var references = Assembly.Load(_library).GetReferencedAssemblies().Select(a => a.Name).ToList();

        references.Should().NotContain([.. _projects, "Knock.Contracts"]);
    }

    [Fact]
    public void Sin_Controladores_Ni_Modulos_CA_055_02()
    {
        var types = Types.InAssembly(Assembly.Load(_library));

        types.ShouldNot().Inherit<ControllerBase>().GetResult().IsSuccessful.Should().BeTrue();
        types.That().ResideInNamespaceContaining(".Modules.").GetTypes().Should().BeEmpty();
    }

    [Fact]
    public void La_Regla_Puede_Fallar_CA_055_02()
    {
        // Caso negativo: la misma regla encuentra los controladores de un servicio.
        Types.InAssembly(Assembly.Load("Knock.Market.Api")).ShouldNot().Inherit<ControllerBase>().GetResult().IsSuccessful.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void Servicios_Sin_Copias_De_La_Etapa_1_CA_055_03(string project)
    {
        var assembly = Assembly.Load(project);

        assembly.GetTypes().Select(t => t.Name).Should().NotContain(_stage1Types);
        assembly.GetReferencedAssemblies().Select(a => a.Name).Should().Contain(_library);
    }

    [Fact]
    public void Gateway_Sin_Copia_Propia_De_Los_Encabezados_Reenviados_CA_055_03() =>
        Assembly.Load("Knock.Gateway").GetTypes().SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Select(m => m.Name).Should().NotContain("AddKnockGatewayForwardedHeaders");
}
