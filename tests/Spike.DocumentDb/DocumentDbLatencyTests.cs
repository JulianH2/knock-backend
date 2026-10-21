using System.Diagnostics;
using AwesomeAssertions;
using Knock.Spike.DocumentDb.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;
using Xunit.Abstractions;

namespace Knock.Spike.DocumentDb;

/// <summary>
/// P3: latencia de «colonia de un punto» ($geoIntersects). Se mide por separado la red (ping) y la consulta:
/// tiempo del servidor con <c>explain executionStats</c> si el destino lo expone; si no, total menos red.
/// El criterio (p95 &lt; 100 ms) es para la consulta. El origen sale de <c>SPIKE_ORIGEN</c>.
/// </summary>
[Collection(SpikeTargets.MeasurementCollection)]
public sealed class DocumentDbLatencyTests(ITestOutputHelper output)
{
    private const int _warmup = 10;
    /// <summary>Consultas medidas (spec P3: al menos 200); configurable con <c>SPIKE_P3_N</c>.</summary>
    private static readonly int _samples = SpikeTargets.PositiveIntFromEnvironment("SPIKE_P3_N", 200);
    private const double _queryP95LimitMs = 100;

    [Theory]
    [LocalTargets]
    [AzureTarget]
    public async Task PointQueryP95UnderOneHundredMilliseconds_TC_TT_BD_05_03(string target)
    {
        await using var spike = await SpikePool.CreateDatabaseAsync(target);
        await SpikeSeeds.CreateColoniasAsync(spike.Database);
        var serverTimed = await ServerMillisAsync(spike.Database, SpikeSeeds.InteriorPoints[0]) is not null;
        for (var i = 0; i < _warmup; i++)
        {
            await MeasureAsync(spike, SpikeSeeds.InteriorPoints[i % SpikeSeeds.InteriorPoints.Count], serverTimed);
        }

        var network = new List<double>(_samples);
        var total = new List<double>(_samples);
        var query = new List<double>(_samples);
        for (var i = 0; i < _samples; i++)
        {
            var sample = await MeasureAsync(spike, SpikeSeeds.InteriorPoints[i % SpikeSeeds.InteriorPoints.Count], serverTimed);
            network.Add(sample.Network);
            total.Add(sample.Total);
            query.Add(sample.Query);
        }

        var method = serverTimed ? "explain executionStats (servidor)" : "total − red (explain sin executionTimeMillis)";
        output.WriteLine($"SPIKE-RESULT P3 [{target}] origen: {SpikeTargets.Origin} · N={_samples} tras {_warmup} de calentamiento · consulta por {method}");
        output.WriteLine($"SPIKE-RESULT P3 [{target}] red      {Summary(network)}");
        output.WriteLine($"SPIKE-RESULT P3 [{target}] total    {Summary(total)}");
        output.WriteLine($"SPIKE-RESULT P3 [{target}] consulta {Summary(query)}");

        Percentile(query, 95).Should().BeLessThan(_queryP95LimitMs);
    }

    private static async Task<(double Network, double Total, double Query)> MeasureAsync(
        SpikeDatabase spike, (string Colonia, double Lng, double Lat) point, bool serverTimed)
    {
        var admin = spike.Client.GetDatabase("admin");
        var clock = Stopwatch.StartNew();
        await admin.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
        var network = clock.Elapsed.TotalMilliseconds;

        clock.Restart();
        var found = await spike.Database.GetCollection<BsonDocument>(SpikeSeeds.Colonias)
            .Find(SpikeQueries.IntersectsPoint(point.Lng, point.Lat)).ToListAsync();
        var total = clock.Elapsed.TotalMilliseconds;
        found.Select(d => d["nombre"].AsString).Should().Equal(point.Colonia);

        var query = serverTimed
            ? await ServerMillisAsync(spike.Database, point) ?? double.NaN
            : Math.Max(0, total - network);
        return (network, total, query);
    }

    private static async Task<double?> ServerMillisAsync(IMongoDatabase db, (string Colonia, double Lng, double Lat) point)
    {
        var explain = await SpikeQueries.ExplainFindAsync(db, SpikeSeeds.Colonias, SpikeQueries.IntersectsPoint(point.Lng, point.Lat));
        var millis = explain is null ? null : SpikeQueries.FindKey(explain, "executionTimeMillis");
        return millis is { IsNumeric: true } ? millis.ToDouble() : null;
    }

    private static string Summary(List<double> values) =>
        $"p50 {Percentile(values, 50):0.0} ms · p95 {Percentile(values, 95):0.0} ms · p99 {Percentile(values, 99):0.0} ms";

    /// <summary>Percentil por rango más cercano.</summary>
    private static double Percentile(List<double> values, int percentile)
    {
        var sorted = values.Order().ToList();
        var rank = (int)Math.Ceiling(percentile / 100.0 * sorted.Count);
        return sorted[Math.Clamp(rank - 1, 0, sorted.Count - 1)];
    }
}
