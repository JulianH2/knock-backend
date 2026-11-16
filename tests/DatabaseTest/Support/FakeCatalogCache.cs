using Knock.Market.Api.Modules.Catalog.Data;
using Knock.Market.Api.Modules.Catalog.Domain;
using MongoDB.Bson;

namespace Knock.DatabaseTest.Support;

/// <summary>
/// Doble de <see cref="ICatalogCache"/> para las pruebas que no prueban la caché (spec 010): siempre falla, así las lecturas van a
/// MongoDB como antes de la spec 010 y las cuentas de comandos no cambian. Las pruebas de caché usan Redis real.
/// </summary>
internal sealed class FakeCatalogCache : ICatalogCache
{
    public Task<IReadOnlyList<OficioListing>?> GetOficiosAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OficioListing>?>(null);

    public Task RemoveOficiosAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<IReadOnlyList<Colonia>?> GetNeighborhoodNamesAsync(string postalCode, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Colonia>?>(null);

    public Task RemoveNeighborhoodNamesAsync(string postalCode, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<bool?> GetColoniaExistsAsync(ObjectId id, string postalCode, CancellationToken cancellationToken) =>
        Task.FromResult<bool?>(null);


    public Task RemoveColoniaExistsAsync(ObjectId id, string postalCode, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<long> GetOficiosGenerationAsync(CancellationToken cancellationToken) => Task.FromResult(0L);

    public Task<bool> SetOficiosIfGenerationAsync(IReadOnlyList<OficioListing> oficios, long expectedGeneration, CancellationToken cancellationToken) => Task.FromResult(true);

    public Task<long> GetNeighborhoodNamesGenerationAsync(string postalCode, CancellationToken cancellationToken) => Task.FromResult(0L);

    public Task<bool> SetNeighborhoodNamesIfGenerationAsync(string postalCode, IReadOnlyList<Colonia> names, long expectedGeneration, CancellationToken cancellationToken) => Task.FromResult(true);

    public Task<long> GetColoniaExistsGenerationAsync(ObjectId id, string postalCode, CancellationToken cancellationToken) => Task.FromResult(0L);

    public Task<bool> SetColoniaExistsIfGenerationAsync(ObjectId id, string postalCode, bool exists, long expectedGeneration, CancellationToken cancellationToken) => Task.FromResult(true);
}
