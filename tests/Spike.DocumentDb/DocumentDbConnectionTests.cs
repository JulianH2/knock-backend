using System.Collections.Concurrent;
using AwesomeAssertions;
using Knock.Spike.DocumentDb.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using Xunit;
using Xunit.Abstractions;

namespace Knock.Spike.DocumentDb;

/// <summary>
/// P7: 8 <see cref="MongoClient"/> independientes (4 servicios × 2 réplicas), cada uno con su pool, con operaciones
/// concurrentes durante <c>SPIKE_P7_SECONDS</c> (spec: al menos 2 min). Después de la carga estable se suben clientes
/// de 8 en 8 hasta el primer rechazo o hasta 64 y se reporta el máximo alcanzado. Se cuentan las conexiones que abren
/// los clientes y, si el servidor lo expone, las que ve <c>serverStatus().connections</c>.
/// </summary>
[Collection(SpikeTargets.MeasurementCollection)]
public sealed class DocumentDbConnectionTests(ITestOutputHelper output)
{
    private const int _services = 4;
    private const int _replicas = 2;
    private const int _workersPerClient = 4;
    private const int _rampStep = 8;
    private const int _rampMax = 64;
    private static readonly TimeSpan _duration = TimeSpan.FromSeconds(SpikeTargets.PositiveIntFromEnvironment("SPIKE_P7_SECONDS", 120));
    private static readonly TimeSpan _rampStepDuration = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _sampleEvery = TimeSpan.FromSeconds(2);

    [Theory]
    [LocalTargets]
    [AzureTarget]
    public async Task EightPooledClientsRunConcurrentlyWithoutErrors_TC_TT_BD_05_07(string target)
    {
        var counter = new ConnectionCounter();
        var clients = new List<MongoClient>();
        await using var spike = await SpikePool.CreateDatabaseAsync(target);
        try
        {
            var before = await ServerConnectionsAsync(spike.Client);
            await AddClientsAsync(target, clients, _services * _replicas, counter);
            var stable = await RunLoadAsync(spike, clients, _duration, counter);
            output.WriteLine($"SPIKE-RESULT P7 [{target}] estable: {clients.Count} clientes × {_workersPerClient} hilos · "
                + $"{_duration.TotalSeconds:0} s · {stable.Operations} operaciones · {stable.Errors.Count} errores · "
                + $"conexiones de los clientes (pico) {stable.PeakClientOpen} · serverStatus.connections.current antes {before}, "
                + $"pico {stable.PeakServer}");
            ReportErrors(target, "estable", stable.Errors);

            var ramp = await RampAsync(target, spike, clients, counter);

            stable.Errors.Should().BeEmpty();
            stable.Operations.Should().BePositive();
            ramp.Should().BeGreaterThanOrEqualTo(_services * _replicas);
        }
        finally
        {
            foreach (var client in clients)
            {
                client.Dispose();
            }

            // Tras el primer rechazo el servidor puede seguir sin conexiones libres (documentdb-local las agota en
            // PostgreSQL): no poder borrar la base es parte de la medición, no un fallo del criterio de P7.
            if (await spike.TryDropAsync() is { } dropFailure)
            {
                output.WriteLine($"SPIKE-RESULT P7 [{target}] limpieza: no se pudo borrar la base tras la rampa "
                    + $"({dropFailure}); el servidor no liberó conexiones en 30 s.");
            }
        }
    }

    /// <summary>Sube de 8 en 8 hasta el primer rechazo o hasta 64; devuelve el máximo de clientes sin errores.</summary>
    private async Task<int> RampAsync(string target, SpikeDatabase spike, List<MongoClient> clients, ConnectionCounter counter)
    {
        var maxWithoutErrors = clients.Count;
        string? rejection = null;
        while (clients.Count < _rampMax && rejection is null)
        {
            await AddClientsAsync(target, clients, _rampStep, counter);
            var step = await RunLoadAsync(spike, clients, _rampStepDuration, counter);
            output.WriteLine($"SPIKE-RESULT P7 [{target}] rampa: {clients.Count} clientes · {step.Operations} operaciones · "
                + $"{step.Errors.Count} errores · conexiones de los clientes (pico) {step.PeakClientOpen} · "
                + $"serverStatus.connections.current (pico) {step.PeakServer}");
            if (step.Errors.IsEmpty)
            {
                maxWithoutErrors = clients.Count;
            }
            else
            {
                rejection = string.Join(", ", step.Errors.Distinct(StringComparer.Ordinal));
            }
        }

        output.WriteLine($"SPIKE-RESULT P7 [{target}] máximo alcanzado sin errores: {maxWithoutErrors} clientes "
            + $"(tope de la prueba {_rampMax}) · primer rechazo: {rejection ?? "ninguno"}");
        return maxWithoutErrors;
    }

