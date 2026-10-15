namespace Knock.Api.Modules.Identity;

/// <summary>Contrato público del módulo Identity: la única vía para que otros módulos lo usen (o por eventos).</summary>
public interface IIdentityApi
{
    string ModuleName { get; }
}
