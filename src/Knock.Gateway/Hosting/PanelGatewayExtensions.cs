using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Knock.Gateway.Hosting;

/// <summary>
/// Configuración del panel interno en el gateway (spec 033, R-14 y R-27): la llave propia de <c>PanelJwt:SigningKey</c> (Base64, al menos 32 bytes), el
/// destinatario <c>PanelJwt:Audience</c> (<c>knock-panel</c>), el mismo emisor que la app y el origen exacto <c>Cors:Panel:Origen</c> (https). La llave es
/// opcional: sin ella el panel queda apagado, ningún token del panel valida y las rutas del panel responden 401, sin impedir el arranque de la app.
/// </summary>
internal sealed record GatewayPanelSettings(string Issuer, string Audience, SymmetricSecurityKey SigningKey, string? Origin)
{
    public const string PolicyName = "panel";
    public const string Scheme = "PanelBearer";
    public const string CorsPolicyName = "panel";
    public const string DefaultAudience = "knock-panel";
    public const string MarkerHeader = "X-Knock-Panel";

    public static GatewayPanelSettings Create(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var issuer = configuration["Jwt:Issuer"];
        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException("Jwt:Issuer is required.");
        }

        var audience = string.IsNullOrWhiteSpace(configuration["PanelJwt:Audience"]) ? DefaultAudience : configuration["PanelJwt:Audience"]!;
        var configured = configuration["PanelJwt:SigningKey"];
        byte[] key;
        if (string.IsNullOrWhiteSpace(configured))
        {
            key = RandomNumberGenerator.GetBytes(GatewayJwtSettings.MinimumKeyBytes);
        }
        else
        {
            var buffer = new byte[configured.Length];
            if (!Convert.TryFromBase64String(configured, buffer, out var written))
            {
                throw new InvalidOperationException("PanelJwt:SigningKey must be Base64.");
            }

            if (written < GatewayJwtSettings.MinimumKeyBytes)
            {
                throw new InvalidOperationException($"PanelJwt:SigningKey must decode to at least {GatewayJwtSettings.MinimumKeyBytes} bytes.");
            }

            key = buffer[..written];
        }

        return new GatewayPanelSettings(issuer, audience, new SymmetricSecurityKey(key), NormalizeOrigin(configuration["Cors:Panel:Origen"]));
    }

    /// <summary>Solo <c>https://host[:puerto]</c> sin ruta, consulta, fragmento ni barra final; cualquier otra cosa deja el origen sin configurar (ninguno se permite).</summary>
    internal static string? NormalizeOrigin(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return null;
        }

        var onlyOrigin = uri.AbsolutePath == "/" && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment) && string.IsNullOrEmpty(uri.UserInfo) && value[^1] != '/';
        return onlyOrigin ? uri.GetLeftPart(UriPartial.Authority) : null;
    }
}

internal static class PanelGatewayExtensions
{
    /// <summary>Esquema JWT del panel, política <c>panel</c> para las rutas del panel y política CORS <c>panel</c> con credenciales solo para el origen del panel.</summary>
    public static IServiceCollection AddKnockGatewayPanel(this IServiceCollection services)
    {
        services.AddSingleton(provider => GatewayPanelSettings.Create(provider.GetRequiredService<IConfiguration>()));
        services.AddAuthentication().AddJwtBearer(GatewayPanelSettings.Scheme, _ => { });
        services.AddOptions<JwtBearerOptions>(GatewayPanelSettings.Scheme).Configure<GatewayPanelSettings>((options, settings) =>
        {
            // jscpd:ignore-start -- EXC-028
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
            // jscpd:ignore-end
            options.Events = new JwtBearerEvents
            {
                // Spec 033, R-14: las sesiones del panel se cancelan por sid igual que las de la app (sesion-cancelada:{sid}).
                OnTokenValidated = AuthenticationExtensions.RejectCancelledSessionAsync,
                OnChallenge = AuthenticationExtensions.ChallengeAsync,
            };
        });
        services.AddAuthorization(options => options.AddPolicy(
            GatewayPanelSettings.PolicyName,
            policy => policy.AddAuthenticationSchemes(GatewayPanelSettings.Scheme).RequireAuthenticatedUser()));
        services.AddOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>().Configure<GatewayPanelSettings>((options, settings) =>
            options.AddPolicy(GatewayPanelSettings.CorsPolicyName, policy =>
            {
                if (settings.Origin is not null)
                {
                    policy.WithOrigins(settings.Origin);
                }

                policy
                    .AllowCredentials()
                    .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                    .WithHeaders("Authorization", "Content-Type", "X-Correlation-Id", GatewayPanelSettings.MarkerHeader)
                    .WithExposedHeaders("X-Correlation-Id", "Retry-After")
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            }));
        return services;
    }
}

/// <summary>
/// Separa los tokens por ruta (spec 033, R-14): en <c>/api/v{n}/panel/**</c> solo vale un token con el destinatario del panel y en cualquier otra ruta no
/// vale uno del panel; el que no corresponde responde 401, también en rutas anónimas. En las del panel autentica con el esquema <c>PanelBearer</c> antes
/// del límite por usuario, para que éste cuente por el <c>sub</c> del token del panel.
/// </summary>
internal sealed class PanelTokenMiddleware(RequestDelegate next, GatewayPanelSettings panel)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var isPanelPath = RateLimitingExtensions.IsApiPath(context.Request.Path, "panel");
        var jwt = ReadBearer(context);
        if (jwt is not null && jwt.Audiences.Contains(panel.Audience, StringComparer.Ordinal) != isPanelPath)
        {
            await ProblemResponses.WriteAsync(context, StatusCodes.Status401Unauthorized, "Unauthorized");
            return;
        }

        if (isPanelPath && jwt is not null)
        {
            var result = await context.AuthenticateAsync(GatewayPanelSettings.Scheme);
            if (result.Succeeded)
            {
                context.User = result.Principal;
            }
        }

        await next(context);
    }

    /// <summary>Lee el token del encabezado sin validarlo, solo para conocer su destinatario; uno ilegible sigue el camino normal (la validación lo rechaza).</summary>
    private static JsonWebToken? ReadBearer(HttpContext context)
    {
        var header = context.Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = header[prefix.Length..].Trim();
        var handler = new JsonWebTokenHandler();
        return handler.CanReadToken(token) ? handler.ReadJsonWebToken(token) : null;
    }
}
