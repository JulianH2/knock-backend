using Knock.Identity.Api.Modules.Addresses.Data;
using Knock.Identity.Api.Modules.Addresses.Domain;
using MongoDB.Bson;

namespace Knock.Identity.Api.Modules.Addresses.Services;

/// <summary>
/// Contextos del cifrado de campo (AES-GCM, dato asociado): ligan cada valor cifrado a su documento y a su campo, así copiarlo a otra dirección o cambiarlo de campo
/// falla al descifrar (spec 040, R-13).
/// </summary>
internal static class AddressCrypto
{
    public static string StreetContext(ObjectId id) => $"direcciones|calleNumero|{id}";

    public static string ReferencesContext(ObjectId id) => $"direcciones|referencias|{id}";
}

/// <summary>
/// Direcciones de la persona (spec 040). La identidad es siempre el <c>sub</c> del token (R-15); la dirección se descifra solo para su dueña y no se escribe en registros
/// ni se cachea (R-13). Si el <c>sub</c> del token no tiene cuenta, devuelve <see langword="null"/> y el controller responde 401, igual que sin token.
/// </summary>
internal sealed class AddressService(IAddressesStore store, AddressCipher cipher)
{
    public async Task<IReadOnlyList<Address>?> ListAsync(string sub, CancellationToken cancellationToken)
    {
        if (await store.FindUserIdAsync(sub, cancellationToken) is not { } userId)
        {
            return null;
        }

        var stored = await store.ListAsync(userId, cancellationToken);
        return [.. stored.Select(cipher.Open)];
    }
}
