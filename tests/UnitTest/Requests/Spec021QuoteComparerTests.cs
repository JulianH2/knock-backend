using AwesomeAssertions;
using Knock.Market.Api.Modules.Requests.Domain;
using MongoDB.Bson;
using Xunit;

namespace Knock.UnitTest.Requests;

/// <summary>Spec 021, R-04 a R-08: orden, desempates, etiquetas, texto de «qué cubre» y «¿ya puede elegir?». Funciones puras: sin base de datos ni reloj.</summary>
public sealed class Spec021QuoteComparerTests
{
    private static readonly DateTime _t0 = new(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);

    private static QuoteToCompare Quote(int seconds, string type = "fijo", int cap = 1000, double? rating = null, string level = "ninguno", string day = "2026-11-18", string time = "10:00") =>
        new(ObjectId.Parse(seconds.ToString("x24", System.Globalization.CultureInfo.InvariantCulture)), "enviada", type, cap, null, null, cap, false, day, time, null, null, _t0.AddSeconds(seconds), "t" + seconds, "Demo", null, rating, level);

    [Fact]
    public void ByPrice_OrdersByCapAndLeavesVisitsLast_R_04()
    {
        var result = QuoteComparer.Compare([Quote(1, "visita", 250), Quote(2, cap: 1100), Quote(3, cap: 900)], QuoteComparer.ByPrice);

        result.Items.Select(i => i.Quote.Tope).Should().Equal(900, 1100, 250);
        result.Items[0].Etiquetas.Should().Contain(QuoteComparer.BestPriceTag);
        result.Items[2].Etiquetas.Should().NotContain(QuoteComparer.BestPriceTag);
    }

    [Fact]
    public void ByRating_PutsHigherFirstAndUnratedLast_R_04()
    {
        var result = QuoteComparer.Compare([Quote(1), Quote(2, rating: 3.5), Quote(3, rating: 4.8)], QuoteComparer.ByRating);

        result.Items.Select(i => i.Quote.Calificacion).Should().Equal(4.8, 3.5, null);
    }

    [Fact]
    public void Soonest_OrdersByDayAndTime_R_04()
    {
        var result = QuoteComparer.Compare([Quote(1, day: "2026-11-19"), Quote(2, day: "2026-11-18", time: "16:00"), Quote(3, day: "2026-11-18", time: "09:00")], QuoteComparer.Soonest);

        result.Items.Select(i => i.Quote.Id).Should().Equal(Quote(3).Id, Quote(2).Id, Quote(1).Id);
        result.Items[0].Etiquetas.Should().Contain(QuoteComparer.SoonestTag);
    }

    [Fact]
    public void Recommended_VerifiedThenRatingThenCapThenOldest_R_05()
    {
        var result = QuoteComparer.Compare(
            [Quote(1, cap: 800, rating: 5), Quote(2, cap: 900, level: "basica"), Quote(3, cap: 700, level: "basica", rating: 4), Quote(4, cap: 700, level: "basica", rating: 4)],
            QuoteComparer.Recommended);

        result.Items.Select(i => i.Quote.Id).Should().Equal(Quote(3).Id, Quote(4).Id, Quote(2).Id, Quote(1).Id);
    }

    [Theory]
    [InlineData(QuoteComparer.Recommended)]
    [InlineData(QuoteComparer.ByPrice)]
    [InlineData(QuoteComparer.ByRating)]
    [InlineData(QuoteComparer.Soonest)]
    public void EveryTie_IsBrokenByCreationThenId_R_04(string order)
    {
        var quotes = new[] { Quote(3), Quote(1), Quote(2) };

        var result = QuoteComparer.Compare(quotes, order);

        result.Items.Select(i => i.Quote.Id).Should().Equal(Quote(1).Id, Quote(2).Id, Quote(3).Id);
        QuoteComparer.Compare([.. quotes.Reverse()], order).Items.Select(i => i.Quote.Id).Should().Equal(result.Items.Select(i => i.Quote.Id));
    }

    [Fact]
    public void Tags_OnlyAllowedValuesAndAtMostOneBestPrice_R_06()
    {
        var result = QuoteComparer.Compare([Quote(1, cap: 900, level: "completa"), Quote(2, cap: 900), Quote(3, "visita", 100)], QuoteComparer.Recommended);

        result.Items.SelectMany(i => i.Etiquetas).Should().OnlyContain(t => t == "verificado" || t == "mejor_precio" || t == "puede_ir_mas_pronto");
        result.Items.Count(i => i.Etiquetas.Contains(QuoteComparer.BestPriceTag)).Should().Be(1);
        result.Items.Single(i => i.Etiquetas.Contains(QuoteComparer.BestPriceTag)).Quote.EsVisita.Should().BeFalse();
    }

    [Fact]
    public void OnlyVisits_HaveNoBestPrice_R_06()
    {
        var result = QuoteComparer.Compare([Quote(1, "visita", 100)], QuoteComparer.ByPrice);

        result.Items[0].Etiquetas.Should().NotContain(QuoteComparer.BestPriceTag);
    }

    [Theory]
    [InlineData("visita", "Solo visita: ir a ver el trabajo")]
    [InlineData("fijo", "Arreglo completo, mano de obra incluida, refacciones aparte")]
    [InlineData("rango", "Estimado del arreglo dentro del rango")]
    public void Covers_IsAFixedTextPerType_R_07(string type, string text) => QuoteComparer.Covers(type).Should().Be(text);

    [Fact]
    public void MixedTypes_AreFlagged_R_07()
    {
        QuoteComparer.Compare([Quote(1), Quote(2, "visita")], QuoteComparer.Recommended).TiposMixtos.Should().BeTrue();
        QuoteComparer.Compare([Quote(1), Quote(2)], QuoteComparer.Recommended).TiposMixtos.Should().BeFalse();
        QuoteComparer.Compare([], QuoteComparer.Recommended).Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData("cotizando", 1, false, null)]
    [InlineData("cotizando", 0, false, "request_without_quotes")]
    [InlineData("enviada", 0, false, "request_without_quotes")]
    [InlineData("cita_confirmada", 2, false, "request_already_chosen")]
    [InlineData("cancelada", 2, false, "request_cancelled")]
    [InlineData("vencida", 2, false, "request_expired")]
    [InlineData("cotizando", 2, true, "request_expired")]
    public void Block_UsesTheSameNamesAsTheChooseErrors_R_08(string state, int sent, bool expired, string? expected)
    {
        var expires = expired ? _t0.AddSeconds(-1) : _t0.AddHours(1);

        ChoiceRules.Block(RequestStates.ParseRequest(state), expires, _t0, sent).Should().Be(expected);
    }

    [Fact]
    public void Block_TreatsTheExactExpiryInstantAsExpired_R_13() =>
        ChoiceRules.Block(RequestStates.ParseRequest("cotizando"), _t0, _t0, 1).Should().Be("request_expired");
}
