using AwesomeAssertions;
using Knock.Spike.DocumentDb.Support;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;
using Xunit.Abstractions;

namespace Knock.Spike.DocumentDb;

/// <summary>
/// P9: el mismo set (P2, P4, P5 y P6, más la alternativa de P5) en cada destino disponible. Registra lo que
/// observa cada uno y lista las diferencias de comportamiento; los tiempos se reportan aparte, no cuentan como diferencia.
/// </summary>
public sealed class DocumentDbParityTests(ITestOutputHelper output)
{
    [Fact]
    public async Task LocalAndAzureBehaveTheSame_TC_TT_BD_05_09()
    {
        var targets = SpikeTargets.Available;

        var observed = await Task.WhenAll(targets.Select(ObserveAsync));

        var behaviors = observed[0].Behavior.Keys.ToList();
        var differences = behaviors.Where(b => observed.Select(o => o.Behavior[b]).Distinct(StringComparer.Ordinal).Count() > 1).ToList();
        output.WriteLine($"| Comportamiento | {string.Join(" | ", targets)} |");
        output.WriteLine($"|---|{string.Concat(targets.Select(_ => "---|"))}");
        foreach (var behavior in behaviors)
        {
            var mark = differences.Contains(behavior) ? " **≠**" : string.Empty;
            output.WriteLine($"| {behavior}{mark} | {string.Join(" | ", observed.Select(o => o.Behavior[behavior]))} |");
        }

        foreach (var measurement in observed[0].Measurements.Keys)
        {
            output.WriteLine($"| {measurement} (medición) | {string.Join(" | ", observed.Select(o => o.Measurements.GetValueOrDefault(measurement, "—")))} |");
        }

        output.WriteLine($"Diferencias ({differences.Count}): {string.Join("; ", differences)}");

        targets.Should().HaveCountGreaterThanOrEqualTo(2);
        observed.Should().OnlyContain(o => o.Behavior.Count == behaviors.Count && o.Behavior.Values.All(v => v.Length > 0));
    }

    private static async Task<Observation> ObserveAsync(string target)
    {
        var observation = new Observation();
        // P6 espera al monitor TTL (hasta ~60 s): corre en paralelo con el resto.
        var ttl = ObserveTtlAsync(target, observation);
        await ObserveGeoAsync(target, observation);
        await ObserveTransactionsAsync(target, observation);
        await ObserveTextAsync(target, observation);
        await ttl;
        return observation;
    }

    private static async Task ObserveGeoAsync(string target, Observation o)
    {
        await using var spike = await SpikePool.CreateDatabaseAsync(target);
        var db = spike.Database;
        await o.RecordAsync("P2 índices 2dsphere sobre el seed", async () =>
        {
            await SpikeSeeds.CreateColoniasAsync(db);
            return $"aceptados ({await db.GetCollection<BsonDocument>(SpikeSeeds.Colonias).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)} docs)";
        });
        await o.RecordAsync("P2 $geoIntersects en 8 puntos interiores", async () =>
        {
            var ok = 0;
            foreach (var (colonia, lng, lat) in SpikeSeeds.InteriorPoints)
            {
                ok += (await SpikeQueries.IntersectingAsync(db, lng, lat)).SequenceEqual([colonia]) ? 1 : 0;
            }

            return $"{ok}/{SpikeSeeds.InteriorPoints.Count} correctos";
        });
        await o.RecordAsync("P2 punto en vértice", async () => Names(await SpikeQueries.IntersectingAsync(db, -99.335268, 20.053768)));
        await o.RecordAsync("P2 punto en traslape del seed", async () => Names(await SpikeQueries.IntersectingAsync(db, -99.323016, 20.065535)));
        await o.RecordAsync("P2 fuera de Tula ($geoIntersects / $near)", async () =>
            $"{Names(await SpikeQueries.IntersectingAsync(db, -98.7333, 20.1167))} / "
            + Names((await SpikeQueries.NearAsync(db, -98.7333, 20.1167)).Select(n => n.Nombre)));
        await o.RecordAsync("P2 respaldo $near (primero)", async () =>
            (await SpikeQueries.NearAsync(db, -99.3405, 20.0385)).Select(n => n.Nombre).FirstOrDefault() ?? "vacío");
        await o.RecordAsync("P2 polígono que se cruza", async () =>
        {
            try
            {
                await SpikeQueries.InsertSelfIntersectingPolygonAsync(db);
                return "aceptado";
            }
            catch (MongoException ex)
            {
                return $"rechazado ({ex.GetType().Name})";
            }
        });
    }

