using Knock.Identity.Api.Common.Errors;
using Knock.Identity.Api.Modules.HomeAndCar.Data;
using Knock.Identity.Api.Modules.HomeAndCar.Domain;
using Knock.Identity.Api.Modules.HomeAndCar.Dtos;

namespace Knock.Identity.Api.Modules.HomeAndCar.Services;

/// <summary>
/// «Mi casa y mi auto» (spec 041): los autos y las cosas de la casa de una persona. La persona es siempre la del <c>sub</c> del token (R-20) y un elemento ajeno es como uno que no existe (R-16).
/// Nunca registra el nombre del elemento (spec 011).
/// </summary>
internal sealed class HomeAndCarService(IHomeAndCarStore store, TimeProvider clock)
{
    /// <summary>Los elementos de la persona, sin paginar (a lo más 20, R-15), del más reciente al más antiguo.</summary>
    public async Task<IReadOnlyList<ItemResponse>> ListAsync(string sub, CancellationToken cancellationToken) =>
        [.. (await store.ListAsync(sub, cancellationToken)).Select(ToResponse)];

    /// <summary>
    /// Registra un elemento de la persona (CAS-02). El formato ya lo validó <c>CreateItemRequestValidator</c>. Si la persona ya tiene 20 lanza 409 <c>item_limit_reached</c>.
    /// No hay <c>Idempotency-Key</c> (R-23, D-03): repetir la petición por mala señal choca con el tope de 20, no deja más de los permitidos.
    /// </summary>
    public async Task<ItemResponse> CreateAsync(string sub, CreateItemRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = new HomeAndCarItem(
            MongoDB.Bson.ObjectId.GenerateNewId(),
            sub,
            request.Tipo!,
            HomeAndCarRules.NormalizeName(request.Nombre)!,
            request.Tipo == ItemTypes.Car ? request.Anio : null,
            clock.GetUtcNow().UtcDateTime);
        return await store.AddAsync(item, HomeAndCarRules.MaxItemsPerPerson, cancellationToken) == AddItemOutcome.Created
            ? ToResponse(item)
            : throw new DomainException(DomainErrorKind.Conflict, "The person already has the maximum number of items.", HomeAndCarRules.LimitReachedCode);
    }

    internal static ItemResponse ToResponse(HomeAndCarItem item) => new(item.Id.ToString(), item.Type, item.Name, item.Year, item.CreatedAt);
}
