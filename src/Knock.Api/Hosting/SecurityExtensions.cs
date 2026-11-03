using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Options;

namespace Knock.Api.Hosting;

internal static class SecurityExtensions
{
    /// <summary>
    /// Opciones de <c>Security:*</c> (validadas al arrancar, sin valores por defecto en código), HSTS sin <c>preload</c>, redirección 308 a HTTPS
    /// (solo si hay <c>Security:Https:RedirectPort</c>) y Kestrel sin el encabezado <c>Server</c>.
    /// </summary>
    public static WebApplicationBuilder AddKnockSecurity(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);
        builder.Services.AddOptions<SecuritySettings>()
            .BindConfiguration(SecuritySettings.SectionName)
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<SecuritySettings>, SecuritySettingsValidator>();
        builder.Services.AddOptions<HstsOptions>().Configure<IOptions<SecuritySettings>>((hsts, settings) =>
        {
            hsts.MaxAge = settings.Value.Hsts.MaxAge;
            hsts.IncludeSubDomains = settings.Value.Hsts.IncludeSubDomains;
            hsts.Preload = false;
        });
        builder.Services.AddOptions<HttpsRedirectionOptions>().Configure<IOptions<SecuritySettings>>((redirection, settings) =>
        {
            redirection.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
            redirection.HttpsPort = settings.Value.Https.RedirectPort;
        });
        return builder;
    }
}
