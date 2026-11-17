using Knock.Contracts.Catalog;
using Knock.Market.Api.Modules.Catalog.Data;
using Knock.Market.Api.Modules.Catalog.Domain;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Catalog.Services;

internal sealed class CatalogApi(NeighborhoodLocator locator, CatalogStore store) : ICatalogApi, INeighborhoodCenterCatalog
{
    public string ModuleName => CatalogModuleExtensions.ModuleKey;

    public Task<(double Latitude, double Longitude)?> FindNeighborhoodCenterAsync(string coloniaId, CancellationToken cancellationToken) =>
        ObjectId.TryParse(coloniaId, out var id) ? store.FindCenterAsync(id, cancellationToken) : Task.FromResult<(double, double)?>(null);

    public async Task<string?> FindNeighborhoodAtAsync(double latitude, double longitude, CancellationToken cancellationToken) =>
        (await locator.FindAtAsync(latitude, longitude, cancellationToken))?.Nombre;

    public Task<IReadOnlyList<string>> FindNeighborhoodsByPostalCodeAsync(string postalCode, CancellationToken cancellationToken)
    {
        if (!Colonia.IsPostalCode(postalCode))
        {
            throw new ArgumentException("The postal code must be five digits.", nameof(postalCode));
        }

        return store.FindNamesByPostalCodeAsync(postalCode, cancellationToken);
    }

    /// <remarks>La entrada se convierte a <see cref="ObjectId"/> y se valida antes de consultar: un texto como <c>{"$ne":null}</c> nunca llega al filtro.</remarks>
    public Task<bool> NeighborhoodExistsAsync(string coloniaId, string cp, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(coloniaId, out var id))
        {
            throw new ArgumentException("The neighborhood id must be 24 hexadecimal characters.", nameof(coloniaId));
        }

        if (!Colonia.IsPostalCode(cp))
        {
            throw new ArgumentException("The postal code must be five digits.", nameof(cp));
        }

        return store.ColoniaExistsAsync(id, cp, cancellationToken);
    }

    /// <remarks>La clave se valida antes de consultar y el filtro es tipado: un texto como <c>{"$ne":null}</c> es solo una clave que no existe.</remarks>
    public async Task<(string Nombre, string Modalidad)?> FindActiveTradeAsync(string clave, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clave) || clave.Length > Oficio.MaxClaveLength)
        {
            throw new ArgumentException("The trade key must have between 1 and 16 characters.", nameof(clave));
        }

        var oficio = await store.FindActiveOficioAsync(clave, cancellationToken);
        return oficio is null ? null : (oficio.Nombre, oficio.Modalidad);
    }

    public async Task<(string Nombre, string Cp)?> FindNeighborhoodAsync(string coloniaId, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(coloniaId, out var id))
        {
            throw new ArgumentException("The neighborhood id must be 24 hexadecimal characters.", nameof(coloniaId));
        }

        var colonia = await store.FindColoniaAsync(id, cancellationToken);
        return colonia is null ? null : (colonia.Nombre, colonia.Cp);
    }
}
