using System.Globalization;
using AwesomeAssertions;
using Knock.Api.Modules.Catalog.Dtos;
using Knock.Api.Modules.Catalog.ModelBinders;
using Knock.Api.Modules.Catalog.Validators;
using Xunit;

namespace Knock.UnitTest.Catalog;

public class PointQueryValidatorTests
{
    private const string _validLat = "20.0505";
    private const string _validLon = "-99.3495";

    private static readonly string[] _cultures = ["es-MX", "de-DE", ""];

    private readonly NeighborhoodAtPointQueryValidator _validator = new();

    public static TheoryData<string, string, double> ValidValues => new()
    {
        { "lat", "-90", -90 },
        { "lat", "90", 90 },
        { "lat", "0", 0 },
        { "lat", "-0", 0 },
        { "lat", "20", 20 },
        { "lat", "20.5", 20.5 },
        { "lat", "20.0512347", 20.0512347 },
        { "lat", "-89.1234567891", -89.1234567891 },
        { "lon", "-180", -180 },
        { "lon", "180", 180 },
        { "lon", "0", 0 },
        { "lon", "-99", -99 },
        { "lon", "-99.3456781", -99.3456781 },
        { "lon", "179.9999999999", 179.9999999999 },
    };

    public static TheoryData<string, string?> InvalidValues
    {
        get
        {
            var data = new TheoryData<string, string?>();
            string?[] malformed =
            [
                null, "", " ", "abc", "20,05", "1,000", "1e1", "0x10", "+20.05", "NaN", "Infinity", "-∞", " 20.05", "20.05 ", "20.",
                ".5", "20.05\n", "٢٠.٠٥", "1234.5", "20.12345678901", "-20.0512347000000001", SingleValueQueryModelBinder.RepeatedValue,
            ];
            foreach (var parameter in new[] { "lat", "lon" })
            {
                foreach (var value in malformed)
                {
                    data.Add(parameter, value);
                }
            }

            data.Add("lat", "90.0000001");
            data.Add("lat", "-91");
            data.Add("lat", "180");
            data.Add("lon", "180.5");
            data.Add("lon", "-180.0000001");
            data.Add("lon", "-181");
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(ValidValues))]
    public void ValidCoordinates_AreAcceptedAndParsedWithInvariantCulture_TC_TT_API_07_01(string field, string value, double expected)
    {
        var original = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        try
        {
            foreach (var name in _cultures)
            {
                CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);

                _validator.Validate(Query(field, value)).IsValid.Should().BeTrue($"{field}={value} es válido en la cultura '{name}'");
                NeighborhoodAtPointQueryValidator.Parse(value).Should().Be(expected, $"la cultura '{name}' no cambia el valor");
            }
        }
        finally
        {
            (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = original;
        }
    }

    [Theory]
    [MemberData(nameof(InvalidValues))]
    public void MissingOutOfRangeOrMalformedCoordinates_AreRejectedWithoutEchoingTheValue_TC_TT_API_07_02(string field, string? value)
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            // es-MX y de-DE leen «20,05» y «1,000» como números; el validador no debe depender de eso.
            foreach (var name in _cultures)
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
                var result = _validator.Validate(Query(field, value));

                result.IsValid.Should().BeFalse($"{field}='{value}' se rechaza en la cultura '{name}'");
                result.Errors.Should().OnlyContain(error => string.Equals(error.PropertyName, field, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(value))
                {
                    result.Errors.Should().NotContain(error => error.ErrorMessage.Contains(value.Trim(), StringComparison.Ordinal));
                }
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    private static NeighborhoodAtPointQuery Query(string field, string? value) => field == "lat"
        ? new NeighborhoodAtPointQuery { Lat = value, Lon = _validLon }
        : new NeighborhoodAtPointQuery { Lat = _validLat, Lon = value };
}
