using AwesomeAssertions;
using Knock.DatabaseTest.Support;
using Knock.IntegrationTest.Support;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Catalog.Data;
using Knock.Market.Api.Modules.Catalog.Domain;
using Knock.Market.Api.Modules.Catalog.Services;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.DatabaseTest.Catalog;

public sealed class Spec027BusquedaStoreTests
{
    [Fact]
    public async Task Redis_No_Disponible_Busca_En_Mongo_CA_027_15()
    {
        var password = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
        await using var database = await CatalogDatabase.CreateAsync(seeded: false, redisConnectionString: $"127.0.0.1:6399,password={password},abortConnect=false,connectTimeout=1000,syncTimeout=1000,asyncTimeout=1000");
        var colonia = ObjectId.GenerateNewId();
        await database.Database.GetCollection<BsonDocument>("colonias").InsertOneAsync(new BsonDocument { ["_id"] = colonia, ["activo"] = true });
        await database.Database.GetCollection<BsonDocument>("catalogo_meta").InsertOneAsync(new BsonDocument { ["_id"] = "sinonimos", ["version"] = 1 });
        var options = Options.Create(new KnockMongoOptions());
        var source = new BusquedaSource(database.Database, options, database.Get<CatalogStore>(), new BusquedaStore(database.Database, options));
        var service = new BusquedaService(source, database.Get<ISearchResponseCache>(), new FiltroDatosPersonalesNoDisponible(), TimeProvider.System, System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        database.ClearCommands();
        var result = await service.SearchAsync(new SearchInput("boiler", colonia.ToString(), null, false, 20, null), CancellationToken.None);
        result.Error.Should().BeNull();
        result.Response.Should().NotBeNull();
        database.Commands.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Entrega_Invalida_Busqueda_Y_Cursor_De_Version_Anterior_CA_027_11()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: false, useRedisCache: true);
        var documentDb = await DocumentDbFixture.SharedAsync();
        var redis = await RedisFixture.SharedAsync();
        var suffix = Guid.NewGuid().ToString("N");
        var env = new Dictionary<string, string>
        {
            ["KNOCK_DB_MARKET"] = database.DatabaseName,
            ["KNOCK_DB_IDENTITY"] = "identity_" + suffix,
            ["KNOCK_DB_MESSAGING"] = "messaging_" + suffix,
            ["KNOCK_DB_DIR"] = MongoToolsContainer.DbDirectory,
            ["KNOCK_REDIS_HOST"] = redis.ContainerInternalHost,
            ["KNOCK_REDIS_PORT"] = RedisFixture.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["KNOCK_REDIS_PASSWORD"] = redis.Password,
            ["KNOCK_REDIS_DB"] = database.Redis.Database.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        try
        {
            await using var tools = await MongoToolsContainer.StartAsync(documentDb, env);
            var script = $"{MongoToolsContainer.DbDirectory}/entrega.js";
            var first = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);
            first.ExitCode.Should().Be(0, first.Output);
            var colonia = ObjectId.GenerateNewId();
            await database.Database.GetCollection<BsonDocument>("colonias").InsertOneAsync(new BsonDocument { ["_id"] = colonia, ["activo"] = true });
            var technicians = Enumerable.Range(0, 2).Select(_ => Technician("demo", 0, 10, "activo", false, "ninguno", "plo")).ToArray();
            foreach (var technician in technicians) technician["coloniaBaseId"] = colonia.ToString();
            await database.Database.GetCollection<BsonDocument>("tecnicos").InsertManyAsync(technicians);
            var options = Options.Create(new KnockMongoOptions());
            var source = new BusquedaSource(database.Database, options, database.Get<CatalogStore>(), new BusquedaStore(database.Database, options));
            var service = new BusquedaService(source, database.Get<ISearchResponseCache>(), new FiltroDatosPersonalesNoDisponible(), TimeProvider.System, System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            var input = new SearchInput("boiler", colonia.ToString(), null, false, 1, null);
            var initial = await service.SearchAsync(input, CancellationToken.None);
            initial.Error.Should().BeNull();
            initial.Response!.NextCursor.Should().NotBeNull();
            database.ClearCommands();
            (await service.SearchAsync(input, CancellationToken.None)).Response!.NextCursor.Should().Be(initial.Response.NextCursor);
            database.Commands.Should().BeEmpty("Redis devuelve contexto y respuesta sin consultar MongoDB");
            var previousVersion = (await database.Database.GetCollection<BsonDocument>("catalogo_meta").Find(new BsonDocument("_id", "sinonimos")).SingleAsync())["version"].AsInt32;
            await database.Database.GetCollection<BsonDocument>("sinonimos").DeleteOneAsync(new BsonDocument("termino", "boiler"));
            var second = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", script);
            second.ExitCode.Should().Be(0, second.Output);
            var newVersion = (await database.Database.GetCollection<BsonDocument>("catalogo_meta").Find(new BsonDocument("_id", "sinonimos")).SingleAsync())["version"].AsInt32;
            newVersion.Should().BeGreaterThan(previousVersion);
            database.ClearCommands();
            (await service.SearchAsync(input, CancellationToken.None)).Error.Should().BeNull();
            database.Commands.Should().NotBeEmpty();
            (await service.SearchAsync(input with { Cursor = initial.Response.NextCursor }, CancellationToken.None)).Error.Should().Be("invalid_cursor");
        }
        finally
        {
            using var client = new MongoClient(documentDb.ConnectionString);
            await client.DropDatabaseAsync(env["KNOCK_DB_IDENTITY"]);
            await client.DropDatabaseAsync(env["KNOCK_DB_MESSAGING"]);
        }
    }

    [Fact]
    public async Task Cache_Contexto_Y_Respuesta_Expira_CA_027_15()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: false);
        var colonia = ObjectId.GenerateNewId();
        await database.Database.GetCollection<BsonDocument>("colonias").InsertOneAsync(new BsonDocument { ["_id"] = colonia, ["activo"] = true });
        await database.Database.GetCollection<BsonDocument>("oficios").InsertOneAsync(new BsonDocument { ["clave"] = "plo", ["nombre"] = "Plomería", ["nombreBusqueda"] = "PLOMERIA", ["modalidad"] = "domicilio", ["activo"] = true });
        await database.Database.GetCollection<BsonDocument>("sinonimos").InsertOneAsync(new BsonDocument { ["termino"] = "boiler", ["oficioClave"] = "plo", ["version"] = 1 });
        await database.Database.GetCollection<BsonDocument>("catalogo_meta").InsertOneAsync(new BsonDocument { ["_id"] = "sinonimos", ["version"] = 1 });
        var options = Options.Create(new KnockMongoOptions());
        var source = new BusquedaSource(database.Database, options, database.Get<CatalogStore>(), new BusquedaStore(database.Database, options));
        var clock = new ManualClock();
        var service = new BusquedaService(source, new TimedSearchCache(clock), new FiltroDatosPersonalesNoDisponible(), clock, System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var input = new SearchInput("boiler", colonia.ToString(), null, false, 20, null);
        database.ClearCommands();
        (await service.SearchAsync(input, CancellationToken.None)).Error.Should().BeNull();
        database.Commands.Should().NotBeEmpty();
        database.ClearCommands();
        (await service.SearchAsync(input, CancellationToken.None)).Error.Should().BeNull();
        database.Commands.Should().BeEmpty();
        clock.Advance(TimeSpan.FromSeconds(31));
        (await service.SearchAsync(input, CancellationToken.None)).Error.Should().BeNull();
        database.Commands.Should().NotBeEmpty();
    }

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UnixEpoch;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now += duration;
    }

    private sealed class TimedSearchCache(TimeProvider clock) : ISearchResponseCache
    {
        private readonly Dictionary<string, (string Value, DateTimeOffset Expiry)> _entries = new(StringComparer.Ordinal);
        public Task<long> GetSearchGenerationAsync(CancellationToken cancellationToken) => Task.FromResult(0L);
        public Task<string?> GetSearchAsync(byte[] fingerprint, CancellationToken cancellationToken)
        {
            var found = _entries.TryGetValue(Convert.ToHexString(fingerprint), out var entry) && entry.Expiry > clock.GetUtcNow();
            return Task.FromResult(found ? entry.Value : null);
        }
        public Task SetSearchAsync(byte[] fingerprint, string response, CancellationToken cancellationToken)
        {
            _entries[Convert.ToHexString(fingerprint)] = (response, clock.GetUtcNow().AddSeconds(30));
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Servicio_Pagina_Cien_En_DocumentDb_CA_027_01_CA_027_10()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: false);
        await ApplyScriptAsync(database);
        var colonia = ObjectId.GenerateNewId();
        await database.Database.GetCollection<BsonDocument>("colonias").InsertOneAsync(new BsonDocument { ["_id"] = colonia, ["activo"] = true });
        await database.Database.GetCollection<BsonDocument>("oficios").InsertOneAsync(new BsonDocument { ["clave"] = "plo", ["nombre"] = "Plomería", ["nombreBusqueda"] = "PLOMERIA", ["modalidad"] = "domicilio", ["activo"] = true });
        await database.Database.GetCollection<BsonDocument>("sinonimos").InsertOneAsync(new BsonDocument { ["termino"] = "boiler", ["oficioClave"] = "plo", ["version"] = 1 });
        await database.Database.GetCollection<BsonDocument>("catalogo_meta").InsertOneAsync(new BsonDocument { ["_id"] = "sinonimos", ["version"] = 1 });
        var docs = Enumerable.Range(0, 130).Select(i =>
        {
            var doc = Technician("demo", 0, 10, "activo", false, "ninguno", "plo");
            doc["coloniaBaseId"] = colonia.ToString();
            return doc;
        }).ToArray();
        await database.Database.GetCollection<BsonDocument>("tecnicos").InsertManyAsync(docs);
        var options = Options.Create(new KnockMongoOptions());
        var store = new BusquedaStore(database.Database, options);
        var source = new BusquedaSource(database.Database, options, database.Get<CatalogStore>(), store);
        var service = new BusquedaService(source, new DisabledCatalogCache(), new FiltroDatosPersonalesNoDisponible(), TimeProvider.System, System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var ids = new List<string>();
        string? cursor = null;
        do
        {
            var result = await service.SearchAsync(new SearchInput("boiler", colonia.ToString(), null, false, 20, cursor), CancellationToken.None);
            result.Error.Should().BeNull();
            result.Response!.Interpretation.Detected!.Key.Should().Be("plo");
            ids.AddRange(result.Response.Items.Select(item => item.Id));
            cursor = result.Response.NextCursor;
        }
        while (cursor is not null);
        ids.Should().HaveCount(100).And.OnlyHaveUniqueItems();
        ids.Should().Equal(docs.OrderBy(d => d["_id"].AsObjectId).Take(100).Select(d => d["_id"].ToString()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Pagina_Desde_Mongo_Sin_Saltos_Y_Tope_CA_027_10(bool center)
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: false);
        await ApplyScriptAsync(database);
        var docs = Enumerable.Range(0, 130).Select(PagedTechnician).ToArray();
        await database.Database.GetCollection<BsonDocument>("tecnicos").InsertManyAsync(docs);
        var store = new BusquedaStore(database.Database, Options.Create(new KnockMongoOptions()));
        var input = new SearchPageRequest("colonia-demo", center ? 0 : null, center ? 0 : null, ["exp"], "plo", ["plo", "ele"], false);
        var all = await store.FindAsync(input.ColoniaId, input.Latitude, input.Longitude, input.Words, input.Trade, input.ActiveTrades, false, CancellationToken.None);
        var expected = OrdenDeResultados.Order(all.Select(c => new SearchRank(c.Id, OrdenDeResultados.Classify(c.MatchesName, c.ServesColonia, c.Trades.Contains("plo")).GetValueOrDefault(), c.Verified, c.Rating, c.DistanceKm))).Take(100).Select(x => x.Id).ToArray();
        database.ClearCommands();
        var found = new List<string>();
        SearchRank? last = null;
        while (found.Count < 100)
        {
            var page = await store.FindPageAsync(input, last, Math.Min(21, 101 - found.Count), CancellationToken.None);
            var selected = page.Take(Math.Min(20, 100 - found.Count)).ToArray();
            found.AddRange(selected.Select(x => x.Candidate.Id));
            if (selected.Length == 0) break;
            last = selected[^1].Rank;
        }
        found.Should().Equal(expected).And.OnlyHaveUniqueItems();
        database.Commands.Where(c => c.Names.FirstOrDefault() == "aggregate" && c["aggregate"] == "tecnicos")
            .Should().NotBeEmpty().And.OnlyContain(c => c["pipeline"].AsBsonArray.Any(s => s.AsBsonDocument.Contains("$limit") && s["$limit"].ToInt32() <= 21));
    }

    [Fact]
    public async Task Busca_Cobertura_Estado_Prefijo_Verificacion_CA_027_04_CA_027_07_CA_027_09()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: false);
        await ApplyScriptAsync(database);
        var collection = database.Database.GetCollection<BsonDocument>("tecnicos");
        var near = Technician("cercano", 0.001, 2, "activo", false, "basica", "plo");
        var middle = Technician("medio", 0.01, 2, "activo", false, "completa", "plo");
        var unverified = Technician("sinverificar", 0.015, 2, "activo", false, "ninguno", "plo");
        var far = Technician("lejos", 0.05, 1, "activo", false, "ninguno", "plo");
        var nameOutside = Technician("especial", 0.4, 1, "activo", false, "basica", "ele");
        var suspended = Technician("suspendido", 0.001, 5, "suspendido", false, "basica", "plo");
        var hidden = Technician("oculto", 0.001, 5, "activo", true, "basica", "plo");
        var inactiveTrade = Technician("sinoficio", 0.001, 5, "activo", false, "basica", "inactivo");
        var differentColonia = Technician("otracolonia", 0.1, 1, "activo", false, "basica", "plo");
        differentColonia["coloniaBaseId"] = "otra";
        await collection.InsertManyAsync([near, middle, unverified, far, nameOutside, suspended, hidden, inactiveTrade, differentColonia]);
        var store = new BusquedaStore(database.Database, Options.Create(new KnockMongoOptions()));
        var candidates = await store.FindAsync("colonia-demo", 0, 0, ["esp"], "plo", ["plo", "ele"], false, CancellationToken.None);
        candidates.Select(x => x.Id).Should().BeEquivalentTo(near["_id"].ToString(), middle["_id"].ToString(), unverified["_id"].ToString(), nameOutside["_id"].ToString());
        candidates.Single(x => x.Id == nameOutside["_id"].ToString()).ServesColonia.Should().BeFalse();
        candidates.Where(x => x.ServesColonia).Select(x => x.DistanceKm).Should().BeInAscendingOrder();
        var verified = await store.FindAsync("colonia-demo", 0, 0, ["esp"], "plo", ["plo", "ele"], true, CancellationToken.None);
        verified.Should().HaveCount(3);
        verified.Should().OnlyContain(x => x.Verified);
        var shortPrefix = await store.FindAsync("otra", 0, 0, ["es"], null, ["plo", "ele"], false, CancellationToken.None);
        shortPrefix.Should().BeEmpty();
        var notPrefix = await store.FindAsync("otra", 0, 0, ["pecial"], null, ["plo", "ele"], false, CancellationToken.None);
        notPrefix.Should().BeEmpty();
        var noCenter = await store.FindAsync("colonia-demo", null, null, [], "plo", ["plo", "ele"], false, CancellationToken.None);
        noCenter.Select(x => x.Id).Should().BeEquivalentTo(near["_id"].ToString(), middle["_id"].ToString(), unverified["_id"].ToString(), far["_id"].ToString());
        var aggregate = database.Commands.First(x => x.Names.FirstOrDefault() == "aggregate" && x["aggregate"] == "tecnicos");
        aggregate["pipeline"].AsBsonArray[0].AsBsonDocument.Contains("$geoNear").Should().BeTrue();
        aggregate.Contains("maxTimeMS").Should().BeTrue();
    }

    [Fact]
    public async Task Conteo_Anonimo_Atomicamente_Y_Ttl_R_027_12()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: false);
        await ApplyScriptAsync(database);
        var store = new BusquedaStore(database.Database, Options.Create(new KnockMongoOptions()));
        var instant = new DateTimeOffset(2026, 11, 14, 2, 0, 0, TimeSpan.Zero);
        await store.CountUnmatchedAsync("quiero pastel", instant, CancellationToken.None);
        await store.CountUnmatchedAsync("quiero pastel", instant, CancellationToken.None);
        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => store.CountUnmatchedAsync("quiero pastel", instant, CancellationToken.None)));
        var collection = database.Database.GetCollection<BsonDocument>("busquedas_sin_resultado");
        var doc = await collection.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        doc.Names.Should().BeEquivalentTo("_id", "terminoNormalizado", "dia", "conteo", "creadoEn");
        doc["terminoNormalizado"].AsString.Should().Be("quiero pastel");
        doc["dia"].AsString.Should().Be("2026-11-13");
        doc["conteo"].ToInt32().Should().Be(12);
        doc["creadoEn"].ToUniversalTime().Should().Be(instant.UtcDateTime);
        (await MongoIndexes.DescribeAsync(database.Database, "busquedas_sin_resultado")).Should().Contain("creadoEn_1 creadoEn:1 - ttl:7776000");
    }

    private static async Task ApplyScriptAsync(CatalogDatabase database)
    {
        await using var tools = await MongoToolsContainer.StartAsync(await DocumentDbFixture.SharedAsync(), new Dictionary<string, string> { ["KNOCK_DB_MARKET"] = database.DatabaseName });
        var result = await tools.RunAsync("mongosh", "--nodb", "--quiet", "--file", $"{MongoToolsContainer.DbDirectory}/027-catalog-busqueda.js");
        result.ExitCode.Should().Be(0, result.Output);
    }

    private static BsonDocument PagedTechnician(int i)
    {
        var doc = Technician(i < 40 || i >= 80 ? "experto" : "otro", i >= 80 ? 0.4 + i * 0.00001 : 0.001 + i * 0.00001,
            10, "activo", false, i % 3 == 0 ? "basica" : "ninguno", i >= 80 ? "ele" : "plo");
        doc["calificacion"] = i % 5;
        return doc;
    }

    private static BsonDocument Technician(string word, double longitude, int radius, string state, bool hidden, string verification, string trade) => new()
    {
        ["_id"] = ObjectId.GenerateNewId(),
        ["nombrePublico"] = word,
        ["palabrasBusqueda"] = new BsonArray { word },
        ["ubicacionBase"] = new BsonDocument { ["type"] = "Point", ["coordinates"] = new BsonArray { longitude, 0 } },
        ["radioAtencionKm"] = radius,
        ["coloniaBaseId"] = "colonia-demo",
        ["estado"] = state,
        ["oculto"] = hidden,
        ["nivelVerificacion"] = verification,
        ["oficios"] = new BsonArray { trade },
        ["calificacion"] = 4,
        ["totalResenas"] = 1,
    };
}
