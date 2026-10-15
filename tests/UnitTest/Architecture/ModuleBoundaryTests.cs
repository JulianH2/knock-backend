using AwesomeAssertions;
using Xunit;

namespace Knock.UnitTest.Architecture;

public class ModuleBoundaryTests
{
    private const string _fixtureRoot = "Knock.UnitTest.Architecture.Fixtures";

    [Fact]
    public void Modules_DoNotReferenceOtherModulesInternals_TC_TT_API_01_07()
    {
        foreach (var module in ModuleCatalog.Names)
        {
            ModuleCatalog.ApiAssembly.GetTypes()
                .Should().Contain(
                    t => t.Namespace != null && t.Namespace.StartsWith($"{ModuleCatalog.ModulesNamespace}.{module}", StringComparison.Ordinal),
                    $"el módulo {module} debe existir para que la regla no pase en vacío");
        }

        var result = ModuleBoundaryRule.Evaluate(ModuleCatalog.ApiAssembly, ModuleCatalog.ModulesNamespace, ModuleCatalog.Names);

        result.FailingTypes.Should().BeEmpty();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void BoundaryRule_WithViolatingFixture_Fails_TC_TT_API_01_08()
    {
        var violating = ModuleBoundaryRule.Evaluate(typeof(ModuleBoundaryTests).Assembly, $"{_fixtureRoot}.Violating", ["Alpha", "Beta"]);

        violating.IsSuccessful.Should().BeFalse();
        violating.FailingTypes.Should().Contain(name => name.EndsWith("AlphaUsesBetaInternal", StringComparison.Ordinal));
    }

    [Fact]
    public void BoundaryRule_WithCompliantFixture_Passes_TC_TT_API_01_08()
    {
        var compliant = ModuleBoundaryRule.Evaluate(typeof(ModuleBoundaryTests).Assembly, $"{_fixtureRoot}.Compliant", ["Alpha", "Beta"]);

        compliant.FailingTypes.Should().BeEmpty();
        compliant.IsSuccessful.Should().BeTrue();
    }
}
