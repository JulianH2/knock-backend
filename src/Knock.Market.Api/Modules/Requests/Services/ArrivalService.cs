using System.Security.Cryptography;
using System.Text;
using Knock.Market.Api.Common.Crypto;
using Knock.Market.Api.Modules.Requests.Data;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Dtos;

namespace Knock.Market.Api.Modules.Requests.Services;

/// <summary>Resultado de confirmar la llegada (spec 022, §5). Solo <see cref="Arrived"/> trae la vista; <see cref="WrongCode"/> trae los intentos que quedan.</summary>
internal enum ArrivalStatus
{
    Arrived,
    NotATechnician,
    NotFound,
    WrongCode,
    CodeUsed,
    CodeExpired,
    VisitLocked,
    VisitCancelled,
    VisitNotActive,
    VisitWithoutCode,
    OutsideWindow,
}

internal sealed record ArrivalOutcome(ArrivalStatus Status, VisitResponse? Visit = null, int AttemptsLeft = 0);

/// <summary>
/// Confirmar la llegada del técnico con el código de cuatro dígitos (spec 022, HU-038): resuelve al técnico por <c>sub</c>, descarta lo que no cuenta como intento, compara en
/// tiempo constante contra el código descifrado y deja que <see cref="VisitStore"/> aplique el cambio con su condición, sin carreras. El código nunca se registra ni se devuelve (R-15).
/// </summary>
internal sealed class ArrivalService(VisitService views, VisitStore visits, ITechnicianDirectory technicians, FieldCipher cipher, TimeProvider clock)
{
    public async Task<ArrivalOutcome> ArriveAsync(string sub, string id, string code, CancellationToken cancellationToken)
    {
        if (!await technicians.ExistsAsync(sub, cancellationToken))
        {
            return new ArrivalOutcome(ArrivalStatus.NotATechnician);
        }

        var visit = await views.FindAsync(id, cancellationToken);
        if (visit is null || !string.Equals(visit.TechnicianUserId, sub, StringComparison.Ordinal))
        {
            return new ArrivalOutcome(ArrivalStatus.NotFound);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        if (VisitRules.BlockForArrival(visit.State, now) is { } block)
        {
            return new ArrivalOutcome(Map(block));
        }

        return Matches(visit, code) ? await ConfirmAsync(visit, now, cancellationToken) : await RejectAsync(visit, now, cancellationToken);
    }

    private async Task<ArrivalOutcome> ConfirmAsync(VisitRecord visit, DateTime now, CancellationToken cancellationToken)
    {
        if (!await visits.MarkArrivedAsync(visit, now, cancellationToken))
        {
            // Otra petición cambió la visita entre la lectura y el cambio: se vuelve a leer y se explica con el estado nuevo, sin contar intento.
            return await ExplainAsync(visit, now, cancellationToken);
        }

        var updated = await visits.FindAsync(visit.Id, cancellationToken) ?? visit;
        return new ArrivalOutcome(ArrivalStatus.Arrived, await views.TechnicianViewAsync(updated, cancellationToken));
    }

    private async Task<ArrivalOutcome> RejectAsync(VisitRecord visit, DateTime now, CancellationToken cancellationToken)
    {
        var failed = await visits.RegisterFailedAttemptAsync(visit, now, cancellationToken);
        if (!failed.Applied)
        {
            return await ExplainAsync(visit, now, cancellationToken);
        }

        return failed.Locked
            ? new ArrivalOutcome(ArrivalStatus.VisitLocked)
            : new ArrivalOutcome(ArrivalStatus.WrongCode, AttemptsLeft: VisitRules.MaxAttempts - failed.Attempts);
    }

    private async Task<ArrivalOutcome> ExplainAsync(VisitRecord stale, DateTime now, CancellationToken cancellationToken)
    {
        var current = await visits.FindAsync(stale.Id, cancellationToken);
        var block = current is null ? ArrivalBlock.VisitNotActive : VisitRules.BlockForArrival(current.State, now) ?? ArrivalBlock.CodeUsed;
        return new ArrivalOutcome(Map(block));
    }

    /// <summary>Comparación en tiempo constante entre el código recibido y el guardado (descifrado en memoria solo para comparar).</summary>
    private bool Matches(VisitRecord visit, string code)
    {
        if (visit.EncryptedCode is null)
        {
            return false;
        }

        var stored = cipher.Decrypt(visit.EncryptedCode, VisitCodeFactory.Context(visit.Id));
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(stored), Encoding.ASCII.GetBytes(code));
    }

    private static ArrivalStatus Map(ArrivalBlock block) => block switch
    {
        ArrivalBlock.CodeUsed => ArrivalStatus.CodeUsed,
        ArrivalBlock.CodeExpired => ArrivalStatus.CodeExpired,
        ArrivalBlock.VisitLocked => ArrivalStatus.VisitLocked,
        ArrivalBlock.VisitCancelled => ArrivalStatus.VisitCancelled,
        ArrivalBlock.VisitNotActive => ArrivalStatus.VisitNotActive,
        ArrivalBlock.VisitWithoutCode => ArrivalStatus.VisitWithoutCode,
        _ => ArrivalStatus.OutsideWindow,
    };
}
