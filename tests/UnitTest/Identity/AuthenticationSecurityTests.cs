using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.Identity.Api.Common.Security;
using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Sesiones.Controllers.V1;
using Knock.Identity.Api.Modules.Sesiones.Data;
using Knock.Identity.Api.Modules.Sesiones.Domain;
using Knock.Identity.Api.Modules.Sesiones.Services;
using Knock.Identity.Api.Modules.Usuarios.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Knock.UnitTest.Identity;

/// <summary>
/// Spec 009 (R-05) y specs 016 y 017: configuración del JWT, validación de la credencial, guardia de arranque, límite de cuerpo, clave del limitador y
/// controllers de renovación y cierre. Sin servidor ni contenedores; solo datos ficticios.
/// </summary>
public class AuthenticationSecurityTests
{
    private static readonly string _goodKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private sealed class Env(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;

        public string ApplicationName { get; set; } = "test";

        public string ContentRootPath { get; set; } = ".";

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(v => v.Key, v => v.Value)).Build();

    private static IConfiguration ValidConfig(string? key = null) =>
        Config(("Jwt:Issuer", "knock-api"), ("Jwt:Audience", "knock-app"), ("Jwt:SigningKey", key ?? _goodKey), ("Firebase:ProjectId", "demo-knock"));

    [Fact]
    public void ValidSettings_CreateTheKey_SEC_009_11()
    {
        var settings = JwtSettingsFactory.Create(ValidConfig(), new Env(Environments.Production));

        settings.Issuer.Should().Be("knock-api");
        settings.Audience.Should().Be("knock-app");
    }

    [Theory]
    [InlineData("Jwt:Issuer")]
    [InlineData("Jwt:Audience")]
    public void MissingIssuerOrAudience_StopsTheStartup_SEC_009_12(string missing)
    {
        var values = new Dictionary<string, string?> { ["Jwt:Issuer"] = "knock-api", ["Jwt:Audience"] = "knock-app", ["Jwt:SigningKey"] = _goodKey };
        values.Remove(missing);

        var act = () => JwtSettingsFactory.Create(new ConfigurationBuilder().AddInMemoryCollection(values).Build(), new Env(Environments.Production));

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{missing}*");
    }

