using System.Reflection;
using AwesomeAssertions;
using Knock.Market.Api.Modules.Catalog;
using Knock.Market.Api.Modules.Catalog.Services;
using Xunit;

namespace Knock.UnitTest.Catalog;

public class CatalogContractTests
{
    [Fact]
    public void CatalogApiHasExactlyTheSixListedMembers_TC_TT_BD_06_17()
    {
        var members = typeof(ICatalogApi).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(member => member is not MethodInfo { IsSpecialName: true })
            .Select(Describe);

        members.Should().BeEquivalentTo(
            "ModuleName : String",
            "FindNeighborhoodAtAsync(Double, Double, CancellationToken) : Task<Nullable<String>>",
            "FindNeighborhoodsByPostalCodeAsync(String, CancellationToken) : Task<IReadOnlyList<String>>",
            "NeighborhoodExistsAsync(String, String, CancellationToken) : Task<Boolean>",
            // Spec 020 (KNOCK-59): lectura por clave de oficio y por id de colonia; devuelven tuplas para no sumar tipos públicos.
            "FindActiveTradeAsync(String, CancellationToken) : Task<Nullable<Nullable<ValueTuple`2>>>",
            "FindNeighborhoodAsync(String, CancellationToken) : Task<Nullable<Nullable<ValueTuple`2>>>");
        typeof(NeighborhoodLocator).IsPublic.Should().BeFalse("la consulta interna no es contrato entre módulos");
        typeof(ICatalogApi).GetMethods().Should().NotContain(method => method.ReturnType.ToString().Contains("Colonia", StringComparison.Ordinal));
    }

    private static string Describe(MemberInfo member) => member switch
    {
        PropertyInfo property => $"{property.Name} : {property.PropertyType.Name}",
        MethodInfo method =>
            $"{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))}) : {TypeName(method.ReturnType, method.ReturnParameter)}",
        _ => member.Name,
    };

    /// <summary>Nombre con genéricos y con la anotación de nulos del primer argumento (<c>Task&lt;string?&gt;</c>).</summary>
    private static string TypeName(Type type, ParameterInfo returnParameter)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var nullability = new NullabilityInfoContext().Create(returnParameter);
        var arguments = type.GetGenericArguments().Select((argument, index) =>
        {
            var name = argument.IsGenericType
                ? $"{argument.Name[..argument.Name.IndexOf('`', StringComparison.Ordinal)]}<{string.Join(", ", argument.GetGenericArguments().Select(a => a.Name))}>"
                : argument.Name;
            return nullability.GenericTypeArguments[index].ReadState == NullabilityState.Nullable ? $"Nullable<{name}>" : name;
        });
        return $"{type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)]}<{string.Join(", ", arguments)}>";
    }
}
