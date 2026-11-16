using AwesomeAssertions;
using Knock.DatabaseTest.Support;
using Knock.IntegrationTest.Support;
using Knock.Market.Api.Modules.Catalog.Data;
using Knock.Market.Api.Modules.Catalog.Domain;
using Knock.Market.Api.Modules.Catalog.Services;
using MongoDB.Bson;
using MongoDB.Driver;
using StackExchange.Redis;
using Xunit;

namespace Knock.DatabaseTest.Catalog;

/// <summary>
/// Spec 010, R-01, R-02, R-04 y R-05: cache-aside con Redis real y MongoDB real (documentdb-local). Cada instancia usa su propia base
/// y limpia Redis al crearse; la colección serializa estas pruebas porque las claves del catálogo son fijas.
/// </summary>
[Collection("CatalogCacheSerial")]
public sealed class CatalogCacheTests
{
    [Theory]
    [InlineData("42800", true)]
    [InlineData("99999", false)]
    public async Task Consulta_Repetida_Usa_Cache_CA_023_13(string cp, bool found)
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: true, useRedisCache: true);
        var store = database.Get<CatalogStore>();
        database.ClearCommands();
        var first = await store.FindByPostalCodeAsync(cp, CancellationToken.None);
        database.FindCommands.Should().HaveCount(1);
        database.ClearCommands();
        var second = await store.FindByPostalCodeAsync(cp, CancellationToken.None);
        database.FindCommands.Should().BeEmpty();
        second.Should().Equal(first);
        (first.Count > 0).Should().Be(found);
        var cached = (await database.Redis.StringGetAsync(RedisCatalogCache.NeighborhoodNamesKey(cp))).ToString();
        cached.Should().NotContain("geom").And.NotContain("centro").And.NotContain("nombreBusqueda").And.NotContain("piloto").And.NotContain("version");
        var ttl = await database.Redis.KeyTimeToLiveAsync(RedisCatalogCache.NeighborhoodNamesKey(cp));
        ttl.Should().NotBeNull();
        if (found) ttl.Value.Should().BeGreaterThan(TimeSpan.FromHours(23)).And.BeLessThanOrEqualTo(TimeSpan.FromHours(24));
        else ttl.Value.Should().BeGreaterThan(TimeSpan.FromMinutes(4)).And.BeLessThanOrEqualTo(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task Cache_Negativa_Corta_CA_023_14()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: true, useRedisCache: true);
        var store = database.Get<CatalogStore>();
        database.ClearCommands();
        (await store.FindByPostalCodeAsync("99998", CancellationToken.None)).Should().BeEmpty();
        database.FindCommands.Should().HaveCount(1);
        database.ClearCommands();
        (await store.FindByPostalCodeAsync("99998", CancellationToken.None)).Should().BeEmpty();
        database.FindCommands.Should().BeEmpty();
        var ttl = await database.Redis.KeyTimeToLiveAsync(RedisCatalogCache.NeighborhoodNamesKey("99998"));
        ttl.Should().NotBeNull();
        ttl.Value.Should().BeGreaterThan(TimeSpan.FromMinutes(4)).And.BeLessThanOrEqualTo(TimeSpan.FromMinutes(5));
    }
    [Fact]
    public async Task RepeatedOficiosRead_QueriesMongoOnce_CA_010_01()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: true, useRedisCache: true);
        var store = database.Get<CatalogStore>();

        database.ClearCommands();
        var first = await store.FindActiveOficiosAsync(CancellationToken.None);
        database.FindCommands.Should().HaveCount(1, "la primera lectura es un fallo de caché y va a MongoDB");
        (await database.Redis.KeyExistsAsync(RedisCatalogCache.OficiosKey)).Should().BeTrue("el fallo guarda el resultado");

        database.ClearCommands();
        var second = await store.FindActiveOficiosAsync(CancellationToken.None);
        database.FindCommands.Should().BeEmpty("la segunda lectura sale de Redis sin consultar MongoDB");
        second.Should().Equal(first);
    }

    [Fact]
    public async Task RepeatedNamesRead_QueriesMongoOnce_CA_010_01()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: true, useRedisCache: true);
        var store = database.Get<CatalogStore>();

        database.ClearCommands();
        var first = await store.FindNamesByPostalCodeAsync("42803", CancellationToken.None);
        database.FindCommands.Should().HaveCount(1, "la primera lectura es un fallo de caché y va a MongoDB");
        (await database.Redis.KeyExistsAsync(RedisCatalogCache.NeighborhoodNamesKey("42803"))).Should().BeTrue("el fallo guarda el resultado");

        database.ClearCommands();
        var second = await store.FindNamesByPostalCodeAsync("42803", CancellationToken.None);
        database.FindCommands.Should().BeEmpty("la segunda lectura sale de Redis sin consultar MongoDB");
        second.Should().Equal(first);
    }

    [Fact]
    public async Task RepeatedExistsRead_QueriesMongoOnce_CA_010_01()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: true, useRedisCache: true);
        var store = database.Get<CatalogStore>();
        var centroId = CatalogSeedData.Colonia("Centro")["_id"].AsObjectId;

        database.ClearCommands();
        (await store.ColoniaExistsAsync(centroId, "42800", CancellationToken.None)).Should().BeTrue();
        database.FindCommands.Should().HaveCount(1, "la primera lectura es un fallo de caché y va a MongoDB");

        database.ClearCommands();
        (await store.ColoniaExistsAsync(centroId, "42800", CancellationToken.None)).Should().BeTrue();
        database.FindCommands.Should().BeEmpty("la segunda lectura sale de Redis sin consultar MongoDB");
    }

    [Fact]
    public async Task CatalogEntriesLiveAbout24Hours_CA_010_01()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: true, useRedisCache: true);
        var store = database.Get<CatalogStore>();
        var centroId = CatalogSeedData.Colonia("Centro")["_id"].AsObjectId;

        await store.FindActiveOficiosAsync(CancellationToken.None);
        await store.FindNamesByPostalCodeAsync("42803", CancellationToken.None);
        await store.ColoniaExistsAsync(centroId, "42800", CancellationToken.None);

        foreach (var key in new[]
                 {
                     RedisCatalogCache.OficiosKey,
                     RedisCatalogCache.NeighborhoodNamesKey("42803"),
                     RedisCatalogCache.ColoniaExistsKey(centroId, "42800"),
                 })
        {
            var ttl = await database.Redis.KeyTimeToLiveAsync(key);
            ttl.Should().NotBeNull($"la clave {key} tiene vigencia");
            ttl.Value.Should().BeLessThanOrEqualTo(TimeSpan.FromHours(24)).And.BeGreaterThan(TimeSpan.FromHours(23));
        }
    }

    [Fact]
    public async Task ConfiguredLifetime_AppliesToCacheKeys_CA_010_01()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: true, useRedisCache: true, catalogLifetimeSeconds: 3600);
        var store = database.Get<CatalogStore>();

        await store.FindActiveOficiosAsync(CancellationToken.None);

        var ttl = await database.Redis.KeyTimeToLiveAsync(RedisCatalogCache.OficiosKey);
        ttl.Should().NotBeNull("la clave guarda con la vida configurada");
        ttl.Value.Should().BeLessThanOrEqualTo(TimeSpan.FromHours(1)).And.BeGreaterThan(TimeSpan.FromMinutes(59));
    }

    [Fact]
    public async Task ExpiredKey_FallsBackToMongo_CA_010_01()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: true, useRedisCache: true, catalogLifetimeSeconds: 60);
        var store = database.Get<CatalogStore>();

        database.ClearCommands();
        await store.FindActiveOficiosAsync(CancellationToken.None);
        database.FindCommands.Should().HaveCount(1, "la primera lectura es un fallo de caché");

        await database.Redis.KeyExpireAsync(RedisCatalogCache.OficiosKey, TimeSpan.FromMilliseconds(50));
        await Task.Delay(200);

        (await database.Redis.KeyExistsAsync(RedisCatalogCache.OficiosKey)).Should().BeFalse("la clave venció");

        database.ClearCommands();
        var second = await store.FindActiveOficiosAsync(CancellationToken.None);
        database.FindCommands.Should().HaveCount(1, "tras vencer la clave, la lectura vuelve a MongoDB");
        second.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DifferentPostalCodesAndIds_DoNotShareCacheEntries_CA_010_01()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: true, useRedisCache: true);
        var store = database.Get<CatalogStore>();

        var cielito = await store.FindNamesByPostalCodeAsync("42803", CancellationToken.None);
        var centro = await store.FindNamesByPostalCodeAsync("42800", CancellationToken.None);
        cielito.Should().HaveCount(11);
        centro.Should().Equal("Centro");

        var centroId = CatalogSeedData.Colonia("Centro")["_id"].AsObjectId;
        (await store.ColoniaExistsAsync(centroId, "42800", CancellationToken.None)).Should().BeTrue();
        (await store.ColoniaExistsAsync(centroId, "42803", CancellationToken.None)).Should().BeFalse("el par _id y CP es otra clave");
    }

    [Fact]
    public async Task ReseedingOficios_RemovesCache_AndNextReadReturnsNewData_CA_010_02()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: true, useRedisCache: true);
        var store = database.Get<CatalogStore>();

        await store.FindActiveOficiosAsync(CancellationToken.None);
        (await database.Redis.KeyExistsAsync(RedisCatalogCache.OficiosKey)).Should().BeTrue("la lectura llena la caché");

        var changed = CatalogSeedData.Oficios.Select(document => document.DeepClone().AsBsonDocument).ToList();
        changed.Single(document => document["clave"] == "ele")["nombre"] = "Electricidad Actualizada";
        await database.InScopeAsync<CatalogSeeder>(seeder => seeder.SeedAsync(changed, CatalogSeedData.Colonias, CancellationToken.None));

        (await database.Redis.KeyExistsAsync(RedisCatalogCache.OficiosKey)).Should().BeFalse("la siembra borra la clave antes de responder");
        var after = await store.FindActiveOficiosAsync(CancellationToken.None);
        after.Should().ContainSingle(oficio => oficio.Clave == "ele" && oficio.Nombre == "Electricidad Actualizada");
    }

    [Fact]
    public async Task ReseedingColonias_RemovesCache_AndNextReadReturnsNewData_CA_010_02()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: true, useRedisCache: true);
        var store = database.Get<CatalogStore>();
        var centroId = CatalogSeedData.Colonia("Centro")["_id"].AsObjectId;

        await store.FindNamesByPostalCodeAsync("42803", CancellationToken.None);
        await store.ColoniaExistsAsync(centroId, "42800", CancellationToken.None);
        (await database.Redis.KeyExistsAsync(RedisCatalogCache.NeighborhoodNamesKey("42803"))).Should().BeTrue();
        (await database.Redis.KeyExistsAsync(RedisCatalogCache.ColoniaExistsKey(centroId, "42800"))).Should().BeTrue();

        await database.Colonias.InsertOneAsync(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "nombre", "Zz Nueva" },
            { "nombreBusqueda", "ZZ NUEVA" },
            { "cp", "42803" },
            { "municipio", "Tula de Allende" },
            { "piloto", false },
            { "aproximado", false },
        });
        await database.InScopeAsync<CatalogSeeder>(seeder => seeder.SeedAsync(CatalogSeedData.Oficios, CatalogSeedData.Colonias, CancellationToken.None));

        (await database.Redis.KeyExistsAsync(RedisCatalogCache.NeighborhoodNamesKey("42803"))).Should().BeFalse("la siembra borra la clave del CP");
        (await database.Redis.KeyExistsAsync(RedisCatalogCache.ColoniaExistsKey(centroId, "42800"))).Should().BeFalse("la siembra borra la clave de existencia");
        var after = await store.FindNamesByPostalCodeAsync("42803", CancellationToken.None);
        after.Should().Contain("Zz Nueva", "la siguiente lectura trae el dato nuevo");
    }

    [Fact]
    public async Task CoordinateQueries_AreNeverCached_CA_010_01()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: true, useRedisCache: true);
        var (colonia, longitude, latitude) = CatalogSeedData.InteriorPoints[0];

        var first = await database.InScopeAsync<INeighborhoodLocator, Colonia?>(
            locator => locator.FindAtAsync(latitude, longitude, CancellationToken.None));
        first?.Nombre.Should().Be(colonia);

        database.ClearCommands();
        var second = await database.InScopeAsync<INeighborhoodLocator, Colonia?>(
            locator => locator.FindAtAsync(latitude, longitude, CancellationToken.None));
        second?.Nombre.Should().Be(colonia);
        database.FindCommands.Should().NotBeEmpty("la colonia por punto siempre consulta MongoDB");

        var server = database.Multiplexer.GetServer(database.Multiplexer.GetEndPoints()[0]);
        var keys = new List<RedisKey>();
        await foreach (var key in server.KeysAsync(pattern: "catalog:*"))
        {
            if (key.ToString().StartsWith("catalog:gen:", StringComparison.Ordinal))
            {
                continue;
            }

            keys.Add(key);
        }

        keys.Should().BeEmpty("ninguna clave de caché lleva coordenadas");
    }

    [Fact]
    public async Task StaleWrite_AfterInvalidation_DoesNotResurrectCacheEntry_CA_010_02()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: true, useRedisCache: true);
        var cache = database.Get<ICatalogCache>();
        var store = database.Get<CatalogStore>();
        var centroId = CatalogSeedData.Colonia("Centro")["_id"].AsObjectId;

        // El lector captura la generación y lee el dato viejo de Mongo.
        var generation = await cache.GetColoniaExistsGenerationAsync(centroId, "42800", CancellationToken.None);
        await store.ColoniaExistsAsync(centroId, "42800", CancellationToken.None);

        // La siembra invalida mientras el lector preparaba su guardado: la generación sube y la clave desaparece.
        await cache.RemoveColoniaExistsAsync(centroId, "42800", CancellationToken.None);
        (await database.Redis.KeyExistsAsync(RedisCatalogCache.ColoniaExistsKey(centroId, "42800"))).Should().BeFalse();

        // El lector intenta guardar lo viejo con la generación vieja: no aparece.
        var saved = await cache.SetColoniaExistsIfGenerationAsync(centroId, "42800", true, generation, CancellationToken.None);
        saved.Should().BeFalse("la generación cambió y el dato viejo no puede resucitar");
        (await database.Redis.KeyExistsAsync(RedisCatalogCache.ColoniaExistsKey(centroId, "42800"))).Should().BeFalse();
    }

    [Fact]
    public async Task FailedSeed_InvalidatesKeysAndBumpsGenerations_CA_010_02()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: true, useRedisCache: true);
        var store = database.Get<CatalogStore>();
        var centroId = CatalogSeedData.Colonia("Centro")["_id"].AsObjectId;

        await store.FindNamesByPostalCodeAsync("42800", CancellationToken.None);
        await store.ColoniaExistsAsync(centroId, "42800", CancellationToken.None);
        (await database.Redis.KeyExistsAsync(RedisCatalogCache.NeighborhoodNamesKey("42800"))).Should().BeTrue();
        (await database.Redis.KeyExistsAsync(RedisCatalogCache.ColoniaExistsKey(centroId, "42800"))).Should().BeTrue();

        var invalid = InvalidColoniasWithDuplicate();

        var act = () => database.InScopeAsync<CatalogSeeder>(seeder => seeder.SeedAsync(CatalogSeedData.Oficios, invalid, CancellationToken.None));

        await act.Should().ThrowAsync<MongoWriteException>();
        (await database.Redis.KeyExistsAsync(RedisCatalogCache.NeighborhoodNamesKey("42800"))).Should().BeFalse("la siembra fallida tampoco deja caché vieja");
        (await database.Redis.KeyExistsAsync(RedisCatalogCache.ColoniaExistsKey(centroId, "42800"))).Should().BeFalse();
        (await database.Redis.KeyExistsAsync(RedisCatalogCache.OficiosKey)).Should().BeFalse();
    }

    [Fact]
    public async Task SeedCancelledMidway_StillInvalidatesWithFreshToken_CA_010_02()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: true, useRedisCache: true);
        var cache = database.Get<ICatalogCache>();
        var store = database.Get<CatalogStore>();
        var centroId = CatalogSeedData.Colonia("Centro")["_id"].AsObjectId;

        await store.FindActiveOficiosAsync(CancellationToken.None);
        await store.FindNamesByPostalCodeAsync("42800", CancellationToken.None);
        await store.ColoniaExistsAsync(centroId, "42800", CancellationToken.None);
        (await database.Redis.KeyExistsAsync(RedisCatalogCache.NeighborhoodNamesKey("42800"))).Should().BeTrue();
        (await database.Redis.KeyExistsAsync(RedisCatalogCache.ColoniaExistsKey(centroId, "42800"))).Should().BeTrue();
        var namesBefore = await cache.GetNeighborhoodNamesGenerationAsync("42800", CancellationToken.None);
        var existsBefore = await cache.GetColoniaExistsGenerationAsync(centroId, "42800", CancellationToken.None);

        // El token se cancela justo después de la primera escritura (el primer oficio): la siembra se corta antes de tocar colonias,
        // así que solo el bloque de emergencia puede invalidar las claves de colonias. Si ese bloque usara el token cancelado, no lo haría.
        using var cancellation = new CancellationTokenSource();
        var seeder = new CatalogSeeder(store, new CancelAfterFirstWriteCache(cache, cancellation));

        var act = () => seeder.SeedAsync(CatalogSeedData.Oficios, CatalogSeedData.Colonias, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>("se relanza la cancelación original, sin AggregateException");
        (await database.Redis.KeyExistsAsync(RedisCatalogCache.OficiosKey)).Should().BeFalse();
        (await database.Redis.KeyExistsAsync(RedisCatalogCache.NeighborhoodNamesKey("42800"))).Should().BeFalse("la invalidación de emergencia corre aunque el token esté cancelado");
        (await database.Redis.KeyExistsAsync(RedisCatalogCache.ColoniaExistsKey(centroId, "42800"))).Should().BeFalse();
        (await cache.GetNeighborhoodNamesGenerationAsync("42800", CancellationToken.None)).Should().BeGreaterThan(namesBefore, "la generación subió: un lector lento no resucita el dato");
        (await cache.GetColoniaExistsGenerationAsync(centroId, "42800", CancellationToken.None)).Should().BeGreaterThan(existsBefore);
    }

    [Fact]
    public async Task RedisDown_ReadsFallBackToMongo_CA_010_01()
    {
        await using var database = await CatalogDatabase.CreateAsync(
            seeded: false, redisConnectionString: "127.0.0.1:6399,password=Kn0ckInvalidInvalidInvalid,abortConnect=false");
        var store = database.Get<CatalogStore>();
        await store.EnsureIndexesAsync(CancellationToken.None);
        await database.Oficios.InsertManyAsync(CatalogSeedData.Oficios.Select(document => document.DeepClone().AsBsonDocument).ToList());
        await database.Colonias.InsertManyAsync(CatalogSeedData.Colonias.Select(document => document.DeepClone().AsBsonDocument).ToList());

        var oficios = await store.FindActiveOficiosAsync(CancellationToken.None);
        oficios.Should().HaveCount(3, "sin Redis la lectura va a MongoDB");
        var names = await store.FindNamesByPostalCodeAsync("42803", CancellationToken.None);
        names.Should().HaveCount(11);
        var centroId = CatalogSeedData.Colonia("Centro")["_id"].AsObjectId;
        (await store.ColoniaExistsAsync(centroId, "42800", CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task RedisDown_SeedingFailsInsteadOfAnsweringWithStaleCache_CA_010_02()
    {
        await using var database = await CatalogDatabase.CreateAsync(
            seeded: false, redisConnectionString: "127.0.0.1:6399,password=Kn0ckInvalidInvalidInvalid,abortConnect=false");

        var act = () => CatalogTestDatabase.SeedAsync(database.Services);

        await act.Should().ThrowAsync<StackExchange.Redis.RedisException>("sin poder invalidar, la escritura no responde dejando datos viejos");
    }
    [Fact]
    public async Task SlowReader_HoldingOldGeneration_CannotResurrectDataAfterSeederInvalidation_CA_010_02()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: true, useRedisCache: true);
        var cache = database.Get<ICatalogCache>();
        var store = database.Get<CatalogStore>();
        var centroId = CatalogSeedData.Colonia("Centro")["_id"].AsObjectId;

        var generationBefore = await cache.GetColoniaExistsGenerationAsync(centroId, "42800", CancellationToken.None);
        await store.ColoniaExistsAsync(centroId, "42800", CancellationToken.None);

        var changed = CatalogSeedData.Colonias.Select(document => document.DeepClone().AsBsonDocument).ToList();
        await database.InScopeAsync<CatalogSeeder>(seeder => seeder.SeedAsync(CatalogSeedData.Oficios, changed, CancellationToken.None));

        (await database.Redis.KeyExistsAsync(RedisCatalogCache.ColoniaExistsKey(centroId, "42800"))).Should().BeFalse();
        (await database.Redis.StringGetAsync(RedisCatalogCache.ColoniaExistsGenerationKey(centroId, "42800"))).ToString().Should().NotBe(generationBefore.ToString(System.Globalization.CultureInfo.InvariantCulture));

        var saved = await cache.SetColoniaExistsIfGenerationAsync(centroId, "42800", false, generationBefore, CancellationToken.None);
        saved.Should().BeFalse("el lector lento capturó la generación antes de la siembra: no resucita");
        (await database.Redis.KeyExistsAsync(RedisCatalogCache.ColoniaExistsKey(centroId, "42800"))).Should().BeFalse();
    }

    [Fact]
    public async Task Invalidation_IncrementsGenerationAndDeletesDataAtomically_CA_010_02()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: true, useRedisCache: true);
        var cache = database.Get<ICatalogCache>();
        var store = database.Get<CatalogStore>();

        await store.FindActiveOficiosAsync(CancellationToken.None);
        var gen0 = (await database.Redis.StringGetAsync(RedisCatalogCache.OficiosGenerationKey)).ToString();

        await cache.RemoveOficiosAsync(CancellationToken.None);
        var gen1 = (await database.Redis.StringGetAsync(RedisCatalogCache.OficiosGenerationKey)).ToString();
        (await database.Redis.KeyExistsAsync(RedisCatalogCache.OficiosKey)).Should().BeFalse("la invalidación borra la clave de datos");
        (long.Parse(gen1, System.Globalization.CultureInfo.InvariantCulture)).Should().BeGreaterThan(long.Parse(string.IsNullOrEmpty(gen0) ? "0" : gen0, System.Globalization.CultureInfo.InvariantCulture), "la generación sube en el mismo script");

        await cache.RemoveOficiosAsync(CancellationToken.None);
        var gen2 = (await database.Redis.StringGetAsync(RedisCatalogCache.OficiosGenerationKey)).ToString();
        (long.Parse(gen2, System.Globalization.CultureInfo.InvariantCulture)).Should().Be(long.Parse(gen1, System.Globalization.CultureInfo.InvariantCulture) + 1, "cada invalidación sube la generación en uno, sin reiniciarla");
    }

    [Fact]
    public async Task Seeder_WithRetiredDocument_InvalidatesItsOldKeys_CA_010_02()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: true, useRedisCache: true);
        var store = database.Get<CatalogStore>();

        var retiredId = ObjectId.GenerateNewId();
        await database.Colonias.InsertOneAsync(new BsonDocument
        {
            { "_id", retiredId },
            { "nombre", "Retirada" },
            { "nombreBusqueda", "RETIRADA" },
            { "cp", "99999" },
            { "municipio", "Tula de Allende" },
            { "piloto", false },
            { "aproximado", false },
        });
        await store.FindNamesByPostalCodeAsync("99999", CancellationToken.None);
        await store.ColoniaExistsAsync(retiredId, "99999", CancellationToken.None);
        (await database.Redis.KeyExistsAsync(RedisCatalogCache.NeighborhoodNamesKey("99999"))).Should().BeTrue();

        await database.InScopeAsync<CatalogSeeder>(seeder => seeder.SeedAsync(CatalogSeedData.Oficios, CatalogSeedData.Colonias, CancellationToken.None));

        (await database.Redis.KeyExistsAsync(RedisCatalogCache.NeighborhoodNamesKey("99999"))).Should().BeFalse("la siembra cubre lo retirado aunque el seed ya no lo trae");
        (await database.Redis.KeyExistsAsync(RedisCatalogCache.ColoniaExistsKey(retiredId, "99999"))).Should().BeFalse();
    }

    [Fact]
    public async Task FailedSeed_BumpsGenerations_NotOnlyDeletesKeys_CA_010_02()
    {
        await using var database = await CatalogDatabase.CreateAsync(seeded: true, useRedisCache: true);
        var store = database.Get<CatalogStore>();

        await store.FindNamesByPostalCodeAsync("42800", CancellationToken.None);
        var genBefore = (await database.Redis.StringGetAsync(RedisCatalogCache.NeighborhoodNamesGenerationKey("42800"))).ToString();

        var invalid = InvalidColoniasWithDuplicate();

        var act = () => database.InScopeAsync<CatalogSeeder>(seeder => seeder.SeedAsync(CatalogSeedData.Oficios, invalid, CancellationToken.None));
        await act.Should().ThrowAsync<MongoWriteException>();

        var genAfter = (await database.Redis.StringGetAsync(RedisCatalogCache.NeighborhoodNamesGenerationKey("42800"))).ToString();
        (long.Parse(genAfter, System.Globalization.CultureInfo.InvariantCulture)).Should().BeGreaterThan(long.Parse(string.IsNullOrEmpty(genBefore) ? "0" : genBefore, System.Globalization.CultureInfo.InvariantCulture), "la siembra fallida también sube la generación");
        (await database.Redis.KeyExistsAsync(RedisCatalogCache.NeighborhoodNamesKey("42800"))).Should().BeFalse();
    }

    /// <summary>Colonias semilla más un segundo documento con el mismo <c>_id</c> y otra clave natural: el INSERT del segundo falla a mitad de la siembra.</summary>
    private static List<BsonDocument> InvalidColoniasWithDuplicate()
    {
        var invalid = CatalogSeedData.Colonias.Select(document => document.DeepClone().AsBsonDocument).ToList();
        var duplicateId = CatalogSeedData.Colonia("Centro")["_id"].DeepClone();
        invalid.Add(new BsonDocument
        {
            { "_id", duplicateId },
            { "nombre", "Centro Duplicada" },
            { "nombreBusqueda", "CENTRO DUPLICADA" },
            { "cp", "42800" },
            { "municipio", "Tula de Allende" },
            { "piloto", false },
            { "aproximado", false },
        });
        return invalid;
    }

    /// <summary>Caché real que cancela el token de la siembra después de la primera invalidación (la que sigue a la primera escritura).</summary>
    private sealed class CancelAfterFirstWriteCache(ICatalogCache inner, CancellationTokenSource cancellation) : ICatalogCache
    {
        public async Task RemoveOficiosAsync(CancellationToken cancellationToken)
        {
            await inner.RemoveOficiosAsync(cancellationToken);
            await cancellation.CancelAsync();
        }

        public Task<IReadOnlyList<OficioListing>?> GetOficiosAsync(CancellationToken cancellationToken) => inner.GetOficiosAsync(cancellationToken);

        public Task<long> GetOficiosGenerationAsync(CancellationToken cancellationToken) => inner.GetOficiosGenerationAsync(cancellationToken);

        public Task<bool> SetOficiosIfGenerationAsync(IReadOnlyList<OficioListing> oficios, long expectedGeneration, CancellationToken cancellationToken) =>
            inner.SetOficiosIfGenerationAsync(oficios, expectedGeneration, cancellationToken);

        public Task<IReadOnlyList<Colonia>?> GetNeighborhoodNamesAsync(string postalCode, CancellationToken cancellationToken) =>
            inner.GetNeighborhoodNamesAsync(postalCode, cancellationToken);

        public Task RemoveNeighborhoodNamesAsync(string postalCode, CancellationToken cancellationToken) =>
            inner.RemoveNeighborhoodNamesAsync(postalCode, cancellationToken);

        public Task<long> GetNeighborhoodNamesGenerationAsync(string postalCode, CancellationToken cancellationToken) =>
            inner.GetNeighborhoodNamesGenerationAsync(postalCode, cancellationToken);

        public Task<bool> SetNeighborhoodNamesIfGenerationAsync(string postalCode, IReadOnlyList<Colonia> names, long expectedGeneration, CancellationToken cancellationToken) =>
            inner.SetNeighborhoodNamesIfGenerationAsync(postalCode, names, expectedGeneration, cancellationToken);

        public Task<bool?> GetColoniaExistsAsync(ObjectId id, string postalCode, CancellationToken cancellationToken) =>
            inner.GetColoniaExistsAsync(id, postalCode, cancellationToken);

        public Task RemoveColoniaExistsAsync(ObjectId id, string postalCode, CancellationToken cancellationToken) =>
            inner.RemoveColoniaExistsAsync(id, postalCode, cancellationToken);

        public Task<long> GetColoniaExistsGenerationAsync(ObjectId id, string postalCode, CancellationToken cancellationToken) =>
            inner.GetColoniaExistsGenerationAsync(id, postalCode, cancellationToken);

        public Task<bool> SetColoniaExistsIfGenerationAsync(ObjectId id, string postalCode, bool exists, long expectedGeneration, CancellationToken cancellationToken) =>
            inner.SetColoniaExistsIfGenerationAsync(id, postalCode, exists, expectedGeneration, cancellationToken);
    }
}
