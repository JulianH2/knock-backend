using System.Diagnostics;
using System.Globalization;
using Knock.Market.Api.Modules.Requests.Domain;
using Xunit;

namespace Knock.UnitTest.Requests;

public sealed class Spec028SeleccionTests
{
    private static readonly OleadasOptions _options = new();

    [Fact]
    public void Compone_Oleada_Tres_Uno_Uno_CA_028_01()
    {
        var candidates = Candidates();
        var result = SeleccionDeOleada.Select(candidates, [], _options);
        Assert.Equal(new[] { ("01", "mejor_ordenado"), ("02", "mejor_ordenado"), ("03", "mejor_ordenado"), ("05", "nuevo"), ("09", "cercano") }, result.Select(x => (x.TechnicianId, x.Reason)));
    }

    [Fact]
    public void Excluye_Envios_Previos_CA_028_04()
    {
        var result = SeleccionDeOleada.Select(Candidates(), ["01", "05", "09"], _options);
        Assert.DoesNotContain(result, x => x.TechnicianId is "01" or "05" or "09");
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public void Completa_Con_Relleno_CA_028_07()
    {
        var candidates = Candidates().Select(x => x with { CompletedJobs = 3 }).ToArray();
        var result = SeleccionDeOleada.Select(candidates, [], _options);
        Assert.Equal("mejor_ordenado,mejor_ordenado,mejor_ordenado,cercano,relleno", string.Join(',', result.Select(x => x.Reason)));
        Assert.Equal("09", result[3].TechnicianId);
        Assert.Equal("04", result[4].TechnicianId);
    }

    [Fact]
    public void SameInputAndDifferentOrderAreDeterministic_CA_028_08()
    {
        var candidates = Candidates();
        var expected = SeleccionDeOleada.Select(candidates, [], _options);
        Assert.Equal(expected, SeleccionDeOleada.Select(candidates, [], _options));
        Assert.Equal(expected, SeleccionDeOleada.Select(candidates.Reverse(), [], _options));
    }

    [Fact]
    [Trait("Category", "Performance")]
    public void TwoHundredCandidatesFinishUnder200Milliseconds_CA_028_08()
    {
        var candidates = Enumerable.Range(0, 200).Select(i => Candidate(i.ToString("D3", CultureInfo.InvariantCulture), 5, 4, i)).ToArray();
        _ = SeleccionDeOleada.Select(candidates, [], _options);
        var watch = Stopwatch.StartNew();
        var result = SeleccionDeOleada.Select(candidates, [], _options);
        watch.Stop();
        Assert.Equal(5, result.Count);
        Assert.True(watch.Elapsed < TimeSpan.FromMilliseconds(200), $"Elapsed: {watch.Elapsed}");
    }

    [Fact]
    public void NoCandidates_ReturnsEmpty_CA_028_08() => Assert.Empty(SeleccionDeOleada.Select([], [], _options));

    [Fact]
    public void FewerThanFive_ReturnsOnlyAvailableCandidates_CA_028_08()
    {
        var result = SeleccionDeOleada.Select(Candidates().Take(2), [], _options);
        Assert.Equal("01,02", string.Join(',', result.Select(x => x.TechnicianId)));
    }

    [Fact]
    public void EqualRatings_OrderByJobsThenOrdinalId_CA_028_08()
    {
        var candidates = new[] { Candidate("b", 4, 7, null), Candidate("a", 4, 7, null), Candidate("c", 4, 8, null), Candidate("d", 5, 3, null) };
        var result = SeleccionDeOleada.Select(candidates, [], _options);
        Assert.Equal("d,c,a,b", string.Join(',', result.Select(x => x.TechnicianId)));
    }

    [Fact]
    public void MissingLocations_FillsNearestSlot_CA_028_07()
    {
        var candidates = Candidates().Select(x => x with { DistanceKm = null, CompletedJobs = 3 }).ToArray();
        var result = SeleccionDeOleada.Select(candidates, [], _options);
        Assert.Equal("mejor_ordenado,mejor_ordenado,mejor_ordenado,relleno,relleno", string.Join(',', result.Select(x => x.Reason)));
        Assert.Equal("01,02,03,04,05", string.Join(',', result.Select(x => x.TechnicianId)));
    }

    [Fact]
    public void EqualDistances_UseRankingTieBreak_CA_028_08()
    {
        var candidates = Candidates().Select(x => x with { DistanceKm = 1, CompletedJobs = 3 }).ToArray();
        var result = SeleccionDeOleada.Select(candidates.Reverse(), [], _options);
        Assert.Equal(("04", "cercano"), (result[3].TechnicianId, result[3].Reason));
    }

    [Fact]
    public void CustomSizeAndNewThreshold_AreRespected_CA_028_08()
    {
        var options = new OleadasOptions { TamanoOleada = 4, MejorOrdenados = 2, UmbralNuevo = 5 };
        var result = SeleccionDeOleada.Select(Candidates(), [], options);
        Assert.Equal(4, result.Count);
        Assert.Equal("mejor_ordenado,mejor_ordenado,nuevo,cercano", string.Join(',', result.Select(x => x.Reason)));
        Assert.Equal("03", result[2].TechnicianId);
    }

    [Fact]
    public void InvalidDistances_AreNotNearest_CA_028_07()
    {
        var candidates = Candidates().Select(x => x with
        {
            CompletedJobs = 4,
            DistanceKm = x.TechnicianId switch
            {
                "04" => double.NaN,
                "05" => -1,
                "06" => double.PositiveInfinity,
                "07" => 0,
                _ => null
            }
        }).ToArray();
        var result = SeleccionDeOleada.Select(candidates, [], _options);
        Assert.Equal(("07", "cercano"), (result[3].TechnicianId, result[3].Reason));
        var invalidOnly = candidates.Select(x => x.TechnicianId == "07" ? x with { DistanceKm = null } : x);
        Assert.DoesNotContain(SeleccionDeOleada.Select(invalidOnly, [], _options), x => x.Reason == "cercano");
    }

    [Fact]
    public void DuplicateTechnicians_DoNotConsumeRankedSlots_CA_028_01()
    {
        var candidates = Candidates();
        var repeated = candidates.Concat([candidates[0] with { DistanceKm = 0.01 }, candidates[1]]).ToArray();
        var result = SeleccionDeOleada.Select(repeated, [], _options);
        Assert.Equal("01,02,03", string.Join(',', result.Take(3).Select(x => x.TechnicianId)));
        Assert.Equal(5, result.Select(x => x.TechnicianId).Distinct().Count());
        Assert.Equal(result, SeleccionDeOleada.Select(repeated.Reverse(), [], _options));
    }

    private static WaveCandidate[] Candidates() => Enumerable.Range(1, 10)
        .Select(i => Candidate(i.ToString("D2", CultureInfo.InvariantCulture), 11 - i, i == 5 ? 2 : 4, i == 9 ? 0.1 : i)).ToArray();

    private static WaveCandidate Candidate(string id, decimal rating, int jobs, double? distance) => new(id, rating, jobs, distance);
}
