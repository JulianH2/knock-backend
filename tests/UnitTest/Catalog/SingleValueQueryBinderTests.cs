using System.Globalization;
using AwesomeAssertions;
using Knock.Api.Modules.Catalog.Dtos;
using Knock.Api.Modules.Catalog.ModelBinders;
using Knock.Api.Modules.Catalog.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Knock.UnitTest.Catalog;

public class SingleValueQueryBinderTests
{
    /// <summary>Valores recibidos para <c>lat</c>, separados por <c>|</c> (<c>""</c> = ninguno; <c>"|"</c> = dos vacíos).</summary>
    public static TheoryData<string> Values => new() { string.Empty, "20.05", "20.05|99.9", "20.05|20.05", "|", "abc|20.05|-99.34" };

    [Theory]
    [MemberData(nameof(Values))]
    public async Task RepeatedParameter_NeverYieldsAReceivedValue_TC_TT_API_07_15(string values)
    {
        var received = values.Length == 0 ? Array.Empty<string>() : values.Split('|');
        var query = received.Length == 0
            ? new QueryCollection()
            : new QueryCollection(new Dictionary<string, StringValues>(StringComparer.Ordinal) { ["lat"] = new(received) });
        var context = new DefaultModelBindingContext
        {
            ModelName = "lat",
            ModelState = new ModelStateDictionary(),
            ValueProvider = new QueryStringValueProvider(BindingSource.Query, query, CultureInfo.InvariantCulture),
        };

        await new SingleValueQueryModelBinder().BindModelAsync(context);

        context.Result.IsModelSet.Should().BeTrue("el binder siempre devuelve un resultado; si no, [BindRequired] daría el 400 automático");
        context.ModelState.ErrorCount.Should().Be(0, "un error en ModelState daría el 400 automático, que repite el valor");
        var bound = context.Result.Model as string;
        switch (received.Length)
        {
            case 0:
                bound.Should().BeNull();
                break;
            case 1:
                bound.Should().Be(received[0]);
                break;
            default:
                received.Should().NotContain(bound, "con un parámetro repetido nunca se toma el primero ni otro de los recibidos");
                (await new NeighborhoodAtPointQueryValidator()
                    .ValidateAsync(new NeighborhoodAtPointQuery { Lat = bound, Lon = "-99.34" }))
                    .Errors.Should().ContainSingle(error => error.PropertyName == nameof(NeighborhoodAtPointQuery.Lat));
                break;
        }
    }
}
