using Knock.Api.Modules.Catalog.Data;
using Knock.Api.Modules.Catalog.Domain;
using MongoDB.Bson;

namespace Knock.Api.Modules.Catalog.Services;

internal sealed class CatalogApi(NeighborhoodLocator locator, CatalogStore store) : ICatalogApi
{
    public string ModuleName => CatalogModuleExtensions.ModuleKey;

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
}
