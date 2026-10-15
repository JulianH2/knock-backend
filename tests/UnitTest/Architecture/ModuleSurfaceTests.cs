using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Knock.UnitTest.Architecture;

public class ModuleSurfaceTests
{
    public static TheoryData<string> Modules
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var module in ModuleCatalog.Names)
            {
                data.Add(module);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void EachModule_ExposesOnlyAddModuleAndInterface_TC_TT_API_01_09(string module)
    {
        var publicTypes = Types.InAssembly(ModuleCatalog.ApiAssembly)
            .That().ResideInNamespaceMatching($"^{Regex.Escape($"{ModuleCatalog.ModulesNamespace}.{module}")}([.]|$)")
            .And().ArePublic()
            .GetTypes()
            .Select(t => t.ReflectionType)
            .Where(t => !t.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            .ToList();

        var rootTypes = ModuleCatalog.ApiAssembly.GetTypes()
            .Where(t => t.Namespace == $"{ModuleCatalog.ModulesNamespace}.{module}" && !t.IsNested)
            .Where(t => !t.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            .ToList();
        rootTypes.Should().OnlyContain(t => t.IsPublic, "lo interno vive en subespacios (Domain, Services…), que la regla de frontera vigila");

        publicTypes.Should().HaveCount(2, "solo se expone la clase AddXxxModule y la interfaz del módulo");

        var registration = publicTypes.Single(t => t.IsClass);
        registration.IsAbstract.Should().BeTrue();
        registration.IsSealed.Should().BeTrue("es una clase estática");
        registration.GetMethod($"Add{module}Module", BindingFlags.Public | BindingFlags.Static).Should().NotBeNull();

        var contract = publicTypes.Single(t => t.IsInterface);
        contract.Name.Should().Be($"I{module}Api");
    }
}
