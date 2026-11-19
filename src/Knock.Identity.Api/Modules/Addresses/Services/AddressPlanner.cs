using Knock.Identity.Api.Modules.Addresses.Domain;
using MongoDB.Bson;

namespace Knock.Identity.Api.Modules.Addresses.Services;

/// <summary>
/// Reglas de negocio de las direcciones de una persona (spec 040, R-11 y R-12) como funciones puras de las direcciones que ya tiene: el tope de 10 y la única principal.
/// El almacén las aplica dentro de la transacción de la persona, así que dos peticiones simultáneas se ven una a la otra.
/// </summary>
internal static class AddressPlanner
{
    /// <summary>
    /// Alta: una dirección que la persona ya tiene (<paramref name="isSame"/>) se rechaza, así una petición repetida por mala señal no deja duplicados (R-18); la primera dirección queda
    /// principal; marcar otra como principal desmarca la anterior; la undécima se rechaza.
    /// </summary>
    public static AddressChanges ForCreate(IReadOnlyList<StoredAddress> existing, StoredAddress created, bool? wantsPrincipal, Func<StoredAddress, bool> isSame)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(created);
        ArgumentNullException.ThrowIfNull(isSame);
        if (existing.Any(isSame))
        {
            return AddressChanges.Reject(AddressRejections.Duplicate);
        }

        if (existing.Count >= AddressRules.MaxPerPerson)
        {
            return AddressChanges.Reject(AddressRejections.LimitReached);
        }

        var principal = existing.Count == 0 || wantsPrincipal == true;
        return WithPrincipal(existing, created with { Principal = principal }, principal);
    }

    /// <summary>
    /// Edición: marcar principal desmarca la anterior; desmarcar la única principal directamente se rechaza (hay que marcar otra, y así siempre hay una mientras haya
    /// direcciones). Sin <paramref name="wantsPrincipal"/> la marca queda como estaba.
    /// </summary>
    public static AddressChanges ForEdit(IReadOnlyList<StoredAddress> existing, StoredAddress edited, bool? wantsPrincipal)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(edited);
        if (wantsPrincipal == false && edited.Principal)
        {
            return AddressChanges.Reject(AddressRejections.PrincipalRequired);
        }

        var promote = wantsPrincipal == true && !edited.Principal;
        return promote ? WithPrincipal(existing, edited with { Principal = true }, true) : new AddressChanges([edited], []);
    }

    /// <summary>Baja: si era la principal, pasa a serlo la más reciente que quede (por fecha de creación y, a igual fecha, por identificador).</summary>
    public static AddressChanges ForDelete(IReadOnlyList<StoredAddress> existing, ObjectId id)
    {
        ArgumentNullException.ThrowIfNull(existing);
        if (existing.FirstOrDefault(address => address.Id == id) is not { } target)
        {
            return AddressChanges.Reject(AddressRejections.NotFound);
        }

        if (!target.Principal)
        {
            return new AddressChanges([], [id]);
        }

        var heir = existing.Where(address => address.Id != id).OrderByDescending(address => address.CreatedAt).ThenByDescending(address => address.Id).FirstOrDefault();
        return new AddressChanges(heir is null ? [] : [heir with { Principal = true }], [id]);
    }

    private static AddressChanges WithPrincipal(IReadOnlyList<StoredAddress> existing, StoredAddress principal, bool unmarkOthers)
    {
        var upserts = new List<StoredAddress> { principal };
        if (unmarkOthers)
        {
            upserts.AddRange(existing.Where(address => address.Principal && address.Id != principal.Id).Select(address => address with { Principal = false }));
        }

        return new AddressChanges(upserts, []);
    }
}
