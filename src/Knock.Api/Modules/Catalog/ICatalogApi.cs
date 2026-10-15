namespace Knock.Api.Modules.Catalog;

/// <summary>Contrato público del módulo Catalog: la única vía para que otros módulos lo usen (o por eventos).</summary>
public interface ICatalogApi
{
    string ModuleName { get; }
}
