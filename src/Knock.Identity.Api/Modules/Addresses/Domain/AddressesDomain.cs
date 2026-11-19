using MongoDB.Bson;

namespace Knock.Identity.Api.Modules.Addresses.Domain;

/// <summary>
/// Dirección de la persona (spec 040, R-01): lo que ve su dueña. <c>Street</c> y <c>References</c> son datos personales en claro, solo en memoria: se
/// guardan cifrados. Nunca se imprime: un registro llevaría la dirección (spec 040, R-13 y R-16).
/// </summary>
internal sealed record Address(string Id, string Street, string Colonia, string? References, bool Principal, DateTime CreatedAt)
{
    /// <summary>Sin datos: un registro de la dirección imprimiría la calle y las referencias.</summary>
    public sealed override string ToString() => nameof(Address);
}

/// <summary>
/// Dirección como se guarda en <c>direcciones</c> (spec 040, §6): la calle y las referencias ya cifradas por campo; la colonia va en claro porque el
/// técnico la ve mientras cotiza. <c>UserId</c> es el <c>_id</c> de la cuenta, no el <c>sub</c>.
/// </summary>
internal sealed record StoredAddress(
    ObjectId Id, ObjectId UserId, string StreetCipher, string Colonia, string? ReferencesCipher, bool Principal, DateTime CreatedAt)
{
    public sealed override string ToString() => nameof(StoredAddress);
}

/// <summary>
/// Cambios que el servicio decide sobre las direcciones de una persona: documentos que se escriben (nuevos o ya existentes, con su <c>principal</c> al día) y los que se
/// borran. Si <see cref="Rejection"/> trae un código, no se escribe nada.
/// </summary>
internal sealed record AddressChanges(IReadOnlyList<StoredAddress> Upserts, IReadOnlyList<ObjectId> Deletes, string? Rejection = null)
{
    public static AddressChanges Reject(string rejection) => new([], [], rejection);

    public sealed override string ToString() => nameof(AddressChanges);
}

/// <summary>Motivos por los que no se aplica un cambio. Los que salen al cliente son códigos de negocio en inglés y snake_case (spec 040, R-12 y D-02).</summary>
internal static class AddressRejections
{
    public const string NoAccount = "no_account";
    public const string NotFound = "not_found";
    public const string LimitReached = "address_limit_reached";
    public const string Duplicate = "address_duplicate";
    public const string PrincipalRequired = "principal_required";
}
