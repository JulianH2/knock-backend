using Knock.Contracts.Moderation;
using Knock.Identity.Api.Common.Errors;
using Knock.Identity.Api.Modules.Safety.Data;
using Knock.Identity.Api.Modules.Safety.Domain;
using Knock.Identity.Api.Modules.Safety.Dtos;
using Knock.Identity.Api.Modules.Usuarios;
using MongoDB.Bson;

namespace Knock.Identity.Api.Modules.Safety.Services;

/// <summary>Cómo terminó crear un reporte.</summary>
internal enum ReportOutcome
{
    Created,
    AlreadyOpen,
    ReportedInvalid,
}

/// <summary>Resultado de crear un reporte: el estado y, salvo que la persona reportada no sea válida, el reporte.</summary>
internal sealed record ReportCreation(ReportOutcome Outcome, ReportCreated? Report = null)
{
    public sealed override string ToString() => nameof(ReportCreation);
}

/// <summary>
/// Reportes y apelación (spec 043, REP-01 a REP-03). La persona que reporta es siempre la del <c>sub</c> del token (R-24). La descripción pasa por la moderación (spec 031) antes de guardarse y,
/// si la moderación no responde, no se guarda nada (constitución §2.13). La persona reportada nunca ve quién la reportó ni el texto (R-22): este servicio solo devuelve identificadores del
/// propio reporte. Los registros no llevan el texto ni los identificadores de las dos personas juntos. No toca MongoDB.
/// </summary>
internal sealed class ReportService(ReportesStore store, IUsuariosApi users, IContentModerator moderator, TimeProvider clock)
{
    /// <summary>
    /// Crea el reporte (<c>abierto</c>, a la espera del panel de la spec 053). Un reporte contra uno mismo o contra una persona que no existe es <see cref="ReportOutcome.ReportedInvalid"/> (422);
    /// repetir el mismo reporte abierto devuelve el que ya existe. Lanza 422 <c>content_rejected</c> o 503 <c>moderation_unavailable</c> según la moderación.
    /// </summary>
    public async Task<ReportCreation> CreateAsync(string sub, CreateReportBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var reportedId = body.ReportadoId!;
        if (string.Equals(sub, reportedId, StringComparison.OrdinalIgnoreCase) || await users.FindPublicUserAsync(reportedId, cancellationToken) is null)
        {
            return new ReportCreation(ReportOutcome.ReportedInvalid);
        }

        if (await store.FindOpenDuplicateAsync(sub, reportedId, body.Motivo!, cancellationToken) is { } existing)
        {
            return new ReportCreation(ReportOutcome.AlreadyOpen, ToCreated(existing));
        }

        var text = body.Descripcion!.Trim();
        var review = await moderator.ReviewTextAsync(text, cancellationToken);
        if (review.Outcome == ModerationOutcome.Rejected)
        {
            throw new DomainException(DomainErrorKind.Unprocessable, "The description was rejected.", SafetyErrorCodes.ContentRejectedCode);
        }

        if (review.Outcome != ModerationOutcome.Allowed)
        {
            throw new DomainException(DomainErrorKind.ServiceUnavailable, "Moderation is not available.", SafetyErrorCodes.ModerationUnavailableCode);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var saved = await store.InsertAsync(
            new NewReport(sub, reportedId, body.Referencia?.Tipo, body.Referencia?.Id, body.Motivo!, text, body.Evidencia ?? [], now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMillisecond))),
            cancellationToken);
        return new ReportCreation(ReportOutcome.Created, ToCreated(saved));
    }

    /// <summary>Los reportes hechos por quien llama, del más reciente al más antiguo. Un cursor que no se puede leer lanza 400 (<c>invalid_safety_cursor</c>).</summary>
    public async Task<ReportPage> ListAsync(string sub, ReportsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var rows = await store.ListByReporterAsync(sub, SafetyCursor.Parse(query.Cursor), query.Limite + 1, cancellationToken);
        var page = rows.Take(query.Limite).ToList();
        return new ReportPage(
            [.. page.Select(r => new ReportSummary(r.Id.ToString(), r.Reason, r.State, r.CreatedAtUtc, ReportRules.CanBeAppealed(r.State, r.HasDecision, r.HasAppeal)))],
            rows.Count > query.Limite ? page[^1].Id.ToString() : null);
    }

    /// <summary>
    /// Apela la decisión (una sola vez). Quien no es una de las dos partes del reporte, o un reporte que no existe, lanza 403; sin decisión o con una apelación previa, 409
    /// <c>appeal_not_allowed</c>. Todavía no hay panel al que asignarla (spec 033): queda guardada y el reporte pasa a <c>en_apelacion</c>.
    /// </summary>
    public async Task<AppealResult> AppealAsync(string sub, string reportId, string text, CancellationToken cancellationToken)
    {
        var id = ObjectId.Parse(reportId);
        if (await store.FindAsync(id, cancellationToken) is not { } report
            || (!string.Equals(report.ReporterId, sub, StringComparison.OrdinalIgnoreCase) && !string.Equals(report.ReportedId, sub, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException(DomainErrorKind.Forbidden, "The person is not affected by this report.");
        }

        if (!ReportRules.CanBeAppealed(report.State, report.HasDecision, report.HasAppeal)
            || !await store.AppealAsync(id, text.Trim(), clock.GetUtcNow().UtcDateTime, cancellationToken))
        {
            throw new DomainException(DomainErrorKind.Conflict, "The report cannot be appealed.", SafetyErrorCodes.AppealNotAllowedCode);
        }

        return new AppealResult(reportId, ReportStates.Appealed);
    }

    private static ReportCreated ToCreated(StoredReport report) =>
        new(report.Id.ToString(), report.State, report.CreatedAtUtc, ReportRules.NoticeFor(report.Reason));
}