    private static async Task AddClientsAsync(string target, List<MongoClient> clients, int count, ConnectionCounter counter)
    {
        for (var i = 0; i < count; i++)
        {
            var n = clients.Count;
            clients.Add(await SpikePool.CreateIndependentClientAsync(
                target,
                $"knock-spike-svc{(n / _replicas % _services) + 1}-r{(n % _replicas) + 1}-c{n + 1}",
                cb => cb
                    .Subscribe<ConnectionOpenedEvent>(_ => counter.Opened())
                    .Subscribe<ConnectionClosedEvent>(_ => counter.Closed())));
        }
    }

    private static async Task<LoadResult> RunLoadAsync(
        SpikeDatabase spike, List<MongoClient> clients, TimeSpan duration, ConnectionCounter counter)
    {
        var dbName = spike.Database.DatabaseNamespace.DatabaseName;
        var errors = new ConcurrentBag<string>();
        var peakClientOpen = 0;
        var peakServer = "no expuesto";
        using var stop = new CancellationTokenSource(duration);
        var workers = new List<Task<long>>();
        foreach (var client in clients)
        {
            // Se pasa la colección (no el cliente, que es IDisposable) a cada hilo de trabajo.
            var collection = client.GetDatabase(dbName).GetCollection<BsonDocument>("carga");
            for (var w = 0; w < _workersPerClient; w++)
            {
                workers.Add(WorkAsync(collection, client.Settings.ApplicationName, errors, stop.Token));
            }
        }

        while (!stop.IsCancellationRequested)
        {
            await Task.Delay(_sampleEvery, CancellationToken.None);
            peakClientOpen = Math.Max(peakClientOpen, counter.Open);
            peakServer = MaxConnections(peakServer, await ServerConnectionsAsync(spike.Client));
        }

        var operations = (await Task.WhenAll(workers)).Sum();
        return new LoadResult(operations, errors, peakClientOpen, peakServer);
    }

    private static async Task<long> WorkAsync(
        IMongoCollection<BsonDocument> collection, string app, ConcurrentBag<string> errors, CancellationToken stop)
    {
        long operations = 0;
        while (!stop.IsCancellationRequested)
        {
            try
            {
                var id = ObjectId.GenerateNewId();
                await collection.InsertOneAsync(new BsonDocument { { "_id", id }, { "app", app } }, cancellationToken: CancellationToken.None);
                await collection.Find(new BsonDocument("_id", id)).FirstOrDefaultAsync(CancellationToken.None);
                operations += 2;
            }
            catch (Exception ex) when (ex is MongoException or TimeoutException)
            {
                // Solo el tipo: el mensaje del driver puede incluir el host del clúster.
                errors.Add(ex.GetType().Name);
            }
        }

        return operations;
    }

    private void ReportErrors(string target, string phase, ConcurrentBag<string> errors)
    {
        foreach (var error in errors.Distinct(StringComparer.Ordinal))
        {
            output.WriteLine($"SPIKE-RESULT P7 [{target}] {phase} error: {error}");
        }
    }

    private static async Task<string> ServerConnectionsAsync(MongoClient client)
    {
        try
        {
            var status = await client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument("serverStatus", 1));
            return status.TryGetValue("connections", out var c) && c is BsonDocument connections
                && connections.TryGetValue("current", out var current)
                ? current.ToString()!
                : "no expuesto";
        }
        catch (Exception ex) when (ex is MongoException or TimeoutException)
        {
            return "no expuesto";
        }
    }

    private static string MaxConnections(string peak, string sample)
    {
        if (!long.TryParse(sample, out var current))
        {
            return peak;
        }

        return long.TryParse(peak, out var previous) && previous >= current ? peak : sample;
    }

    private sealed record LoadResult(long Operations, ConcurrentBag<string> Errors, int PeakClientOpen, string PeakServer);

    /// <summary>Conexiones abiertas por todos los clientes del spike (eventos del driver).</summary>
    private sealed class ConnectionCounter
    {
        private int _opened;
        private int _closed;

        public int Open => Volatile.Read(ref _opened) - Volatile.Read(ref _closed);

        public void Opened() => Interlocked.Increment(ref _opened);

        public void Closed() => Interlocked.Increment(ref _closed);
    }
}
