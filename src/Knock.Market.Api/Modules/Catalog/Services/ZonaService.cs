using Knock.Market.Api.Modules.Catalog.Data;
using Knock.Market.Api.Modules.Catalog.Dtos;

namespace Knock.Market.Api.Modules.Catalog.Services;

internal sealed class ZonaService(CatalogStore store, IConfiguration configuration)
{
    public async Task<ZonaResponse> FindAsync(string postalCode, CancellationToken cancellationToken)
    {
        var colonias = await store.FindByPostalCodeAsync(postalCode, cancellationToken);
        var items = colonias.Select(c => new ZonaColoniaResponse(c.Id, c.Nombre, c.Cp, c.Municipio)).ToList();
        return new ZonaResponse(items.Count > 0, configuration["Catalogo:CiudadPiloto"] ?? throw new InvalidOperationException("Catalogo:CiudadPiloto is required."), items);
    }
}
