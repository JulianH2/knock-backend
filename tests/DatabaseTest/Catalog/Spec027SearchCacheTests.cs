using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Knock.Market.Api.Modules.Catalog.Data;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xunit;

namespace Knock.DatabaseTest.Catalog;

public sealed class Spec027SearchCacheTests
{
    [Fact]
    public async Task Cache_Usa_Redis_Y_Vence_En_30_Segundos_R_027_13()
    {
        var fixture = await RedisFixture.SharedAsync();
        using var redis = await ConnectionMultiplexer.ConnectAsync(fixture.ConnectionString);
        var cache = new RedisCatalogCache(redis, Options.Create(new CatalogCacheOptions()), NullLogger<RedisCatalogCache>.Instance);
        var hash = RandomNumberGenerator.GetBytes(32);
        await cache.SetSearchAsync(hash, "synthetic", CancellationToken.None);
        (await cache.GetSearchAsync(hash, CancellationToken.None)).Should().Be("synthetic");
        var key = "busqueda:" + Convert.ToHexStringLower(hash);
        var ttl = await redis.GetDatabase().KeyTimeToLiveAsync(key);
        ttl.Should().NotBeNull();
        ttl.Value.TotalSeconds.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(30);
        await redis.GetDatabase().KeyDeleteAsync(key);
    }

    [Fact]
    public async Task Redis_No_Disponible_No_Impide_Lectura_R_027_13()
    {
        var config = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            ConnectTimeout = 200,
            AsyncTimeout = 200,
            ConnectRetry = 0,
        };
        config.EndPoints.Add("127.0.0.1", 1);
        using var redis = await ConnectionMultiplexer.ConnectAsync(config);
        var cache = new RedisCatalogCache(redis, Options.Create(new CatalogCacheOptions()), NullLogger<RedisCatalogCache>.Instance);
        var hash = RandomNumberGenerator.GetBytes(32);
        (await cache.GetSearchAsync(hash, CancellationToken.None)).Should().BeNull();
        await cache.SetSearchAsync(hash, "synthetic", CancellationToken.None);
    }
}
