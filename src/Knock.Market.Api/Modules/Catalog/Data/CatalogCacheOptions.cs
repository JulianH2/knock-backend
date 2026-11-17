using Microsoft.Extensions.Options;

namespace Knock.Market.Api.Modules.Catalog.Data;

/// <summary>Vigencia de la caché del catálogo en Redis, solo del servidor; no se usa en <c>Cache-Control</c> (spec 010, R-04): valor inicial de 24 horas, ajustable por configuración y validada al arrancar.</summary>
internal sealed class CatalogCacheOptions
{
    public const string SectionName = "Cache";
    public const string LifetimeKey = "Cache:CatalogLifetimeSeconds";
    public const int DefaultLifetimeSeconds = 86400;
    public const int MinimumLifetimeSeconds = 60;
    public const int MaximumLifetimeSeconds = 604800;

    /// <summary>Vida en segundos de cada clave del catálogo; <c>null</c> si falta o no es un entero.</summary>
    public int? CatalogLifetimeSeconds { get; set; } = DefaultLifetimeSeconds;

    public TimeSpan Lifetime =>
        TimeSpan.FromSeconds(CatalogLifetimeSeconds ?? throw new InvalidOperationException($"{LifetimeKey} is not configured."));
}

/// <summary>Valida <see cref="CatalogCacheOptions"/> al arrancar. Los mensajes nombran la clave, nunca el valor.</summary>
internal sealed class CatalogCacheOptionsValidator : IValidateOptions<CatalogCacheOptions>
{
    // jscpd:ignore-start -- EXC-018
    public ValidateOptionsResult Validate(string? name, CatalogCacheOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.CatalogLifetimeSeconds is not (>= CatalogCacheOptions.MinimumLifetimeSeconds and <= CatalogCacheOptions.MaximumLifetimeSeconds))
        {
            return ValidateOptionsResult.Fail($"{CatalogCacheOptions.LifetimeKey} must be an integer from {CatalogCacheOptions.MinimumLifetimeSeconds} to {CatalogCacheOptions.MaximumLifetimeSeconds} (seconds).");
        }

        return ValidateOptionsResult.Success;
    }
    // jscpd:ignore-end -- EXC-018
}
