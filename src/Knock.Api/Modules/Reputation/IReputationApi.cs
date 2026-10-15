namespace Knock.Api.Modules.Reputation;

/// <summary>Contrato público del módulo Reputation: la única vía para que otros módulos lo usen (o por eventos).</summary>
public interface IReputationApi
{
    string ModuleName { get; }
}
