using Knock.Identity.Api.Common.Errors;
using Knock.Identity.Api.Modules.HomeAndCar.Data;
using Knock.Identity.Api.Modules.HomeAndCar.Domain;
using Knock.Identity.Api.Modules.HomeAndCar.Dtos;
using Knock.Identity.Api.Modules.HomeAndCar.Validators;
using MongoDB.Bson;

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
            ObjectId.GenerateNewId(),
            sub,
            request.Tipo!,
            HomeAndCarRules.NormalizeName(request.Nombre)!,
            request.Tipo == ItemTypes.Car ? request.Anio : null,
            clock.GetUtcNow().UtcDateTime);
        return await store.AddAsync(item, HomeAndCarRules.MaxItemsPerPerson, cancellationToken) == AddItemOutcome.Created
            ? ToResponse(item)
            : throw new DomainException(DomainErrorKind.Conflict, "The person already has the maximum number of items.", HomeAndCarRules.LimitReachedCode);
    }

    /// <summary>
    /// Edita un elemento propio (CAS-03): cambian solo los campos enviados (R-14). Un elemento ajeno o inexistente lanza 404 (R-16). Si el resultado deja de ser un auto, el año se quita; un año
    /// para un elemento que no es auto devuelve el error por campo (422).
    /// </summary>
    public async Task<UpdateItemOutcome> UpdateAsync(string sub, string id, UpdateItemRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var current = await FindOwnAsync(sub, id, cancellationToken);
        var type = request.Tipo ?? current.Type;
        if (request.Anio is not null && type != ItemTypes.Car)
        {
            return new UpdateItemOutcome(null, new Dictionary<string, string[]>(StringComparer.Ordinal) { ["anio"] = [ItemReasons.YearNotForType] });
        }

        var name = request.Nombre is null ? current.Name : HomeAndCarRules.NormalizeName(request.Nombre)!;
        var year = type == ItemTypes.Car ? request.Anio ?? current.Year : null;
        var saved = await store.UpdateAsync(sub, current.Id, current.Type, type, name, year, cancellationToken)
            ?? throw NotFound();
        return new UpdateItemOutcome(ToResponse(saved), null);
    }

    private async Task<HomeAndCarItem> FindOwnAsync(string sub, string id, CancellationToken cancellationToken) =>
        ObjectId.TryParse(id, out var objectId) && await store.FindAsync(sub, objectId, cancellationToken) is { } item ? item : throw NotFound();

    private static DomainException NotFound() => new(DomainErrorKind.NotFound, "The item does not exist.");

    internal static ItemResponse ToResponse(HomeAndCarItem item) => new(item.Id.ToString(), item.Type, item.Name, item.Year, item.CreatedAt);
}

/// <summary>Resultado de editar: el elemento nuevo, o los errores por campo cuando el elemento resultante no sería coherente.</summary>
internal sealed record UpdateItemOutcome(ItemResponse? Item, IDictionary<string, string[]>? Errors);
