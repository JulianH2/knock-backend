using System.Diagnostics;
using AwesomeAssertions;
using Knock.Api.Modules.Catalog.Services;
using Knock.DatabaseTest.Support;
using MongoDB.Bson;
using Xunit;
using Xunit.Abstractions;

namespace Knock.DatabaseTest.Catalog;

public sealed class NeighborhoodLookupPerformanceTests(ITestOutputHelper output)
{
    private const int _warmup = 10;
    private const int _samples = 200;
    private const double _serverP95LimitMs = 100;

    /// <summary>Campos de sesión y de enrutamiento que el driver agrega al comando y que <c>explain</c> no acepta dentro.</summary>
    private static readonly string[] _sessionFields = ["lsid", "$db", "$clusterTime", "$readPreference", "txnNumber", "apiVersion"];

    private static readonly Lazy<Task<CatalogDatabase>> _shared = CatalogDatabase.SharedSeeded();

    [Fact]
    public async Task PointLookupServerP95UnderOneHundredMilliseconds_TC_TT_BD_06_12()
    {
        var interior = await MeasureAsync(CatalogSeedData.InteriorPoints.Select(p => (p.Longitude, p.Latitude)).ToList(), "$geoIntersects");
        var fallback = await MeasureAsync([CatalogSeedData.NearCenterOutsidePolygons], "$near");

        Report("interior ($geoIntersects)", interior);
        Report("respaldo ($geoIntersects vacío + $near)", fallback);

        Percentile(interior.Server, 95).Should().BeLessThan(_serverP95LimitMs);
        Percentile(fallback.Server, 95).Should().BeLessThan(_serverP95LimitMs);
    }

    /// <summary>
    /// Por muestra: tiempo total de la consulta interna (informativo) y, con <c>explain</c> de cada <c>find</c> que envió el driver,
    /// el tiempo en el servidor de la consulta que lleva <paramref name="operator"/>.
    /// </summary>
    private static async Task<(List<double> Server, List<double> Total)> MeasureAsync(
        List<(double Longitude, double Latitude)> points,
        string @operator)
    {
        var database = await _shared.Value;
        var locator = database.Get<NeighborhoodLocator>();
        var server = new List<double>();
        var total = new List<double>();
        for (var i = 0; i < _warmup + _samples; i++)
        {
            var (longitude, latitude) = points[i % points.Count];
            database.ClearCommands();
            var watch = Stopwatch.StartNew();
            await locator.FindAtAsync(latitude, longitude, CancellationToken.None);
            watch.Stop();
            var find = database.FindCommands.Single(command => command.ToJson().Contains(@operator, StringComparison.Ordinal));
            var serverMillis = await ServerMillisAsync(database, find);
            if (i >= _warmup)
            {
                total.Add(watch.Elapsed.TotalMilliseconds);
                server.Add(serverMillis);
            }
        }

        return (server, total);
    }

    /// <summary>Lee <c>executionTimeMillis</c> a cualquier profundidad: documentdb-local usa otro formato de <c>explain</c> (P9).</summary>
    private static async Task<double> ServerMillisAsync(CatalogDatabase database, BsonDocument find)
    {
        // El driver entrega el comando como RawBsonDocument (inmutable): se copia elemento por elemento sin los de sesión.
        var command = new BsonDocument(find.Elements.Where(element => !_sessionFields.Contains(element.Name)));

        var explain = await database.Database.RunCommandAsync<BsonDocument>(new BsonDocument
        {
            { "explain", command },
            { "verbosity", "executionStats" },
        });
        var millis = FindKey(explain, "executionTimeMillis");
        return millis?.ToDouble()
            ?? throw new InvalidOperationException("explain con executionStats no informó executionTimeMillis.");
    }

    private void Report(string query, (List<double> Server, List<double> Total) times)
    {
        output.WriteLine($"TC-TT-BD-06-12 {query} · N={_samples} tras {_warmup} de calentamiento");
        output.WriteLine($"  consulta en servidor: {Summary(times.Server)}");
        output.WriteLine($"  total ida y vuelta (informativo): {Summary(times.Total)}");
    }

    private static string Summary(List<double> values) =>
        $"p50 {Percentile(values, 50):0.00} ms · p95 {Percentile(values, 95):0.00} ms · p99 {Percentile(values, 99):0.00} ms";

    /// <summary>Percentil por rango más cercano.</summary>
    private static double Percentile(List<double> values, int percentile)
    {
        var sorted = values.Order().ToList();
        var rank = (int)Math.Ceiling(percentile / 100.0 * sorted.Count);
        return sorted[Math.Clamp(rank - 1, 0, sorted.Count - 1)];
    }

    private static BsonValue? FindKey(BsonValue value, string key) => value switch
    {
        BsonDocument document when document.TryGetValue(key, out var found) => found,
        BsonDocument document => document.Values.Select(v => FindKey(v, key)).FirstOrDefault(v => v is not null),
        BsonArray array => array.Select(v => FindKey(v, key)).FirstOrDefault(v => v is not null),
        _ => null,
    };
}
