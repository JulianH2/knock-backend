using AwesomeAssertions;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Validators;
using Microsoft.Extensions.Options;
using Xunit;

namespace Knock.UnitTest.Requests;

public class Spec020QuoteValidatorTests
{
    private static readonly DateOnly _created = new(2026, 11, 12);
    private static QuoteValidationInput Valid() => new("fijo", 1, null, null, _created, "00:00", null, _created);
    private static string[] Errors(QuoteValidationInput input, QuotePricingOptions? options = null) =>
        new QuoteValidator(Options.Create(options ?? new QuotePricingOptions())).Validate(input).Errors.Select(x => x.PropertyName).ToArray();

    [Theory]
    [InlineData("fijo", 1)]
    [InlineData("fijo", 200000)]
    [InlineData("visita", 100)]
    [InlineData("visita", 250)]
    public void AmountBoundariesAreValid_R_13(string type, int amount) =>
        Errors(Valid() with { TipoPrecio = type, Precio = amount }).Should().BeEmpty();

    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    [InlineData(251)]
    [InlineData(200000)]
    public void VisitFeeOutsideConfiguredBoundsIsReportedOnPrecio_R_14(int amount) =>
        Errors(Valid() with { TipoPrecio = "visita", Precio = amount }).Should().Equal("Precio");

    [Fact]
    public void VisitFeeBoundsComeFromConfiguration_R_14()
    {
        var options = new QuotePricingOptions { VisitFeeMin = 50, VisitFeeMax = 400 };

        Errors(Valid() with { TipoPrecio = "visita", Precio = 400 }, options).Should().BeEmpty();
        Errors(Valid() with { TipoPrecio = "visita", Precio = 49 }, options).Should().Equal("Precio");
        Errors(Valid() with { TipoPrecio = "visita", Precio = 401 }, options).Should().Equal("Precio");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    [InlineData(100)]
    [InlineData(250)]
    [InlineData(251)]
    [InlineData(200000)]
    public void AValidatedVisitFeeNeverMakesTheCalculatorThrow_R_14(int amount)
    {
        var options = new QuotePricingOptions();
        var accepted = Errors(Valid() with { TipoPrecio = "visita", Precio = amount }, options).Length == 0;

        var act = () => QuotePricing.Calculate(PriceType.Visita, "ele", amount, null, null, options);
        if (accepted) act.Should().NotThrow();
        else act.Should().Throw<Knock.Market.Api.Common.Errors.DomainException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(200001)]
    [InlineData(1.5)]
    public void InvalidAmountsAreReportedPerField_CA_020_10(double amount)
    {
        var value = (decimal)amount;
        Errors(Valid() with { Precio = value }).Should().Contain("Precio");
        Errors(Valid() with { TipoPrecio = "rango", Precio = null, PrecioMin = value, PrecioMax = 200000 }).Should().Contain("PrecioMin");
        Errors(Valid() with { TipoPrecio = "rango", Precio = null, PrecioMin = 1, PrecioMax = value }).Should().Contain("PrecioMax");
    }

    [Fact]
    public void RangeRequiresBothAmountsAndStrictOrder_CA_020_10()
    {
        var range = Valid() with { TipoPrecio = "rango", Precio = null, PrecioMin = 1, PrecioMax = 200000 };
        Errors(range).Should().BeEmpty();
        Errors(range with { PrecioMin = null }).Should().Contain("PrecioMin");
        Errors(range with { PrecioMax = null }).Should().Contain("PrecioMax");
        Errors(range with { PrecioMin = 2, PrecioMax = 2 }).Should().Contain("PrecioMax");
        Errors(range with { PrecioMin = 3, PrecioMax = 2 }).Should().Contain("PrecioMax");
    }

    [Fact]
    public void RejectsUnexpectedAndMissingFields_R_13()
    {
        Errors(Valid() with { TipoPrecio = "other" }).Should().Contain("TipoPrecio");
        Errors(Valid() with { TipoPrecio = null }).Should().Contain("TipoPrecio");
        Errors(Valid() with { Precio = null }).Should().Contain("Precio");
        Errors(Valid() with { TipoPrecio = "visita", Precio = null }).Should().Contain("Precio");
        Errors(Valid() with { PrecioMin = 1, PrecioMax = 2 }).Should().Contain(["PrecioMin", "PrecioMax"]);
        Errors(Valid() with { TipoPrecio = "visita", PrecioMin = 1, PrecioMax = 2 }).Should().Contain(["PrecioMin", "PrecioMax"]);
        Errors(Valid() with { TipoPrecio = "rango", PrecioMin = 1, PrecioMax = 2 }).Should().Contain("Precio");
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(7, true)]
    [InlineData(8, false)]
    public void DayUsesInclusiveRequestDateBounds_R_15(int offset, bool valid)
    {
        var errors = Errors(Valid() with { Dia = _created.AddDays(offset) });
        if (valid) errors.Should().BeEmpty();
        else errors.Should().Contain("Dia");
    }

    [Theory]
    [InlineData("00:00", true)]
    [InlineData("23:59", true)]
    [InlineData("24:00", false)]
    [InlineData("7:5", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("12:60", false)]
    [InlineData("12:00\n", false)]
    public void HourRequiresExactTwentyFourHourFormat_R_15(string? hour, bool valid)
    {
        var errors = Errors(Valid() with { Hora = hour });
        if (valid) errors.Should().BeEmpty();
        else errors.Should().Contain("Hora");
    }

    [Fact]
    public void DayIsRequiredAndNoteIsOptionalWithMaximumLength_R_15()
    {
        Errors(Valid() with { Dia = null }).Should().Contain("Dia");
        Errors(Valid() with { Nota = new string('a', 300) }).Should().BeEmpty();
        Errors(Valid() with { Nota = new string('a', 301) }).Should().Contain("Nota");
    }
}
