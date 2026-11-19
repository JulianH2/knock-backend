using Knock.Identity.Api.Common.Crypto;
using Knock.Identity.Api.Modules.Addresses.Domain;
using MongoDB.Bson;

namespace Knock.Identity.Api.Modules.Addresses.Services;

/// <summary>Cifra y descifra los dos campos sensibles de la dirección (calle y referencias) con el dato asociado de cada documento (spec 040, R-10 y R-13).</summary>
internal sealed class AddressCipher(FieldCipher cipher)
{
    public string SealStreet(ObjectId id, string street) => cipher.Encrypt(street, AddressCrypto.StreetContext(id));

    public string SealReferences(ObjectId id, string references) => cipher.Encrypt(references, AddressCrypto.ReferencesContext(id));

    /// <summary>Aplica una edición: devuelve el documento con los campos enviados cifrados de nuevo y la dirección en claro para responder. Lo que no se envía no cambia.</summary>
    public (StoredAddress Stored, Address Open) Edit(StoredAddress current, AddressEdit edit)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(edit);
        var open = Open(current);
        var street = edit.Street ?? open.Street;
        var colonia = edit.Colonia ?? open.Colonia;
        var references = open.References;
        if (edit.References is not null)
        {
            references = edit.References.Length == 0 ? null : edit.References;
        }

        var stored = current with
        {
            StreetCipher = edit.Street is null ? current.StreetCipher : SealStreet(current.Id, street),
            Colonia = colonia,
            ReferencesCipher = SealedReferences(current, edit, references),
        };
        return (stored, open with { Street = street, Colonia = colonia, References = references });
    }

    private string? SealedReferences(StoredAddress current, AddressEdit edit, string? references)
    {
        if (edit.References is null)
        {
            return current.ReferencesCipher;
        }

        return references is null ? null : SealReferences(current.Id, references);
    }

    public Address Open(StoredAddress stored)
    {
        ArgumentNullException.ThrowIfNull(stored);
        return new Address(
            stored.Id.ToString(),
            cipher.Decrypt(stored.StreetCipher, AddressCrypto.StreetContext(stored.Id)),
            stored.Colonia,
            stored.ReferencesCipher is null ? null : cipher.Decrypt(stored.ReferencesCipher, AddressCrypto.ReferencesContext(stored.Id)),
            stored.Principal,
            stored.CreatedAt);
    }
}
