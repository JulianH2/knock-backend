using System.Reflection;
using AwesomeAssertions;
using Knock.Identity.Api.Modules.Sesiones.Services;
using NetArchTest.Rules;
using Xunit;

namespace Knock.UnitTest.Architecture;

/// <summary>
/// Spec 002, CA-02: un controller con acceso directo a MongoDB falla la prueba de arquitectura. Aplica la regla a los controllers de Identity y de
/// Market y comprueba con un controller de muestra que la regla sí puede fallar.
/// </summary>
public class Spec002ServiceStructureTests
{
    private const string _mongoDriver = "MongoDB.Driver";

    private static (int Controllers, IReadOnlyList<string> Failing) ControllersTouchingMongo(Assembly assembly)
    {
        var controllers = Types.InAssembly(assembly).That().HaveNameEndingWith("Controller");
        var count = controllers.GetTypes().Count();
        var result = controllers.ShouldNot().HaveDependencyOnAny(_mongoDriver).GetResult();
        return (count, (result.FailingTypes ?? []).Select(type => type.FullName).ToList());
    }

    [Fact]
    public void ControllersOfIdentity_DoNotTouchMongoDirectly_CA_002_02()
    {
        var (controllers, failing) = ControllersTouchingMongo(typeof(LoginService).Assembly);

        controllers.Should().BeGreaterThan(0, "si no hay controllers la regla pasaría en vacío");
        failing.Should().BeEmpty();
    }

    [Fact]
    public void ControllersOfMarket_DoNotTouchMongoDirectly_CA_002_02()
    {
        var (controllers, failing) = ControllersTouchingMongo(ModuleCatalog.ApiAssembly);

        controllers.Should().BeGreaterThan(0, "si no hay controllers la regla pasaría en vacío");
        failing.Should().BeEmpty();
    }

    [Fact]
    public void WaitlistController_DoesNotTouchMongoOrRedis_CA_023_10()
    {
        var controllers = Types.InAssembly(ModuleCatalog.ApiAssembly).That().HaveName("ListaEsperaController");
        controllers.GetTypes().Should().ContainSingle();
        controllers.ShouldNot().HaveDependencyOnAny("MongoDB.Driver", "StackExchange.Redis").GetResult().IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void TheRuleCanFail_ForAControllerThatUsesAMongoCollection_CA_002_02()
    {
        var (controllers, failing) = ControllersTouchingMongo(typeof(Spec002ServiceStructureTests).Assembly);

        controllers.Should().Be(1);
        failing.Should().ContainSingle().Which.Should().Contain("AlphaController");
    }
}
