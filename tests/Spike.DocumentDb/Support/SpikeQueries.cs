using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;

namespace Knock.Spike.DocumentDb.Support;

/// <summary>Consultas del spike compartidas por las pruebas de cada pregunta y por la paridad (P9).</summary>
internal static class SpikeQueries
{
    public const double FallbackMaxDistanceMeters = 2000;
    public const string NormalizedField = "nombreBusqueda";

    private static readonly Collation _spanishPrimary = new("es", strength: CollationStrength.Primary);

    // P2 ------------------------------------------------------------------------------------------------

    public static FilterDefinition<BsonDocument> IntersectsPoint(double lng, double lat) =>
        Builders<BsonDocument>.Filter.GeoIntersects("geom", GeoJson.Point(GeoJson.Geographic(lng, lat)));

    public static async Task<List<string>> IntersectingAsync(IMongoDatabase db, double lng, double lat)
    {
        var docs = await db.GetCollection<BsonDocument>(SpikeSeeds.Colonias).Find(IntersectsPoint(lng, lat)).ToListAsync();
        return docs.Select(d => d["nombre"].AsString).ToList();
    }

    public static async Task<List<(string Nombre, double Meters)>> NearAsync(IMongoDatabase db, double lng, double lat)
    {
        var filter = Builders<BsonDocument>.Filter.Near(
            "centro", GeoJson.Point(GeoJson.Geographic(lng, lat)), maxDistance: FallbackMaxDistanceMeters);
        var docs = await db.GetCollection<BsonDocument>(SpikeSeeds.Colonias).Find(filter).ToListAsync();
        return docs.Select(d =>
        {
            var c = d["centro"]["coordinates"].AsBsonArray;
            return (d["nombre"].AsString, HaversineMeters(lng, lat, c[0].ToDouble(), c[1].ToDouble()));
        }).ToList();
    }

    /// <summary>Inserta el polígono «moño» sintético (aristas que se cruzan).</summary>
    public static Task InsertSelfIntersectingPolygonAsync(IMongoDatabase db)
    {
        var bowTie = BsonDocument.Parse(
            "{ type: 'Polygon', coordinates: [[[-99.33, 20.05], [-99.32, 20.06], [-99.32, 20.05], [-99.33, 20.06], [-99.33, 20.05]]] }");
        return db.GetCollection<BsonDocument>(SpikeSeeds.Colonias)
            .InsertOneAsync(new BsonDocument { { "nombre", "Inválida" }, { "geom", bowTie } });
    }

    // P4 ------------------------------------------------------------------------------------------------

    /// <summary>Las colecciones se crean antes, como lo harán los scripts de índices del producto.</summary>
    public static async Task CreateTransactionCollectionsAsync(IMongoDatabase db)
    {
        await db.CreateCollectionAsync(SpikeSeeds.Solicitudes);
        await db.CreateCollectionAsync(SpikeSeeds.Outbox);
    }

    public static async Task InsertRequestAndEventAsync(IMongoDatabase db, IClientSessionHandle session, ObjectId requestId)
    {
        await db.GetCollection<BsonDocument>(SpikeSeeds.Solicitudes)
            .InsertOneAsync(session, new BsonDocument { { "_id", requestId }, { "estado", "Creada" } });
        await db.GetCollection<BsonDocument>(SpikeSeeds.Outbox)
            .InsertOneAsync(session, new BsonDocument { { "tipo", "SolicitudCreada" }, { "solicitudId", requestId } });
    }

    /// <summary>Una transacción con commit y otra con abort; devuelve los conteos (solicitud, evento) de cada una.</summary>
    public static async Task<((long, long) Committed, (long, long) Aborted)> CommitAndAbortAsync(SpikeDatabase spike)
    {
        var committed = ObjectId.GenerateNewId();
        var aborted = ObjectId.GenerateNewId();

        using (var session = await spike.Client.StartSessionAsync())
        {
            session.StartTransaction();
            await InsertRequestAndEventAsync(spike.Database, session, committed);
            await session.CommitTransactionAsync();
        }

        using (var session = await spike.Client.StartSessionAsync())
        {
            session.StartTransaction();
            await InsertRequestAndEventAsync(spike.Database, session, aborted);
            await session.AbortTransactionAsync();
        }

        return (await CountRequestAndEventsAsync(spike.Database, committed), await CountRequestAndEventsAsync(spike.Database, aborted));
    }

    public static async Task<(long Requests, long Events)> CountRequestAndEventsAsync(IMongoDatabase db, ObjectId requestId)
    {
        var requests = await db.GetCollection<BsonDocument>(SpikeSeeds.Solicitudes)
            .CountDocumentsAsync(new BsonDocument("_id", requestId));
        var events = await db.GetCollection<BsonDocument>(SpikeSeeds.Outbox)
            .CountDocumentsAsync(new BsonDocument("solicitudId", requestId));
        return (requests, events);
    }

    // P5 ------------------------------------------------------------------------------------------------

