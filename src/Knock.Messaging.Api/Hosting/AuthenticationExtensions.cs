// jscpd:ignore-start -- EXC-021: copia de src/Knock.Market.Api/Hosting/AuthenticationExtensions.cs (KNOCK-241) más la comprobación de sesión cancelada; se unifica en la biblioteca técnica de KNOCK-251.
using System.Security.Claims;
using Knock.Contracts.Sessions;
using Knock.Messaging.Api.Common.Errors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace Knock.Messaging.Api.Hosting;

/// <summary>Sesiones canceladas (logout o reuso de un token de renovación): solo lee <c>sesion-cancelada:{sid}</c> en Redis; quien escribe es Identidad.</summary>
internal interface IRevokedSessions
{
    Task<bool> IsRevokedAsync(string sessionId, CancellationToken cancellationToken);
}

/// <summary>Consulta la misma clave que escribe Identidad (<see cref="SessionRevocationKey.Prefix"/>).</summary>
internal sealed class RedisRevokedSessions(IConnectionMultiplexer redis) : IRevokedSessions
{
    public async Task<bool> IsRevokedAsync(string sessionId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        cancellationToken.ThrowIfCancellationRequested();
        return await redis.GetDatabase().KeyExistsAsync(SessionRevocationKey.Prefix + sessionId);
    }
}

internal static class AuthenticationExtensions
{
    public const string SessionIdClaim = "sid";

    /// <summary>Clave de <c>HttpContext.Items</c> que marca que Redis no respondió al revisar la sesión: se responde 503 y no 401.</summary>
    internal const string RevocationUnavailableItem = "knock.revocation-unavailable";

    /// <summary>
    /// JWT propio (BUG-004): HS256, <c>ClockSkew</c> cero, <c>iss</c> y <c>aud</c> de configuración, 401 uniforme en ProblemDetails, sesión cancelada en Redis
    /// (si Redis no responde, 503 y no pasa: falla cerrada) y política por defecto «exigir sesión»: lo que no declare <c>[AllowAnonymous]</c> la exige.
    /// </summary>
    public static IServiceCollection AddKnockAuthentication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddKnockRedis();
        services.AddSingleton<IRevokedSessions, RedisRevokedSessions>();
        services.AddSingleton(sp => JwtSettingsFactory.Create(
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<IHostEnvironment>()));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtSettings>((options, settings) =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = settings.SigningKey,
                    RequireSignedTokens = true,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.Zero,
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = AcceptHubTokenFromQuery,
                    OnTokenValidated = RejectRevokedSessionAsync,

                    // Mismo 401 para cualquier fallo: sin motivo en el cuerpo ni en WWW-Authenticate.
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        if (context.HttpContext.Items.ContainsKey(RevocationUnavailableItem))
                        {
                            // Sin Redis no se sabe si la sesión sigue viva: se cierra el paso con 503 reintentable, como el gateway.
                            context.Response.Headers.RetryAfter = "5";
                            await ProblemResponses.WriteAsync(context.HttpContext, StatusCodes.Status503ServiceUnavailable, "Service Unavailable");
                            return;
                        }

                        await ProblemResponses.WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized, "Unauthorized", ErrorCodes.UnauthorizedCode);
                    },
                };
            });
        services.AddAuthorization(options =>
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        services.AddHostedService<AuthenticationStartupCheck>();
        return services;
    }

    /// <summary>
    /// Spec 026, R-02: el cliente WebSocket no puede mandar el encabezado <c>Authorization</c> en el saludo, así que para el hub (y solo para esa ruta) el token puede venir en la
    /// consulta <c>access_token</c>. El gateway lo convierte en encabezado y lo quita de la URL; esto cubre llamar al servicio directo en desarrollo. Nunca se registra.
    /// </summary>
    private static Task AcceptHubTokenFromQuery(MessageReceivedContext context)
    {
        if (string.IsNullOrEmpty(context.Token)
            && context.HttpContext.Request.Path.StartsWithSegments(Knock.Messaging.Api.Modules.Chat.ChatModuleExtensions.HubPath, StringComparison.OrdinalIgnoreCase)
            && context.HttpContext.Request.Query.TryGetValue("access_token", out var token)
            && !string.IsNullOrWhiteSpace(token))
        {
            context.Token = token.ToString();
        }

        return Task.CompletedTask;
    }

    /// <summary>Exige <c>sub</c> y <c>sid</c>; si la sesión está cancelada el token se rechaza (401); si Redis no responde también no pasa, pero con 503 (falla cerrada).</summary>
    private static async Task RejectRevokedSessionAsync(TokenValidatedContext context)
    {
        var sessionId = context.Principal?.FindFirstValue(SessionIdClaim);
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(context.Principal?.FindFirstValue("sub")))
        {
            context.Fail("missing claims");
            return;
        }

        try
        {
            var revoked = context.HttpContext.RequestServices.GetRequiredService<IRevokedSessions>();
            if (await revoked.IsRevokedAsync(sessionId, context.HttpContext.RequestAborted))
            {
                context.Fail("session revoked");
            }
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException)
        {
            context.HttpContext.Items[RevocationUnavailableItem] = true;
            context.Fail("revocation list unavailable");
        }
    }
}

/// <summary>Al arrancar valida la configuración de autenticación (falla rápido): una llave ausente o corta impide el arranque.</summary>
internal sealed class AuthenticationStartupCheck(IServiceProvider services) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = services.GetRequiredService<JwtSettings>();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
// jscpd:ignore-end -- EXC-021
