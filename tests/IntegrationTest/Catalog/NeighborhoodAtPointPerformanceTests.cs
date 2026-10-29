using System.Diagnostics;
using System.Globalization;
using System.Net;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Xunit;
using Xunit.Abstractions;

namespace Knock.IntegrationTest.Catalog;

public sealed class NeighborhoodAtPointPerformanceTests(ITestOutputHelper output)
{
    private const int _warmup = 20;
    private const int _measured = 200;
    private const double _budgetMs = 150;
    private const double _ciCeilingMs = 500;

    [Fact]
    [Trait("Category", "Performance")]
    public async Task EndpointP95ReportedAgainstBudgetAndFailsOnClearDeviation_TC_TT_API_07_14()
    {
        var (factory, databaseName) = await PointLookupHttp.CreateSeededFactoryAsync((await DocumentDbFixture.SharedAsync()).ConnectionString);
        using var _ = factory;
        using var client = factory.CreateClient();
        var token = TestHttp.IssueToken(factory, Guid.NewGuid());

        // Mitad con punto interior y mitad con respaldo por centro o fuera de cobertura; en secuencia (documentdb-local, spike P7).
        var interior = PointLookupHttp.PilotInteriorPoints.Select(p => PointLookupHttp.Query(p.Lat, p.Lon)).ToList();
        var other = PointLookupHttp.OutOfCoverage.Append(PointLookupHttp.NearCenter).Select(p => PointLookupHttp.Query(p.Lat, p.Lon)).ToList();
        string QueryFor(int i) => i % 2 == 0 ? interior[i / 2 % interior.Count] : other[i / 2 % other.Count];

        for (var i = 0; i < _warmup; i++)
        {
            using var response = await TestHttp.GetAsync(client, PointLookupHttp.Path(QueryFor(i)), bearerToken: token);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var samples = new List<double>(_measured);
        for (var i = 0; i < _measured; i++)
        {
            var started = Stopwatch.GetTimestamp();
            using var response = await TestHttp.GetAsync(client, PointLookupHttp.Path(QueryFor(i)), bearerToken: token);
            samples.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        samples.Sort();
        double Percentile(double p) => samples[Math.Clamp((int)Math.Ceiling(p * samples.Count) - 1, 0, samples.Count - 1)];
        var p95 = Percentile(0.95);
        var strict = Environment.GetEnvironmentVariable("KNOCK_PERF_STRICT") == "1";
        output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"GET /api/v1/colonias/en-punto en proceso ({_measured} peticiones): p50={Percentile(0.50):0.0} ms p95={p95:0.0} ms p99={Percentile(0.99):0.0} ms · meta p95 < {_budgetMs} ms · falla si p95 > {(strict ? _budgetMs : _ciCeilingMs)} ms"));

        p95.Should().BeLessThanOrEqualTo(strict ? _budgetMs : _ciCeilingMs, "solo una desviación clara hace fallar la prueba (decisión (e))");
        await factory.Services.GetRequiredService<IMongoClient>().DropDatabaseAsync(databaseName);
    }
}
