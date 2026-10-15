namespace Knock.Api.Modules.Notifications;

/// <summary>Contrato público del módulo Notifications: la única vía para que otros módulos lo usen (o por eventos).</summary>
public interface INotificationsApi
{
    string ModuleName { get; }
}