    private static async Task ObserveTransactionsAsync(string target, Observation o)
    {
        await using var spike = await SpikePool.CreateDatabaseAsync(target);
        await SpikeQueries.CreateTransactionCollectionsAsync(spike.Database);
        await o.RecordAsync("P4 commit / abort (solicitud, evento)", async () =>
        {
            var (committed, aborted) = await SpikeQueries.CommitAndAbortAsync(spike);
            return $"{committed} / {aborted}";
        });
        await o.RecordAsync("P4 excepción dentro de WithTransaction", async () =>
        {
            var requestId = ObjectId.GenerateNewId();
            using var session = await spike.Client.StartSessionAsync();
            try
            {
                await session.WithTransactionAsync<bool>(async (s, ct) =>
                {
                    await spike.Database.GetCollection<BsonDocument>(SpikeSeeds.Solicitudes)
                        .InsertOneAsync(s, new BsonDocument("_id", requestId), cancellationToken: ct);
                    throw new InvalidOperationException("Falla simulada.");
                });
            }
            catch (InvalidOperationException)
            {
                // Esperada: se compara lo que quedó escrito.
            }

            return (await SpikeQueries.CountRequestAndEventsAsync(spike.Database, requestId)).ToString();
        });
    }

    private static async Task ObserveTextAsync(string target, Observation o)
    {
        await using var spike = await SpikePool.CreateDatabaseAsync(target);
        var db = spike.Database;
        var spanish = await SpikeQueries.OficiosWithTextIndexAsync(db, SpikeSeeds.Oficios, "spanish");
        var none = await SpikeQueries.OficiosWithTextIndexAsync(db, SpikeSeeds.Oficios + "_none", "none");
        var normalized = await SpikeQueries.OficiosWithNormalizedFieldAsync(db, SpikeSeeds.Oficios + "_norm");
        await o.RecordAsync("P5 $text spanish (plomeria, electricidad)", async () =>
            $"{await SpikeQueries.TextAsync(spanish, "plomeria", "spanish") ?? "—"}, {await SpikeQueries.TextAsync(spanish, "electricidad", "spanish") ?? "—"}");
        await o.RecordAsync("P5 $text none (plomeria, electricidad)", async () =>
            $"{await SpikeQueries.TextAsync(none, "plomeria", "none") ?? "—"}, {await SpikeQueries.TextAsync(none, "electricidad", "none") ?? "—"}");
        await o.RecordAsync("P5 colación es fuerza 1 (plomeria, electricidad)", async () =>
            $"{await SpikeQueries.CollationAsync(spanish, "plomeria") ?? "—"}, {await SpikeQueries.CollationAsync(spanish, "electricidad") ?? "—"}");
        await o.RecordAsync("P5 alternativa: campo normalizado + prefijo", async () =>
            $"{Names(await SpikeQueries.NormalizedPrefixAsync(normalized, "plomeria"))}, {Names(await SpikeQueries.NormalizedPrefixAsync(normalized, "electricidad"))}");
    }

    private static async Task ObserveTtlAsync(string target, Observation o)
    {
        await using var spike = await SpikePool.CreateDatabaseAsync(target);
        await o.RecordAsync("P6 ttlMonitorSleepSecs", () => SpikeQueries.ReadTtlMonitorSecondsAsync(spike.Client));
        await o.RecordAsync("P6 TTL borra el vencido y conserva el vigente", async () =>
        {
            var tokens = await SpikeQueries.TokensWithTtlAsync(spike.Database);
            var elapsed = await SpikeQueries.WaitForTtlDeletionAsync(tokens, DocumentDbTtlTests.MaxWait);
            o.Measurements["P6 segundos hasta el borrado"] = $"{elapsed.TotalSeconds:0} s";
            var expired = await tokens.CountDocumentsAsync(new BsonDocument("jti", "vencido"));
            var valid = await tokens.CountDocumentsAsync(new BsonDocument("jti", "vigente"));
            return expired == 0 && valid == 1 ? "sí" : $"no (vencido {expired}, vigente {valid})";
        });
    }

    private static string Names(IEnumerable<string> names)
    {
        var list = names.Order(StringComparer.Ordinal).ToList();
        return list.Count == 0 ? "vacío" : string.Join(" + ", list);
    }

    /// <summary>Lo observado en un destino. Un error se registra como resultado, no detiene la comparación.</summary>
    private sealed class Observation
    {
        private readonly object _gate = new();

        public SortedDictionary<string, string> Behavior { get; } = new(StringComparer.Ordinal);

        public SortedDictionary<string, string> Measurements { get; } = new(StringComparer.Ordinal);

        public async Task RecordAsync(string behavior, Func<Task<string>> probe)
        {
            string result;
            try
            {
                result = await probe();
            }
            catch (Exception ex) when (ex is MongoException or TimeoutException)
            {
                result = $"error {ex.GetType().Name}";
            }

            lock (_gate)
            {
                Behavior[behavior] = result;
            }
        }
    }
}
