using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Requests.Data;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Services;
using Knock.Market.Api.Modules.Requests.Validators;

namespace Knock.Market.Api.Modules.Requests;

/// <summary>Registro del módulo Requests. Solo esta clase y <see cref="IRequestsApi"/> son públicas; el resto del módulo es internal.</summary>
public static class RequestsModuleExtensions
{
    /// <summary>Nombre del módulo y clave de su base (<c>Mongo:Databases:Requests</c>).</summary>
    public const string ModuleKey = "Requests";

    public static IServiceCollection AddRequestsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOptions<QuotePricingOptions>().BindConfiguration(QuotePricingOptions.SectionName);
        services.AddKnockMongoDatabase(ModuleKey);
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ITechnicianDirectory, MongoTechnicianDirectory>();
        services.AddScoped<IQuoteRequestReader, MongoQuoteRequestReader>();
        services.AddScoped<QuoteValidator>();
        services.AddScoped<INoteModerator, UnavailableNoteModerator>();
        services.AddScoped<IQuoteWriter, QuoteStore>();
        services.AddScoped<QuoteService>();
        services.AddScoped<IRequestsApi, RequestsApi>();
        services.AddScoped<DemoTechnicianStore>();
        services.AddScoped<DemoTechnicianSeeder>();
        services.AddHostedService<DemoTechnicianStartup>();
        return services;
    }
}