    [Theory]
    [InlineData(16)]
    [InlineData(31)]
    public void KeyShorterThan32Bytes_IsRefused_AndTheMessageNeverRepeatsIt_SEC_009_13(int bytes)
    {
        var shortKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes));

        var act = () => JwtSettingsFactory.Create(ValidConfig(shortKey), new Env(Environments.Production));

        act.Should().Throw<InvalidOperationException>().Where(e => !e.Message.Contains(shortKey, StringComparison.Ordinal));
    }

    [Fact]
    public void KeyThatIsNotBase64_IsRefused_SEC_009_13()
    {
        var act = () => JwtSettingsFactory.Create(ValidConfig("esto no es base64 !!"), new Env(Environments.Production));

        act.Should().Throw<InvalidOperationException>().WithMessage("*Base64*");
    }

    [Theory]
    [InlineData("Production", false)]
    [InlineData("Staging", false)]
    [InlineData("Development", true)]
    public void MissingKey_IsOnlyToleratedInDevelopment_SEC_009_14(string environment, bool tolerated)
    {
        var configuration = Config(("Jwt:Issuer", "knock-api"), ("Jwt:Audience", "knock-app"));

        var act = () => JwtSettingsFactory.Create(configuration, new Env(environment));

        if (tolerated)
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<InvalidOperationException>();
        }
    }

    [Fact]
    public void BearerValidation_PinsOneAlgorithmZeroSkewAndEveryCheck_SEC_009_15()
    {
        var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton(ValidConfig())
            .AddSingleton<IHostEnvironment>(new Env(Environments.Production))
            .AddKnockProblemDetails()
            .AddKnockAuthentication()
            .BuildServiceProvider();

        var parameters = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters;

        parameters.ValidAlgorithms.Should().BeEquivalentTo(SecurityAlgorithms.HmacSha256);
        parameters.ClockSkew.Should().Be(TimeSpan.Zero);
        parameters.ValidateIssuer.Should().BeTrue();
        parameters.ValidateAudience.Should().BeTrue();
        parameters.ValidateLifetime.Should().BeTrue();
        parameters.ValidateIssuerSigningKey.Should().BeTrue();
        parameters.RequireSignedTokens.Should().BeTrue();
        parameters.RequireExpirationTime.Should().BeTrue();
        parameters.ValidIssuer.Should().Be("knock-api");
        parameters.ValidAudience.Should().Be("knock-app");
        provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme).MapInboundClaims.Should().BeFalse();
    }

    [Fact]
    public async Task StartupGuard_RefusesTheFirebaseEmulatorOutsideDevelopment_SEC_009_16()
    {
        var provider = new ServiceCollection().AddSingleton(JwtSettingsFactory.Create(ValidConfig(), new Env(Environments.Production))).BuildServiceProvider();
        var withEmulator = Config(("Jwt:Issuer", "i"), ("Jwt:Audience", "a"), ("Jwt:SigningKey", _goodKey), ("Firebase:ProjectId", "p"), (AuthenticationExtensions.EmulatorHostVariable, "127.0.0.1:9099"));

        var production = new AuthenticationStartupCheck(provider, withEmulator, new Env(Environments.Production));
        var development = new AuthenticationStartupCheck(provider, withEmulator, new Env(Environments.Development));

        var refuse = async () => await production.StartAsync(CancellationToken.None);
        await refuse.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*{AuthenticationExtensions.EmulatorHostVariable}*");
        var allow = async () => await development.StartAsync(CancellationToken.None);
        await allow.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartupGuard_RefusesAMissingFirebaseProject_SEC_009_16()
    {
        var configuration = Config(("Jwt:Issuer", "i"), ("Jwt:Audience", "a"), ("Jwt:SigningKey", _goodKey));
        var provider = new ServiceCollection().AddSingleton(JwtSettingsFactory.Create(configuration, new Env(Environments.Production))).BuildServiceProvider();
        var check = new AuthenticationStartupCheck(provider, configuration, new Env(Environments.Production));

        var act = async () => await check.StartAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Firebase:ProjectId*");
    }

    private static (DefaultHttpContext Context, MemoryStream Body) HttpWithProblemDetails(long? contentLength, int? limit)
    {
        var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        var body = new MemoryStream();
        context.Response.Body = body;
        context.Request.ContentLength = contentLength;
        if (limit is { } max)
        {
            context.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(new RequestSizeLimitAttribute(max)), "test"));
        }

        return (context, body);
    }

    [Theory]
    [InlineData(1025, 1024, true)]
    [InlineData(1024, 1024, false)]
    [InlineData(0, 1024, false)]
    public async Task BodyLimit_RejectsOnlyWhatExceedsTheDeclaredLimitBeforeReading_SEC_016_21(long length, int limit, bool rejected)
    {
        var (context, body) = HttpWithProblemDetails(length, limit);
        var nextCalled = false;
        var middleware = new RequestBodyLimitMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextCalled.Should().Be(!rejected);
        context.Response.StatusCode.Should().Be(rejected ? StatusCodes.Status413PayloadTooLarge : StatusCodes.Status200OK);
        (body.Length > 0).Should().Be(rejected);
    }

    [Fact]
    public async Task BodyLimit_LeavesEndpointsWithoutALimitAlone_SEC_016_21()
    {
        var (context, _) = HttpWithProblemDetails(contentLength: 10_000_000, limit: null);
        var nextCalled = false;

        await new RequestBodyLimitMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }).InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task BodyLimit_TranslatesAStreamedOverflowTo413_SEC_016_21()
    {
        var (context, _) = HttpWithProblemDetails(contentLength: null, limit: 8);
        var middleware = new RequestBodyLimitMiddleware(_ => throw new BadHttpRequestException("too big", StatusCodes.Status413PayloadTooLarge));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status413PayloadTooLarge);
    }

    [Fact]
    public void RateLimitKey_NormalizesMappedIpv4AndGroupsIpv6By64_SEC_009_17()
    {
        static string Key(string? ip) => RateLimitingExtensions.ClientKey(new DefaultHttpContext { Connection = { RemoteIpAddress = ip is null ? null : IPAddress.Parse(ip) } });

        Key("203.0.113.9").Should().Be("203.0.113.9");
        Key("::ffff:203.0.113.9").Should().Be("203.0.113.9", "IPv4 mapeada y directa comparten contador");
        Key("2001:db8:1:2:aaaa:bbbb:cccc:dddd").Should().Be(Key("2001:db8:1:2:1:2:3:4"), "rotar dentro de un /64 no evade el límite");
        Key("2001:db8:1:3::1").Should().NotBe(Key("2001:db8:1:2::1"));
        Key(null).Should().Be("unknown");
    }

    [Fact]
    public void RedisKeysAndLifetimes_FollowTheSpecs_SEC_016_22()
    {
        RedisSesionCancelada.Key("665f1b2c3d4e5f6071829304").Should().Be("sesion-cancelada:665f1b2c3d4e5f6071829304");
        RedisProfileCache.Key("0f8e7d6c-5b4a-8392-a1b0-c9d8e7f6a5b4").Should().Be("perfil:0f8e7d6c-5b4a-8392-a1b0-c9d8e7f6a5b4");
        new ProfileCacheOptions().Lifetime.Should().Be(TimeSpan.FromSeconds(60));
        LogoutService.MaxCancellation.Should().Be(TimeSpan.FromMinutes(15));
        RefreshController.MaxBodyBytes.Should().Be(1024);
        AuthController.MaxBodyBytes.Should().Be(8192);
    }

    private sealed class RecordingCancelled : ISesionCancelada
    {
        public List<(string Sid, TimeSpan Lifetime)> Entries { get; } = [];

        public Task CancelAsync(string sessionId, TimeSpan lifetime, CancellationToken cancellationToken)
        {
            Entries.Add((sessionId, lifetime));
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static T WithContext<T>(T controller, IEnumerable<Claim> claims)
        where T : ControllerBase
    {
        var services = new ServiceCollection().AddLogging().AddControllers().Services.BuildServiceProvider();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = services, User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) },
        };
        return controller;
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task Logout_WithoutSidOrSub_Returns401AndRevokesNothing_SEC_016_23(bool hasSid, bool hasSub)
    {
        var store = new FakeSesionesStore();
        var cancelled = new RecordingCancelled();
        var now = DateTimeOffset.UtcNow;
        var claims = new List<Claim> { new("exp", now.AddMinutes(5).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture)) };
        if (hasSid)
        {
            claims.Add(new Claim("sid", "665f1b2c3d4e5f6071829304"));
        }

        if (hasSub)
        {
            claims.Add(new Claim("sub", "0f8e7d6c-5b4a-8392-a1b0-c9d8e7f6a5b4"));
        }

        var controller = WithContext(new LogoutController(new LogoutService(store, cancelled, NullLogger<LogoutService>.Instance), new FixedTime(now)), claims);

        var result = await controller.Logout(CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        cancelled.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Logout_CancelsTheSidForWhatIsLeftOfTheJwt_SEC_016_23()
    {
        var store = new FakeSesionesStore();
        var cancelled = new RecordingCancelled();
        var now = new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);
        // La sesión es de esa cuenta: desde KNOCK-203 (H-1) el logout solo cancela el sid de una sesión que le pertenece.
        await store.AddSessionAsync(
            new NewSession("665f1b2c3d4e5f6071829304", "user-1", "0f8e7d6c-5b4a-8392-a1b0-c9d8e7f6a5b4", "device-1", "Teléfono de prueba", SHA256.HashData(new byte[RefreshTokenFingerprint.TokenBytes]), now.UtcDateTime),
            CancellationToken.None);
        var claims = new[]
        {
            new Claim("sid", "665f1b2c3d4e5f6071829304"),
            new Claim("sub", "0f8e7d6c-5b4a-8392-a1b0-c9d8e7f6a5b4"),
            new Claim("exp", now.AddMinutes(7).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture)),
        };
        var controller = WithContext(new LogoutController(new LogoutService(store, cancelled, NullLogger<LogoutService>.Instance), new FixedTime(now)), claims);

        var result = await controller.Logout(CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        cancelled.Entries.Should().ContainSingle().Which.Lifetime.Should().Be(TimeSpan.FromMinutes(7));
    }

    [Theory]
    [InlineData("")]
    [InlineData("corto")]
    [InlineData("' OR '1'='1")]
    public async Task Refresh_AnyRejectedToken_GivesTheSameProblem_SEC_016_24(string token)
    {
        var time = new FixedTime(DateTimeOffset.UtcNow);
        var issuer = new SessionTokenIssuer(new JwtSettings("knock-api", "knock-app", new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32))), time);
        var service = new SessionRefreshService(new FakeSesionesStore(), new RecordingCancelled(), issuer, time, NullLogger<SessionRefreshService>.Instance);
        var controller = WithContext(new RefreshController(service), []);

        var result = await controller.Refresh(new Knock.Identity.Api.Modules.Sesiones.Dtos.RefreshRequest(token), CancellationToken.None);

        var problem = result.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        var details = problem.Value.Should().BeOfType<ProblemDetails>().Subject;
        details.Detail.Should().Be("Invalid credentials.");
        details.Title.Should().Be("Unauthorized");
    }
}
