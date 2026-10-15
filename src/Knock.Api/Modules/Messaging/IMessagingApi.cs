namespace Knock.Api.Modules.Messaging;

/// <summary>Contrato público del módulo Messaging: la única vía para que otros módulos lo usen (o por eventos).</summary>
public interface IMessagingApi
{
    string ModuleName { get; }
}
