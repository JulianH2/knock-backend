using Knock.Identity.Api.Modules.HomeAndCar.Data;
using Knock.Identity.Api.Modules.HomeAndCar.Domain;
using Knock.Identity.Api.Modules.HomeAndCar.Dtos;

namespace Knock.Identity.Api.Modules.HomeAndCar.Services;

/// <summary>
/// «Mi casa y mi auto» (spec 041): los autos y las cosas de la casa de una persona. La persona es siempre la del <c>sub</c> del token (R-20) y un elemento ajeno es como uno que no existe (R-16).
/// Nunca registra el nombre del elemento (spec 011).
/// </summary>
internal sealed class HomeAndCarService(IHomeAndCarStore store)
{
    /// <summary>Los elementos de la persona, sin paginar (a lo más 20, R-15), del más reciente al más antiguo.</summary>
    public async Task<IReadOnlyList<ItemResponse>> ListAsync(string sub, CancellationToken cancellationToken) =>
        [.. (await store.ListAsync(sub, cancellationToken)).Select(ToResponse)];

    internal static ItemResponse ToResponse(HomeAndCarItem item) => new(item.Id.ToString(), item.Type, item.Name, item.Year, item.CreatedAt);
}
