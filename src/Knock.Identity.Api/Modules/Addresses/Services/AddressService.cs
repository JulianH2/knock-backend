using Knock.Contracts.Colonias;
using Knock.Identity.Api.Common.Errors;
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

/// <summary>Cómo terminó guardar una dirección. Los demás fallos (conflicto, catálogo caído) salen como <see cref="DomainException"/>.</summary>
internal enum AddressOutcome
{
    Saved,
    NoAccount,
    ColoniaNotFound,
}

/// <summary>Resultado de guardar: la dirección guardada (solo con <see cref="AddressOutcome.Saved"/>) o el motivo por el que no se guardó.</summary>
internal sealed record AddressResult(AddressOutcome Outcome, Address? Address = null)
{
    public sealed override string ToString() => nameof(AddressResult);
}

/// <summary>
/// Direcciones de la persona (spec 040). La identidad es siempre el <c>sub</c> del token (R-15); la dirección se descifra solo para su dueña y no se escribe en registros
/// ni se cachea (R-13). Si el <c>sub</c> del token no tiene cuenta, responde «sin cuenta» y el controller contesta 401, igual que sin token.
/// </summary>
internal sealed partial class AddressService(
    IAddressesStore store,
    AddressCipher cipher,
    IColoniaCatalog colonias,
    TimeProvider timeProvider,
    ILogger<AddressService> logger)
{
    public const string MarketUnavailableCode = "market_unavailable";

    public async Task<IReadOnlyList<Address>?> ListAsync(string sub, CancellationToken cancellationToken)
    {
        if (await store.FindUserIdAsync(sub, cancellationToken) is not { } userId)
        {
            return null;
        }

        var stored = await store.ListAsync(userId, cancellationToken);
        return [.. stored.Select(cipher.Open)];
    }

    public async Task<AddressResult> CreateAsync(string sub, AddressInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (await store.FindUserIdAsync(sub, cancellationToken) is not { } userId)
        {
            return new AddressResult(AddressOutcome.NoAccount);
        }

        if (!await ColoniaExistsAsync(input.Colonia, cancellationToken))
        {
            return new AddressResult(AddressOutcome.ColoniaNotFound);
        }

        var id = ObjectId.GenerateNewId();
        var created = timeProvider.GetUtcNow().UtcDateTime;
        var street = cipher.SealStreet(id, input.Street);
        var references = input.References is null ? null : cipher.SealReferences(id, input.References);
        var principal = false;
        var rejection = await store.ApplyAsync(
            userId,
            existing =>
            {
                // La primera dirección de la persona queda como principal (R-11).
                principal = existing.Count == 0;
                return new AddressChanges([new StoredAddress(id, userId, street, input.Colonia, references, principal, created)], []);
            },
            cancellationToken);
        if (rejection is not null)
        {
            return Rejected(rejection);
        }

        LogCreated(logger);
        return new AddressResult(AddressOutcome.Saved, new Address(id.ToString(), input.Street, input.Colonia, input.References, principal, created));
    }

    public async Task<AddressResult> UpdateAsync(string sub, ObjectId id, AddressEdit edit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(edit);
        if (await store.FindUserIdAsync(sub, cancellationToken) is not { } userId)
        {
            return new AddressResult(AddressOutcome.NoAccount);
        }

        if (edit.Colonia is not null && !await ColoniaExistsAsync(edit.Colonia, cancellationToken))
        {
            return new AddressResult(AddressOutcome.ColoniaNotFound);
        }

        Address? updated = null;
        var rejection = await store.ApplyAsync(
            userId,
            existing =>
            {
                // La búsqueda filtra por las direcciones de la cuenta del token: una dirección ajena no existe aquí (R-14).
                if (existing.FirstOrDefault(address => address.Id == id) is not { } current)
                {
                    return AddressChanges.Reject(AddressRejections.NotFound);
                }

                (var changed, updated) = cipher.Edit(current, edit);
                return new AddressChanges([changed], []);
            },
            cancellationToken);
        if (rejection is not null)
        {
            return Rejected(rejection);
        }

        LogUpdated(logger);
        return new AddressResult(AddressOutcome.Saved, updated);
    }

    private static AddressResult Rejected(string rejection) => rejection switch
    {
        AddressRejections.NoAccount => new AddressResult(AddressOutcome.NoAccount),
        AddressRejections.NotFound => throw new DomainException(DomainErrorKind.NotFound, "The address does not exist."),
        _ => throw new InvalidOperationException("The address change was rejected for an unknown reason."),
    };

    private async Task<bool> ColoniaExistsAsync(string colonia, CancellationToken cancellationToken)
    {
        try
        {
            return await colonias.ExistsAsync(colonia, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TimeoutException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            // Sin poder validar la colonia no se guarda nada ni se acepta sin validar (como en el perfil, spec 017, R-11).
            LogMarketUnavailable(logger);
            throw new DomainException(DomainErrorKind.ServiceUnavailable, "The colonia catalog did not answer.", MarketUnavailableCode);
        }
    }

    // Los registros no llevan calle, colonia, referencias, identificadores de dirección ni sub (spec 040, R-13 y R-16).
    [LoggerMessage(EventId = 4001, Level = LogLevel.Information, Message = "Address created")]
    private static partial void LogCreated(ILogger logger);

    [LoggerMessage(EventId = 4003, Level = LogLevel.Information, Message = "Address updated")]
    private static partial void LogUpdated(ILogger logger);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Warning, Message = "Colonia catalog unavailable: the address was not saved")]
    private static partial void LogMarketUnavailable(ILogger logger);
}
