using Knock.Identity.Api.Common.Errors;
using Knock.Identity.Api.Modules.Account.Data;
using Knock.Identity.Api.Modules.Account.Domain;
using Microsoft.Extensions.Options;

namespace Knock.Identity.Api.Modules.Account.Services;

/// <summary>
/// Consentimientos de la persona (spec 039, USR-07 y USR-08; R-23). Los dos obligatorios (aviso de privacidad y términos) se listan pero no se retiran por
/// aquí; los tres opcionales (promociones, encuestas, recomendaciones) se activan o retiran guardando tipo, versión y fecha, y el cambio vale de inmediato.
/// </summary>
internal sealed partial class ConsentService(
    IAccountStore store,
    IOptions<OptionalConsentOptions> options,
    TimeProvider timeProvider,
    ILogger<ConsentService> logger)
{
    /// <exception cref="DomainException">404 si la cuenta del token ya no existe.</exception>
    public async Task<IReadOnlyList<ConsentEntry>> ListAsync(string sub, CancellationToken cancellationToken)
    {
        var stored = await store.FindConsentsAsync(sub, cancellationToken)
            ?? throw new DomainException(DomainErrorKind.NotFound, "The account of the token does not exist.");
        return ConsentCatalog.Build(stored);
    }

    /// <summary>Activa o retira un consentimiento opcional y devuelve cómo quedó.</summary>
    /// <exception cref="DomainException">422 si el tipo no es opcional (también lo impide el validador); 404 si la cuenta ya no existe.</exception>
    public async Task<ConsentEntry> SetAsync(string sub, string type, bool active, CancellationToken cancellationToken)
    {
        if (!ConsentTypes.IsOptional(type))
        {
            throw new DomainException(DomainErrorKind.Unprocessable, "Only the optional consents can be changed.");
        }

        var saved = await store.SetOptionalConsentAsync(sub, type, active, options.Value.Opcionales, timeProvider.GetUtcNow().UtcDateTime, cancellationToken)
            ?? throw new DomainException(DomainErrorKind.NotFound, "The account of the token does not exist.");
        LogChanged(logger, active);
        return new ConsentEntry(type, saved.Active == true, false, saved.Version, saved.At);
    }

    // Los registros no llevan el tipo ni la persona: solo si se activó o se retiró.
    [LoggerMessage(EventId = 3910, Level = LogLevel.Information, Message = "Optional consent changed; active: {Active}")]
    private static partial void LogChanged(ILogger logger, bool active);
}
