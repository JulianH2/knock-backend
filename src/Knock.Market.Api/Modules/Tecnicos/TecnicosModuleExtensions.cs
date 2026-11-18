using Knock.Market.Api.Common.Crypto;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Tecnicos.Data;
using Knock.Market.Api.Modules.Tecnicos.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Knock.Market.Api.Modules.Tecnicos;

/// <summary>Registro del módulo Tecnicos (spec 046). Solo esta clase es pública; el resto del módulo es internal.</summary>
public static class TecnicosModuleExtensions
{
    /// <summary>Nombre del módulo y clave de su base (<c>Mongo:Databases:Tecnicos</c>).</summary>
    public const string ModuleKey = "Tecnicos";

    public static IServiceCollection AddTecnicosModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddKnockMongoDatabase(ModuleKey);
        services.AddOptions<FieldEncryptionOptions>().BindConfiguration(FieldEncryptionOptions.SectionName);
        services.TryAddSingleton<FieldCipher>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ITechnicianProfileStore, TechnicianProfileStore>();
        services.AddScoped<TechnicianRegistrationService>();
        services.AddMemoryCache();
        services.AddScoped<ITechnicianProfileReader, TechnicianProfileStore>();
        services.AddScoped<TechnicianProfileService>();
        return services;
    }
}
