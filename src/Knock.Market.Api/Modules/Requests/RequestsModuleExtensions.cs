using Knock.Contracts.Moderation;
using Knock.Market.Api.Common.Crypto;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Requests.Data;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Services;
using Knock.Market.Api.Modules.Requests.Validators;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Knock.Market.Api.Modules.Requests;

/// <summary>Registro del módulo Requests. Solo esta clase y <see cref="IRequestsApi"/> son públicas; el resto del módulo es internal.</summary>
public static class RequestsModuleExtensions
{
    /// <summary>Nombre del módulo y clave de su base (<c>Mongo:Databases:Requests</c>).</summary>
    public const string ModuleKey = "Requests";

    public static IServiceCollection AddRequestsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOptions<OleadasOptions>()
            .BindConfiguration(OleadasOptions.SectionName)
            .Validate(options => options.IsValid(), "Oleadas values must be positive and MejorOrdenados + 2 must not exceed TamanoOleada.")
            .ValidateOnStart();
        services.AddOptions<QuotePricingOptions>().BindConfiguration(QuotePricingOptions.SectionName);
        services.AddKnockMongoDatabase(ModuleKey);
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ITechnicianDirectory, MongoTechnicianDirectory>();
        services.AddScoped<IQuoteRequestReader, MongoQuoteRequestReader>();
        services.AddScoped<QuoteValidator>();
        services.AddScoped<INoteModerator, UnavailableNoteModerator>();
        services.AddScoped<IQuoteWriter, QuoteStore>();
        services.AddScoped<QuoteStore>();
        services.AddScoped<MyQuotesService>();
        services.AddScoped<QuoteService>();
        services.AddOptions<FieldEncryptionOptions>().BindConfiguration(FieldEncryptionOptions.SectionName);
        services.AddSingleton<FieldCipher>();
        services.AddScoped<IOutbox, MongoOutbox>();
        services.AddScoped<RequestsStore>();
        services.AddScoped<CierreDeSolicitudStore>();
        services.AddScoped<CierreDeSolicitudService>();
        // Fotos y moderación: hasta que lleguen los servicios reales (spec 013, OI-2) fallan cerrado; las pruebas los sustituyen por dobles.
        services.TryAddScoped<IRequestPhotos, UnavailableRequestPhotos>();
        services.TryAddSingleton<IContentModerator, UnavailableContentModerator>();
        services.AddScoped<SolicitudService>();
        services.AddSingleton<VisitCodeFactory>();
        services.AddScoped<ChoiceStore>();
        services.AddScoped<HistoryStore>();
        services.AddScoped<HistoryService>();
        services.AddScoped<QuoteComparisonService>();
        services.AddScoped<ConversationPermissionStore>();
        services.AddScoped<ConversationPermissionService>();
        services.AddScoped<ChooseQuoteService>();
        services.AddScoped<VisitStore>();
        services.AddScoped<VisitService>();
        services.AddScoped<ArrivalService>();
        services.AddScoped<TrustNoticeService>();
        services.AddScoped<IRequestsApi, RequestsApi>();
        services.AddScoped<DemoTechnicianStore>();
        services.AddScoped<DemoTechnicianSeeder>();
        services.AddScoped<DemoRequestStore>();
        services.AddScoped<DemoRequestSeeder>();
        services.AddHostedService<DemoTechnicianStartup>();
        return services;
    }
}
