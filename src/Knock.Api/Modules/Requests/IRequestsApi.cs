namespace Knock.Api.Modules.Requests;

/// <summary>Contrato público del módulo Requests: la única vía para que otros módulos lo usen (o por eventos).</summary>
public interface IRequestsApi
{
    string ModuleName { get; }
}
