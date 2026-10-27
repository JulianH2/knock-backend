using Knock.Api.Common.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.IdentityModel.Tokens;

namespace Knock.Api.Hosting;

internal static class AuthenticationExtensions
{
    public const string EmulatorHostVariable = "FIREBASE_AUTH_EMULATOR_HOST";

    /// <summary>JWT propio: HS256, <c>ClockSkew</c> cero, <c>iss</c> y <c>aud</c> de configuración, 401 uniforme en ProblemDetails.</summary>
    public static IServiceCollection AddKnockAuthentication(this IServiceCollection services)
    {
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
                    // Mismo 401 para cualquier fallo: sin motivo en el cuerpo ni en WWW-Authenticate.
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        await ProblemResponses.WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized, "Unauthorized");
                    },
                };
            });
        services.AddAuthorization();
        services.AddHostedService<AuthenticationStartupCheck>();
        return services;
    }

    /// <summary>Los controllers de los módulos son <c>internal</c>: MVC solo descubre públicos, así que se amplía el descubrimiento.</summary>
    public static IMvcBuilder AddInternalControllers(this IMvcBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ConfigureApplicationPartManager(manager =>
        {
            foreach (var provider in manager.FeatureProviders.Where(p => p.GetType() == typeof(ControllerFeatureProvider)).ToList())
            {
                manager.FeatureProviders.Remove(provider);
            }

            manager.FeatureProviders.Add(new InternalControllerFeatureProvider());
        });
    }
}

/// <summary>Descubre los controllers públicos (como MVC) y también los <c>internal</c> del ensamblado de la API.</summary>
internal sealed class InternalControllerFeatureProvider : ControllerFeatureProvider
{
    protected override bool IsController(System.Reflection.TypeInfo typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        return base.IsController(typeInfo) || IsInternalController(typeInfo);
    }

    private static bool IsInternalController(System.Reflection.TypeInfo typeInfo) =>
        typeInfo.Assembly == typeof(InternalControllerFeatureProvider).Assembly
        && typeInfo is { IsClass: true, IsAbstract: false, IsNotPublic: true, IsGenericType: false }
        && typeof(ControllerBase).IsAssignableFrom(typeInfo)
        && !typeInfo.IsDefined(typeof(NonControllerAttribute), inherit: true)
        && typeInfo.Name.EndsWith("Controller", StringComparison.Ordinal);
}

/// <summary>Al arrancar valida la configuración de autenticación (falla rápido) y el guardia del emulador de Firebase.</summary>
internal sealed class AuthenticationStartupCheck(IServiceProvider services, IConfiguration configuration, IHostEnvironment environment) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() && !string.IsNullOrWhiteSpace(configuration[AuthenticationExtensions.EmulatorHostVariable]))
        {
            throw new InvalidOperationException(
                $"{AuthenticationExtensions.EmulatorHostVariable} is set outside Development: the Firebase emulator issues unsigned tokens, so the API refuses to start.");
        }

        // Resolver la configuración aquí hace que una clave ausente o corta impida el arranque.
        _ = services.GetRequiredService<JwtSettings>();
        if (string.IsNullOrWhiteSpace(configuration["Firebase:ProjectId"]))
        {
            throw new InvalidOperationException("Firebase:ProjectId is required.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
