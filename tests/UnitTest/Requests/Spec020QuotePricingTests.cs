using AwesomeAssertions;
using Knock.Market.Api.Common.Errors;
using Knock.Market.Api.Modules.Requests.Domain;
using Xunit;

namespace Knock.UnitTest.Requests;

public class Spec020TopeTests
{
    private static readonly QuotePricingOptions _options = new();

    [Theory]
    [InlineData("ele", 1000, 1100, true)]
    [InlineData("plo", 50, 55, false)]
    public void Calcula_Tope_CA_020_11(string oficio, int price, int expected, bool approval)
    {
        var cap = QuotePricing.Calculate(PriceType.Fijo, oficio, price, null, null, _options);
        cap.Tope.Should().Be(expected);
        cap.RequiereAprobacion.Should().Be(approval);
    }

    [Fact]
    public void FixedPriceRoundsTheCapUp_R_14()
    {
        QuotePricing.Calculate(PriceType.Fijo, "ele", 15, null, null, _options).Tope.Should().Be(17);
    }

    [Fact]
    public void ApprovalIsRequiredExactlyWhenTheMarginReaches100Pesos_R_14()
    {
        // 990 -> tope 1089 (margen 99); 991 -> tope 1091 (margen 100), porque el tope se redondea hacia arriba.
        QuotePricing.Calculate(PriceType.Fijo, "ele", 990, null, null, _options).RequiereAprobacion.Should().BeFalse();
        QuotePricing.Calculate(PriceType.Fijo, "ele", 991, null, null, _options).RequiereAprobacion.Should().BeTrue();
    }

    [Theory]
    [InlineData("ele", 1000, 1200)]
    [InlineData("plo", 1000, 1250)]
    [InlineData("mec", 1000, 1250)]
    public void RangeCapAddsTwentyPercentForElectricityAndTwentyFiveForTheRest_R_14(string oficio, int max, int tope)
    {
        var cap = QuotePricing.Calculate(PriceType.Rango, oficio, null, 500, max, _options);

        cap.Tope.Should().Be(tope);
        cap.RequiereAprobacion.Should().BeTrue();
    }

    [Fact]
    public void VisitCapEqualsTheFeeAndNeverRequiresApproval_R_14()
    {
        var cap = QuotePricing.Calculate(PriceType.Visita, "mec", 200, null, null, _options);

        cap.Tope.Should().Be(200);
        cap.RequiereAprobacion.Should().BeFalse();
    }

    [Theory]
    [InlineData(99)]
    [InlineData(251)]
    public void VisitFeeOutsideTheConfiguredBoundsIsRejected_R_14(int fee)
    {
        var act = () => QuotePricing.Calculate(PriceType.Visita, "ele", fee, null, null, _options);

        act.Should().Throw<DomainException>().Which.Kind.Should().Be(DomainErrorKind.Unprocessable);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(250)]
    public void VisitFeeAtTheBoundsIsAccepted_R_14(int fee)
    {
        QuotePricing.Calculate(PriceType.Visita, "ele", fee, null, null, _options).Tope.Should().Be(fee);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(200_001)]
    public void FixedAmountOutsideOneToTwoHundredThousandIsRejected_R_13(int price)
    {
        var act = () => QuotePricing.Calculate(PriceType.Fijo, "ele", price, null, null, _options);

        act.Should().Throw<DomainException>().Which.Kind.Should().Be(DomainErrorKind.Unprocessable);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(200_000)]
    public void FixedAmountAtTheBoundsIsAccepted_R_13(int price)
    {
        QuotePricing.Calculate(PriceType.Fijo, "ele", price, null, null, _options).Tope.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(500, 500)]
    [InlineData(600, 500)]
    [InlineData(0, 500)]
    [InlineData(500, 200_001)]
    [InlineData(-1, 500)]
    [InlineData(1, 0)]
    public void RangeRequiresMinBelowMaxBothInsideTheBounds_R_13(int min, int max)
    {
        var act = () => QuotePricing.Calculate(PriceType.Rango, "ele", null, min, max, _options);

        act.Should().Throw<DomainException>().Which.Kind.Should().Be(DomainErrorKind.Unprocessable);
    }

    [Fact]
    public void FixedPriceRequiresThePriceAndRangeRequiresMinAndMax_R_13()
    {
        var fixedWithout = () => QuotePricing.Calculate(PriceType.Fijo, "ele", null, null, null, _options);
        var rangeWithout = () => QuotePricing.Calculate(PriceType.Rango, "ele", null, 100, null, _options);

        fixedWithout.Should().Throw<DomainException>();
        rangeWithout.Should().Throw<DomainException>();
    }

    [Fact]
    public void UnknownOficioIsRejected()
    {
        var act = () => QuotePricing.Calculate(PriceType.Rango, "xyz", null, 100, 200, _options);

        act.Should().Throw<DomainException>().Which.Kind.Should().Be(DomainErrorKind.Unprocessable);
    }
}
