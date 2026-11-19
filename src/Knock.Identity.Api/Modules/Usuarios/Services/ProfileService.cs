using Knock.Contracts.Colonias;
using Knock.Identity.Api.Common.Errors;
using Knock.Identity.Api.Modules.Usuarios.Data;
using Knock.Identity.Api.Modules.Usuarios.Domain;

namespace Knock.Identity.Api.Modules.Usuarios.Services;

/// <summary>
/// Perfil de la persona (spec 017): lectura con caché de 60 segundos y escritura que valida la colonia contra Mercado, guarda y borra la caché antes
/// de responder. La identidad es siempre el <c>sub</c> del token; nunca se loguea el nombre ni el celular (R-10).
/// </summary>
internal sealed partial class ProfileService(
    IUsuariosStore store,
    IProfileCache cache,
    IColoniaCatalog colonias,
    TimeProvider timeProvider,
    ILogger<ProfileService> logger)
{
    public const string ColoniaNotFoundCode = "colonia_not_found";
    public const string MarketUnavailableCode = CommonErrorCodes.MarketUnavailable;
    public const string DeletionScheduledCode = "deletion_scheduled";

    public async Task<UserProfile> GetAsync(string sub, CancellationToken cancellationToken)
    {
        if (await cache.GetAsync(sub, cancellationToken) is { } cached)
        {
            return cached;
        }

        var profile = await store.FindProfileAsync(sub, cancellationToken)
            ?? throw new DomainException(DomainErrorKind.NotFound, "The account of the token does not exist.");
        await cache.SetAsync(profile, cancellationToken);
        return profile;
    }

    public async Task<UserProfile> UpdateAsync(string sub, ProfileUpdate update, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(update);
        var current = await store.FindProfileAsync(sub, cancellationToken)
            ?? throw new DomainException(DomainErrorKind.NotFound, "The account of the token does not exist.");
        if (current.State == ProfileStates.DeletionScheduled)
        {
            throw DeletionScheduled();
        }

        await EnsureColoniaExistsAsync(update.Colonia, cancellationToken);
        var (outcome, saved) = await store.UpdateProfileAsync(sub, update, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        if (outcome == ProfileUpdateOutcome.DeletionScheduled)
        {
            throw DeletionScheduled();
        }

        if (outcome == ProfileUpdateOutcome.NotFound || saved is null)
        {
            throw new DomainException(DomainErrorKind.NotFound, "The account of the token does not exist.");
        }

        await cache.RemoveAsync(sub, cancellationToken);
        LogUpdated(logger);
        return saved;
    }

    private async Task EnsureColoniaExistsAsync(string? colonia, CancellationToken cancellationToken)
    {
        if (colonia is null)
        {
            return;
        }

        bool exists;
        try
        {
            exists = await colonias.ExistsAsync(colonia, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TimeoutException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            // R-11: sin poder validar la colonia no se guarda nada ni se acepta sin validar.
            LogMarketUnavailable(logger);
            throw new DomainException(DomainErrorKind.ServiceUnavailable, "The colonia catalog did not answer.", MarketUnavailableCode);
        }

        if (!exists)
        {
            throw new DomainException(DomainErrorKind.Unprocessable, "The colonia does not exist in the catalog.", ColoniaNotFoundCode);
        }
    }

    private static DomainException DeletionScheduled() =>
        new(DomainErrorKind.Conflict, "The account has a scheduled deletion.", DeletionScheduledCode);

    // Los registros no llevan nombre, colonia, código postal, foto, celular ni sub.
    [LoggerMessage(EventId = 2202, Level = LogLevel.Information, Message = "Profile updated")]
    private static partial void LogUpdated(ILogger logger);

    [LoggerMessage(EventId = 2203, Level = LogLevel.Warning, Message = "Colonia catalog unavailable: the profile was not saved")]
    private static partial void LogMarketUnavailable(ILogger logger);
}
