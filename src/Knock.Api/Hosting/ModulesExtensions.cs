using Knock.Api.Modules.Assistant;
using Knock.Api.Modules.Catalog;
using Knock.Api.Modules.Identity;
using Knock.Api.Modules.Media;
using Knock.Api.Modules.Messaging;
using Knock.Api.Modules.Notifications;
using Knock.Api.Modules.Reputation;
using Knock.Api.Modules.Requests;
using Knock.Api.Modules.Trust;

namespace Knock.Api.Hosting;

internal static class ModulesExtensions
{
    public static IServiceCollection AddKnockModules(this IServiceCollection services)
    {
        return services
            .AddIdentityModule()
            .AddCatalogModule()
            .AddRequestsModule()
            .AddMediaModule()
            .AddMessagingModule()
            .AddReputationModule()
            .AddTrustModule()
            .AddNotificationsModule()
            .AddAssistantModule();
    }
}