    public static async Task<IMongoCollection<BsonDocument>> OficiosWithTextIndexAsync(
        IMongoDatabase db, string name, string language)
    {
        var oficios = db.GetCollection<BsonDocument>(name);
        await oficios.InsertManyAsync(SpikeSeeds.LoadOficios());
        await oficios.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Text("nombre"),
            new CreateIndexOptions { DefaultLanguage = language }));
        return oficios;
    }

    public static Task<string?> TextAsync(IMongoCollection<BsonDocument> oficios, string term, string language) =>
        FirstNameOrErrorAsync(() => oficios.Find(Builders<BsonDocument>.Filter.Text(term, language)).FirstOrDefaultAsync());

    public static Task<string?> CollationAsync(IMongoCollection<BsonDocument> oficios, string term) =>
        FirstNameOrErrorAsync(() => oficios
            .Find(Builders<BsonDocument>.Filter.Eq("nombre", term), new FindOptions { Collation = _spanishPrimary })
            .FirstOrDefaultAsync());

    /// <summary>
    /// Alternativa a P5 (hallazgo, no cambia el criterio): sin acentos ni mayúsculas, calculado en la app.
    /// Quita las marcas diacríticas (la «ñ» queda como «N»).
    /// </summary>
    public static string Normalize(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
        {
            builder.Append(c);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }

    /// <summary>Oficios con el campo normalizado y un índice ascendente normal sobre él.</summary>
    public static async Task<IMongoCollection<BsonDocument>> OficiosWithNormalizedFieldAsync(IMongoDatabase db, string name)
    {
        var oficios = db.GetCollection<BsonDocument>(name);
        var docs = SpikeSeeds.LoadOficios();
        foreach (var doc in docs)
        {
            doc[NormalizedField] = Normalize(doc["nombre"].AsString);
        }

        await oficios.InsertManyAsync(docs);
        await oficios.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending(NormalizedField)));
        return oficios;
    }

    /// <summary>Búsqueda por prefijo anclado (<c>^</c>) sobre el campo normalizado, que puede usar el índice.</summary>
    public static FilterDefinition<BsonDocument> NormalizedPrefix(string term) =>
        Builders<BsonDocument>.Filter.Regex(NormalizedField, new BsonRegularExpression("^" + Regex.Escape(Normalize(term))));

    public static async Task<List<string>> NormalizedPrefixAsync(IMongoCollection<BsonDocument> oficios, string term)
    {
        var docs = await oficios.Find(NormalizedPrefix(term)).ToListAsync();
        return docs.Select(d => d["nombre"].AsString).ToList();
    }

    // P6 ------------------------------------------------------------------------------------------------

    /// <summary>Índice TTL sobre <c>expiraEn</c> con un token vencido y uno vigente.</summary>
    public static async Task<IMongoCollection<BsonDocument>> TokensWithTtlAsync(IMongoDatabase db)
    {
        var tokens = db.GetCollection<BsonDocument>(SpikeSeeds.TokensRevocados);
        await tokens.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("expiraEn"),
            new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }));
        var now = DateTime.UtcNow;
        await tokens.InsertManyAsync(
        [
            new BsonDocument { { "jti", "vencido" }, { "expiraEn", now.AddSeconds(-1) } },
            new BsonDocument { { "jti", "vigente" }, { "expiraEn", now.AddHours(1) } },
        ]);
        return tokens;
    }

    /// <summary>Espera acotada hasta que el monitor TTL borre el token vencido; devuelve el tiempo real.</summary>
    public static async Task<TimeSpan> WaitForTtlDeletionAsync(IMongoCollection<BsonDocument> tokens, TimeSpan maxWait)
    {
        var elapsed = Stopwatch.StartNew();
        while (elapsed.Elapsed < maxWait && await tokens.CountDocumentsAsync(new BsonDocument("jti", "vencido")) > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        return elapsed.Elapsed;
    }

    public static async Task<string> ReadTtlMonitorSecondsAsync(MongoClient client)
    {
        try
        {
            var result = await client.GetDatabase("admin").RunCommandAsync<BsonDocument>(
                new BsonDocument { { "getParameter", 1 }, { "ttlMonitorSleepSecs", 1 } });
            return result.GetValue("ttlMonitorSleepSecs", "no expuesto").ToString()!;
        }
        catch (MongoCommandException)
        {
            return "no expuesto";
        }
    }

    // Comunes -------------------------------------------------------------------------------------------

    /// <summary>Busca una clave a cualquier profundidad (los planes de explain cambian de forma entre motores).</summary>
    public static BsonValue? FindKey(BsonValue value, string key) => value switch
    {
        BsonDocument doc when doc.TryGetValue(key, out var found) => found,
        BsonDocument doc => doc.Values.Select(v => FindKey(v, key)).FirstOrDefault(v => v is not null),
        BsonArray array => array.Select(v => FindKey(v, key)).FirstOrDefault(v => v is not null),
        _ => null,
    };

    public static async Task<BsonDocument?> ExplainFindAsync(IMongoDatabase db, string collection, FilterDefinition<BsonDocument> filter)
    {
        var rendered = filter.Render(new RenderArgs<BsonDocument>(
            db.GetCollection<BsonDocument>(collection).DocumentSerializer, BsonSerializer.SerializerRegistry));
        try
        {
            return await db.RunCommandAsync<BsonDocument>(new BsonDocument
            {
                { "explain", new BsonDocument { { "find", collection }, { "filter", rendered } } },
                { "verbosity", "executionStats" },
            });
        }
        catch (MongoCommandException)
        {
            return null;
        }
    }

    private static async Task<string?> FirstNameOrErrorAsync(Func<Task<BsonDocument>> find)
    {
        try
        {
            var doc = await find();
            return doc?["nombre"].AsString;
        }
        catch (MongoException ex)
        {
            return $"error {ex.GetType().Name}";
        }
    }

    private static double HaversineMeters(double lng1, double lat1, double lng2, double lat2)
    {
        const double earthRadius = 6_371_008.8;
        static double Rad(double d) => d * Math.PI / 180;
        var a = Math.Pow(Math.Sin(Rad(lat2 - lat1) / 2), 2)
            + (Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Pow(Math.Sin(Rad(lng2 - lng1) / 2), 2));
        return 2 * earthRadius * Math.Asin(Math.Sqrt(a));
    }
}
