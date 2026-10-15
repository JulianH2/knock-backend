namespace Knock.Api.Modules.Assistant;

/// <summary>Contrato público del módulo Assistant: la única vía para que otros módulos lo usen (o por eventos).</summary>
public interface IAssistantApi
{
    string ModuleName { get; }
}
