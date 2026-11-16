using Microsoft.Extensions.Options;

namespace Knock.Identity.Api.Modules.Usuarios.Data;

/// <summary>Vigencia de la caché del perfil (spec 010, R-04): valor inicial de 60 segundos, ajustable por configuración y validada al arrancar.</summary>
internal sealed class ProfileCacheOptions
{
    public const string SectionName = "Cache";
    public const string LifetimeKey = "Cache:ProfileLifetimeSeconds";
    public const int DefaultLifetimeSeconds = 60;
    public const int MinimumLifetimeSeconds = 1;
    public const int MaximumLifetimeSeconds = 3600;

    /// <summary>Vida en segundos de cada clave <c>perfil:{sub}</c>; <c>null</c> si falta o no es un entero.</summary>
    public int? ProfileLifetimeSeconds { get; set; } = DefaultLifetimeSeconds;

    public TimeSpan Lifetime =>
        TimeSpan.FromSeconds(ProfileLifetimeSeconds ?? throw new InvalidOperationException($"{LifetimeKey} is not configured."));
}

/// <summary>Valida <see cref="ProfileCacheOptions"/> al arrancar. Los mensajes nombran la clave, nunca el valor.</summary>
internal sealed class ProfileCacheOptionsValidator : IValidateOptions<ProfileCacheOptions>
{
    // jscpd:ignore-start -- EXC-018
    public ValidateOptionsResult Validate(string? name, ProfileCacheOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.ProfileLifetimeSeconds is not (>= ProfileCacheOptions.MinimumLifetimeSeconds and <= ProfileCacheOptions.MaximumLifetimeSeconds))
        {
            return ValidateOptionsResult.Fail($"{ProfileCacheOptions.LifetimeKey} must be an integer from {ProfileCacheOptions.MinimumLifetimeSeconds} to {ProfileCacheOptions.MaximumLifetimeSeconds} (seconds).");
        }

        return ValidateOptionsResult.Success;
    }
    // jscpd:ignore-end -- EXC-018
}
