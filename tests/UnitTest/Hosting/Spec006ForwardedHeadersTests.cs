using System.Net;
using AwesomeAssertions;
using Knock.Hosting.ForwardedHeaders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Knock.UnitTest.Hosting;

/// <summary>
/// Spec 006, R-03: Messaging solo confía en <c>X-Forwarded-For</c>/<c>X-Forwarded-Proto</c> de las redes de
/// <c>ForwardedHeaders:KnownNetworks</c> (o proxies conocidos); sin configuración no confía en nadie, y un encabezado falso desde una IP
/// no confiable se ignora (seguridad negativa: no se puede suplantar la IP para evadir el límite de peticiones).
/// </summary>
public sealed class Spec006ForwardedHeadersTests
{
    private const string _trustedNetwork = "10.20.0.0/23";

    [Fact]
    public void WithoutConfiguration_TrustsNobody_R_006_03()
    {
        var options = OptionsOf([]);

        options.ForwardedHeaders.Should().Be(ForwardedHeaders.None, "sin redes ni proxies conocidos no se confía en ningún remitente");
        options.KnownProxies.Should().BeEmpty();
        options.KnownIPNetworks.Should().BeEmpty();
    }

    [Fact]
    public void WithAKnownNetwork_TrustsOnlyThatNetwork_R_006_03()
    {
        var options = OptionsOf([_trustedNetwork]);

        options.ForwardedHeaders.Should().Be(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
        options.KnownIPNetworks.Should().ContainSingle().Which.PrefixLength.Should().Be(23);
    }

    [Fact]
    public void WithAnInvalidNetwork_FailsNamingTheKey_R_006_03()
    {
        var act = () => OptionsOf(["no-es-un-cidr"]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*ForwardedHeaders:KnownNetworks*");
    }

    [Fact]
    public async Task SpoofedHeaderFromAnUntrustedIp_IsIgnored_R_006_03()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.9");
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.7";

        await RunMiddlewareAsync(context, [_trustedNetwork]);

        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("203.0.113.9"), "la IP no confiable no puede suplantar la IP real");
    }

    [Fact]
    public async Task HeaderFromATrustedNetwork_IsAccepted_R_006_03()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.20.0.5");
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.7";

        await RunMiddlewareAsync(context, [_trustedNetwork]);

        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("198.51.100.7"), "el balanceador de confianza sí puede reenviar la IP real");
    }

    private static ForwardedHeadersOptions OptionsOf(IReadOnlyList<string> networks)
    {
        var values = new Dictionary<string, string?>();
        for (var i = 0; i < networks.Count; i++)
        {
            values[$"ForwardedHeaders:KnownNetworks:{i}"] = networks[i];
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        using var provider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddKnockForwardedHeaders()
            .BuildServiceProvider();
        return provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
    }

    private static async Task RunMiddlewareAsync(DefaultHttpContext context, IReadOnlyList<string> networks)
    {
        var middleware = new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            new StubOptions(OptionsOf(networks)));
        await middleware.Invoke(context);
    }

    private sealed class StubOptions(ForwardedHeadersOptions value) : IOptions<ForwardedHeadersOptions>
    {
        public ForwardedHeadersOptions Value { get; } = value;
    }
}
