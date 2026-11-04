using Knock.Api.Hosting;
namespace Knock.Api.Modules.Identity.Data;

/// <summary>Registro de la capa de datos de Identity: la base del módulo del cliente compartido de Mongo y su acceso a las colecciones.</summary>
internal static class IdentityDataExtensions
{
    /// <summary>Nombre del módulo y clave de su base (<c>Mongo:Databases:Identity</c>).</summary>
    public const string ModuleKey = "Identity";

    public static IServiceCollection AddIdentityData(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddKnockMongoDatabase(ModuleKey);
        services.AddSingleton<IdentityStore>();
        return services;
    }
}
