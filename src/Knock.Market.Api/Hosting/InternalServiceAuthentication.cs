using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using System.Security.Cryptography;
using Knock.Market.Api.Common.Errors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Knock.Market.Api.Hosting;

/// <summary>
/// Autenticación de servicio a servicio de las rutas internas de Mercado (spec 041, R-17, con el esquema de la spec 037, R-11): token HS256 firmado con una clave por emisor
/// (<c>Security:InternalTokens:IdentityKey</c>, el secreto <c>interno-key-identity</c>), <c>iss</c> <c>knock-identity</c>, <c>aud</c> <c>knock-market</c>, <c>exp</c> de 5 minutos como máximo,
/// <c>jti</c> y el claim <c>permisos</c>. No reutiliza <c>jwt-signing-key</c>, la clave de los tokens de usuario. Sin clave válida ninguna ruta interna acepta nada (falla cerrado).
/// </summary>
internal static class InternalServiceAuthentication
{
    public const string Scheme = "InternalService";
    public const string HistoryPolicy = "InternalHistory";
    public const string KeyConfigurationKey = "Security:InternalTokens:IdentityKey";
    public const string Issuer = "knock-identity";
    public const string Audience = "knock-market";
    public const string PermissionsClaim = "permisos";
    public const string HistoryPermission = "historial";
    public const string ServiceNotAllowedCode = "service_not_allowed";

    /// <summary>Un token de servicio dura 5 minutos como máximo (spec 037, R-11).</summary>
    public const int MaxLifetimeSeconds = 300;

    /// <summary>Longitud mínima de la clave HMAC: 32 bytes (256 bits).</summary>
    public const int MinKeyBytes = 32;

    public static AuthenticationBuilder AddInternalServiceScheme(this AuthenticationBuilder builder) =>
        builder.AddJwtBearer(Scheme, _ => { });

    public static IServiceCollection AddKnockInternalServiceAuthentication(this IServiceCollection services)
    {
        services.AddOptions<JwtBearerOptions>(Scheme).Configure<IConfiguration>((options, configuration) =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(ReadKey(configuration[KeyConfigurationKey])),
                RequireSignedTokens = true,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ClockSkew = TimeSpan.Zero,
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    if (!HasBoundedLifetime(context.Principal))
                    {
                        context.Fail("The service token lifetime is not valid.");
                    }

                    return Task.CompletedTask;
                },
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    await ProblemResponses.WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized, "Unauthorized", ErrorCodes.UnauthorizedCode);
                },
                OnForbidden = context => ProblemResponses.WriteAsync(context.HttpContext, StatusCodes.Status403Forbidden, "Forbidden", ServiceNotAllowedCode),
            };
        });
        services.AddAuthorizationBuilder().AddPolicy(
            HistoryPolicy,
            policy => policy.AddAuthenticationSchemes(Scheme).RequireAuthenticatedUser().RequireClaim(PermissionsClaim, HistoryPermission));
        return services;
    }

    /// <summary>La clave de la configuración si es Base64 de al menos 32 bytes; si no, una clave al azar que nadie conoce, así que ningún token se valida.</summary>
    internal static byte[] ReadKey(string? configured)
    {
        if (!string.IsNullOrWhiteSpace(configured))
        {
            try
            {
                var key = Convert.FromBase64String(configured);
                if (key.Length >= MinKeyBytes)
                {
                    return key;
                }
            }
            catch (FormatException)
            {
                // Una clave mal escrita deja las rutas internas cerradas; el mensaje nunca la repite.
            }
        }

        return RandomNumberGenerator.GetBytes(MinKeyBytes);
    }

    /// <summary>El token trae <c>jti</c> e <c>iat</c> y vive 5 minutos o menos (<c>exp</c> menos <c>iat</c>).</summary>
    private static bool HasBoundedLifetime(System.Security.Claims.ClaimsPrincipal? principal)
    {
        if (principal?.FindFirst("jti") is not { Value.Length: > 0 }
            || !long.TryParse(principal.FindFirst("iat")?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var issuedAt)
            || !long.TryParse(principal.FindFirst("exp")?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var expires))
        {
            return false;
        }

        return expires - issuedAt is > 0 and <= MaxLifetimeSeconds;
    }
}
