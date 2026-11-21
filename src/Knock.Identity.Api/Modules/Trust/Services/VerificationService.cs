using Knock.Identity.Api.Common.Errors;
using Knock.Identity.Api.Modules.Trust.Data;
using Knock.Identity.Api.Modules.Trust.Domain;
using Knock.Identity.Api.Modules.Trust.Dtos;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace Knock.Identity.Api.Modules.Trust.Services;

/// <summary>Aviso de privacidad del consentimiento biométrico (sección <c>Verificacion</c>): la versión vigente y su texto (R-09).</summary>
internal sealed class VerificationOptions
{
    public const string SectionName = "Verificacion";

    public string AvisoVersion { get; set; } = string.Empty;

    public string AvisoTexto { get; set; } = string.Empty;
}

/// <summary>
/// Crear y consultar la verificación propia (spec 025, versión sin fotos: T-05 sin T-03, T-04 ni T-06). La persona es siempre la del <c>sub</c>. La CURP solo se valida
/// y se descarta: sin la llave del HMAC (T-03) no se guarda de ninguna forma y la detección de duplicados (R-06) llega con esa tarea.
/// </summary>
internal sealed class VerificationService(IVerificationStore store, IOptions<VerificationOptions> options, TimeProvider clock)
{
    /// <summary>Una captura a medias vive 24 h (§14, punto 5).</summary>
    public static readonly TimeSpan CaptureLifetime = TimeSpan.FromHours(24);

    /// <summary>Crea la verificación. <see langword="null"/> si la cuenta o la sesión del token ya no existen (401).</summary>
    /// <exception cref="DomainException">422 o 409 con el código de la spec (§5).</exception>
    public async Task<VerificationCreatedResponse?> CreateAsync(string sub, string sessionId, CreateVerificationRequest request, CancellationToken cancellationToken)
    {
        var account = await store.FindAccountAsync(sub, cancellationToken);
        if (account is null)
        {
            return null;
        }

        if (account.State == "eliminacion_programada")
        {
            throw new DomainException(DomainErrorKind.Conflict, "The account has a scheduled deletion.", CommonErrorCodes.DeletionScheduled);
        }

        // R-02 y R-09: sin valor por omisión, y el aviso aceptado debe ser el vigente.
        if (request.AceptaBiometria != true || !string.Equals(request.VersionAviso, options.Value.AvisoVersion, StringComparison.Ordinal))
        {
            throw new DomainException(DomainErrorKind.Unprocessable, "Biometric consent is missing.", VerificationErrors.ConsentRequiredCode);
        }

        if (!Curp.IsValid(request.Curp))
        {
            throw new DomainException(DomainErrorKind.Unprocessable, "The CURP is not valid.", VerificationErrors.CurpInvalidCode);
        }

        EnsureCanStart(await store.FindLatestAsync(sub, cancellationToken), request.Nivel!);
        var device = await store.FindSessionDeviceAsync(account.UserId, sessionId, cancellationToken);
        if (device is null)
        {
            return null;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var verification = new NewVerification(ObjectId.GenerateNewId(), sub, request.Nivel!, now, now + CaptureLifetime, options.Value.AvisoTexto,
            options.Value.AvisoVersion, device);
        await store.CreateAsync(verification, cancellationToken);
        return new VerificationCreatedResponse(verification.Id.ToString(), VerificationStates.Capturing, verification.Level,
            [.. VerificationLevels.PhotosFor(verification.Level).Select(type => new VerificationPhotoResponse(type, PhotoStates.Pending, null, null))],
            verification.CaptureExpiresAt);
    }

    /// <summary>R-07: una en revisión bloquea; una verificada no se repite con el mismo nivel ni por debajo del otorgado.</summary>
    internal static void EnsureCanStart(StoredVerification? latest, string level)
    {
        if (latest is null)
        {
            return;
        }

        if (latest.State == VerificationStates.UnderReview)
        {
            throw new DomainException(DomainErrorKind.Conflict, "A verification is under review.", VerificationErrors.UnderReviewCode);
        }

        if (latest.State != VerificationStates.Verified)
        {
            return;
        }

        if (latest.GrantedLevel == VerificationLevels.Full)
        {
            throw new DomainException(DomainErrorKind.Conflict, "The account is fully verified.", VerificationErrors.AlreadyFullyVerifiedCode);
        }

        if (level == VerificationLevels.Basic)
        {
            throw new DomainException(DomainErrorKind.Conflict, "The account is already verified at this level.", VerificationErrors.AlreadyVerifiedCode);
        }
    }

    /// <summary>R-14 y R-15: la más reciente, o <c>sin_verificar</c> si nunca hubo.</summary>
    public async Task<VerificationStatusResponse> GetAsync(string sub, CancellationToken cancellationToken)
    {
        var latest = await store.FindLatestAsync(sub, cancellationToken);
        if (latest is null)
        {
            return new VerificationStatusResponse(null, null, VerificationStates.NotVerified, null, [], null, [], null, null, null, null, PuedeReintentar: true);
        }

        var showsReason = latest.State is VerificationStates.Rejected or VerificationStates.NotVerified;
        var reason = showsReason ? latest.Reason : null;
        return new VerificationStatusResponse(
            latest.Id.ToString(),
            latest.Level,
            latest.State,
            reason,
            VerificationReasons.TipsFor(reason),
            latest.State == VerificationStates.Verified ? latest.GrantedLevel : null,
            [.. latest.Photos.Select(p => new VerificationPhotoStateResponse(p.Type, p.State))],
            latest.CreatedAt,
            latest.SentAt,
            latest.DecidedAt,
            latest.ExpiresAt,
            CanRetry(latest));
    }

    /// <summary>Puede empezar otra si no hay una en revisión y no tiene ya el nivel completo.</summary>
    internal static bool CanRetry(StoredVerification latest) =>
        latest.State != VerificationStates.UnderReview && !(latest.State == VerificationStates.Verified && latest.GrantedLevel == VerificationLevels.Full);
}
