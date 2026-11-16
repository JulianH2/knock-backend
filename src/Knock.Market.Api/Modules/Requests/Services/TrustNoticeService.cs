using Knock.Market.Api.Modules.Requests.Data;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Dtos;

namespace Knock.Market.Api.Modules.Requests.Services;

/// <summary>Resultado de preparar el aviso: el texto, o por qué no se puede (spec 022, R-12 y R-13).</summary>
internal enum TrustNoticeStatus
{
    Prepared,
    NotFound,
    NotNotifiable,
}

internal sealed record TrustNoticeOutcome(TrustNoticeStatus Status, TrustNotice? Notice = null);

/// <summary>
/// Preparar el texto de aviso a una persona de confianza (spec 022, HU-037). Solo el cliente dueño y solo con la visita <c>confirmada</c> o <c>en_curso</c>. No guarda contactos
/// ni envía nada; cuenta el aviso y deja <c>AvisoConfianzaPreparado</c> con identificadores. El texto nunca lleva el código ni la dirección.
/// </summary>
internal sealed class TrustNoticeService(VisitService views, VisitStore visits, ChoiceStore choices, TimeProvider clock)
{
    public async Task<TrustNoticeOutcome> PrepareAsync(string clientId, string id, CancellationToken cancellationToken)
    {
        var visit = await views.FindAsync(id, cancellationToken);
        if (visit is null || !string.Equals(visit.ClientId, clientId, StringComparison.Ordinal))
        {
            return new TrustNoticeOutcome(TrustNoticeStatus.NotFound);
        }

        if (!VisitRules.CanNotify(visit.Estado) || !await visits.CountNoticeAsync(visit, clock.GetUtcNow().UtcDateTime, cancellationToken))
        {
            return new TrustNoticeOutcome(TrustNoticeStatus.NotNotifiable);
        }

        var technicians = await choices.FindTechniciansAsync([visit.TechnicianId], cancellationToken);
        technicians.TryGetValue(visit.TechnicianId, out var technician);
        var text = VisitRules.TrustNotice(technician?.Name ?? "el técnico", technician?.VerificationLevel ?? QuoteComparer.NoVerification, visit.Day, visit.Time);
        return new TrustNoticeOutcome(TrustNoticeStatus.Prepared, new TrustNotice(text, false, false));
    }
}
