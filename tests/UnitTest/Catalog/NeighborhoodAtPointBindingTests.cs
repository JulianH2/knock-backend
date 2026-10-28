using System.Reflection;
using AwesomeAssertions;
using Knock.Api.Modules.Catalog.Controllers.V1;
using Knock.Api.Modules.Catalog.Dtos;
using Knock.Api.Modules.Catalog.ModelBinders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Xunit;

namespace Knock.UnitTest.Catalog;

public class NeighborhoodAtPointBindingTests
{
    private static readonly Type[] _typed =
    [
        typeof(double), typeof(decimal), typeof(float), typeof(int), typeof(long),
        typeof(double?), typeof(decimal?), typeof(float?), typeof(int?), typeof(long?), typeof(string[]),
    ];

    private static readonly Dictionary<string, string> _queryNames = new(StringComparer.Ordinal) { ["Lat"] = "lat", ["Lon"] = "lon" };

    [Fact]
    public void QueryDtoUsesSingleValueStringsNamedLatAndLon_TC_TT_API_07_15()
    {
        var action = typeof(ColoniasController).GetMethod(nameof(ColoniasController.GetAtPoint))!;
        var parameters = action.GetParameters();
        parameters.Select(p => p.ParameterType).Should().Equal(typeof(NeighborhoodAtPointQuery), typeof(CancellationToken));
        parameters[0].GetCustomAttribute<FromQueryAttribute>().Should().NotBeNull();

        var properties = typeof(NeighborhoodAtPointQuery).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        properties.Select(p => p.Name).Should().BeEquivalentTo("Lat", "Lon");
        properties.Select(p => p.PropertyType).Concat(parameters.Select(p => p.ParameterType)).Intersect(_typed).Should().BeEmpty(
            "un tipo numérico o string[] trae el 400 automático que repite el valor, o un array en el OpenAPI");

        foreach (var property in properties)
        {
            property.PropertyType.Should().Be<string>();
            var binder = property.GetCustomAttribute<SingleValueFromQueryAttribute>();
            binder.Should().NotBeNull($"{property.Name} usa el binder de un solo valor");
            binder.BinderType.Should().Be<SingleValueQueryModelBinder>();
            binder.BindingSource.Should().Be(BindingSource.Query);
            binder.Name.Should().Be(_queryNames[property.Name]);
            property.GetCustomAttributes().OfType<IBindingSourceMetadata>().Should().ContainSingle("una sola fuente de enlace por propiedad");
            property.GetCustomAttribute<ValidateNeverAttribute>().Should().NotBeNull("sin él, MVC valida los atributos del esquema con el 400 automático");
            property.GetCustomAttribute<BindRequiredAttribute>().Should().NotBeNull("el OpenAPI marca el parámetro como obligatorio");
        }
    }
}
