using Knock.Identity.Api.Modules.Account.Data;
using Knock.Identity.Api.Modules.Account.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Knock.Identity.Api.Modules.Account;

/// <summary>Registro del módulo Account (spec 039: cuenta y datos personales). Solo esta clase es pública; el resto del módulo es internal.</summary>
public static class AccountModuleExtensions
{
    public static IServiceCollection AddAccountModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<AccountStore>();
        services.AddSingleton<IAccountStore>(provider => provider.GetRequiredService<AccountStore>());
        services.TryAddSingleton<IAccountOutbox, PendingAccountOutbox>();
        services.AddSingleton<PhoneChangeService>();
        return services;
    }
}
