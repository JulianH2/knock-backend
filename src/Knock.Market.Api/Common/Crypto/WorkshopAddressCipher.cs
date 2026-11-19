using System.Security.Cryptography;
using MongoDB.Bson;

namespace Knock.Market.Api.Common.Crypto;

/// <summary>
/// Dirección del taller de <c>tecnicos.tallerDireccion</c> (spec 046, §6: texto cifrado). La escribe el registro de técnicos y la lee la elección de cotización
/// (spec 021) cuando la revela. El contexto liga el valor al documento, así que un valor copiado a otro técnico no se descifra.
/// </summary>
internal static class WorkshopAddressCipher
{
    public static string Context(ObjectId technicianId) => $"tecnicos|tallerDireccion|{technicianId}";

    public static string Encrypt(FieldCipher cipher, ObjectId technicianId, string address) => cipher.Encrypt(address, Context(technicianId));

    /// <summary>
    /// Texto de la dirección. Los técnicos de demostración (<c>demo: true</c>) se siembran en claro; un valor que no se puede descifrar no se devuelve, nunca se
    /// muestra el texto cifrado.
    /// </summary>
    public static string? Read(FieldCipher cipher, ObjectId technicianId, string? stored, bool demo)
    {
        if (stored is null || demo)
        {
            return stored;
        }

        try
        {
            return cipher.Decrypt(stored, Context(technicianId));
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException or InvalidOperationException or KeyNotFoundException)
        {
            return null;
        }
    }
}
