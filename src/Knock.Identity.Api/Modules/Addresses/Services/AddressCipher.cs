using Knock.Identity.Api.Common.Crypto;
using Knock.Identity.Api.Modules.Addresses.Domain;

namespace Knock.Identity.Api.Modules.Addresses.Services;

/// <summary>Cifra y descifra los dos campos sensibles de la dirección (calle y referencias) con el dato asociado de cada documento (spec 040, R-10 y R-13).</summary>
internal sealed class AddressCipher(FieldCipher cipher)
{
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
