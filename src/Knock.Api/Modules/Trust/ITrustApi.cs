namespace Knock.Api.Modules.Trust;

/// <summary>Contrato público del módulo Trust: la única vía para que otros módulos lo usen (o por eventos).</summary>
public interface ITrustApi
{
    string ModuleName { get; }
}
