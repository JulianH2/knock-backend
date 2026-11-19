using System.Diagnostics;
using System.Threading.RateLimiting;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;
using Xunit;

namespace Knock.UnitTest.Hosting;

/// <summary>
/// Spec 006, R-05 (KNOCK-247): si Redis no está conectado, el limitador compartido de Identity y Market cae de inmediato al límite
/// por copia, sin esperar el tiempo de espera del cliente de Redis en cada petición. Sin contenedores: apunta a un puerto sin servicio.
/// </summary>
public sealed class Spec006SharedRateLimiterTests
{
    [Fact]
    public async Task Market_RedisNotConnected_FallsBackToPerInstanceLimitWithoutWaiting_R_006_05()
    {
        using var provider = await CreateProviderAsync();
        var policy = new Knock.Market.Api.Hosting.RateLimitPolicySettings { PermitLimit = 2, Window = TimeSpan.FromMinutes(1) };
        using var limiter = new Knock.Market.Api.Hosting.SharedRateLimiter(provider, "global:test", policy);

        var (results, elapsed) = await AcquireThreeAsync(limiter);

        results.Should().Equal(true, true, false);
        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Identity_RedisNotConnected_FallsBackToPerInstanceLimitWithoutWaiting_R_006_05()
    {
        using var provider = await CreateProviderAsync();
        var policy = new Knock.Identity.Api.Hosting.RateLimitPolicySettings { PermitLimit = 2, Window = TimeSpan.FromMinutes(1) };
        using var limiter = new Knock.Identity.Api.Hosting.SharedRateLimiter(provider, "global:test", policy);

        var (results, elapsed) = await AcquireThreeAsync(limiter);

        results.Should().Equal(true, true, false);
        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }

    private static async Task<ServiceProvider> CreateProviderAsync()
    {
        var options = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            ConnectTimeout = 500,
            AsyncTimeout = 5000,
            SyncTimeout = 5000,
        };
        options.EndPoints.Add("127.0.0.1", 9);
        var connection = await ConnectionMultiplexer.ConnectAsync(options);
        connection.IsConnected.Should().BeFalse();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<IConnectionMultiplexer>(connection);
        return services.BuildServiceProvider();
    }

    private static async Task<(bool[] Results, TimeSpan Elapsed)> AcquireThreeAsync(RateLimiter limiter)
    {
        var clock = Stopwatch.StartNew();
        var results = new bool[3];
        for (var i = 0; i < results.Length; i++)
        {
            using var lease = await limiter.AcquireAsync();
            results[i] = lease.IsAcquired;
        }

        return (results, clock.Elapsed);
    }
}
