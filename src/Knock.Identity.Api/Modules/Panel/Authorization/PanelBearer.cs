using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Panel.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace Knock.Identity.Api.Modules.Panel.Authorization;

/// <summary>
/// Esquema de autenticación <c>PanelBearer</c> (spec 033, §7): valida solo tokens del panel (llave <c>PanelJwt:SigningKey</c>, destinatario <c>knock-panel</c>,
/// HMAC-SHA256, sin tolerancia de reloj) y rechaza los de una sesión cancelada en Redis (<c>sesion-cancelada:{sid}</c>). Se aplica solo a los controllers del
/// panel: el token de la app no se acepta ahí, y el del panel no se acepta en el esquema de la app (otra llave y otro destinatario).
/// </summary>
internal static class PanelBearer
{
    public const string Scheme = "PanelBearer";

    public static void Configure(JwtBearerOptions options, PanelSettings settings)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settings);
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
            OnTokenValidated = RejectCancelledSessionAsync,
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await ProblemResponses.WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized, "Unauthorized");
            },
        };
    }

    private static async Task RejectCancelledSessionAsync(TokenValidatedContext context)
    {
        var settings = context.HttpContext.RequestServices.GetRequiredService<PanelSettings>();
        var sid = context.Principal?.FindFirst("sid")?.Value;
        var sub = context.Principal?.FindFirst("sub")?.Value;
        if (!settings.IsConfigured || string.IsNullOrWhiteSpace(sid) || string.IsNullOrWhiteSpace(sub) || context.Principal?.FindFirst("pv") is null)
        {
            context.Fail("invalid");
            return;
        }

        try
        {
            var redis = context.HttpContext.RequestServices.GetRequiredService<IConnectionMultiplexer>();
            if (await redis.GetDatabase().KeyExistsAsync("sesion-cancelada:" + sid))
            {
                context.Fail("cancelled");
            }
        }
        catch (RedisException)
        {
            throw PanelInfra.Unavailable();
        }
    }
}
