using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Validators;

namespace Knock.Market.Api.Modules.Requests.Services;

internal sealed record Technician(string UsuarioId, string Estado, bool Oculto, IReadOnlyList<string> Oficios,
    string? ColoniaBaseId, double? Latitude, double? Longitude, int RadioAtencionKm, string Id = "");

internal sealed record QuoteRequestSnapshot(string Trade, string ColoniaId, RequestState State,
    DateTimeOffset ExpiresAt, DateOnly Day, double? Latitude, double? Longitude);

internal interface ITechnicianDirectory
{
    Task<Technician?> FindBySubAsync(string sub, CancellationToken cancellationToken);
    Task<Technician?> FindEligibleAsync(string sub, QuoteRequestSnapshot request, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string sub, CancellationToken cancellationToken);
}

internal interface IQuoteRequestReader
{
    Task<QuoteRequestSnapshot?> FindAsync(string id, CancellationToken cancellationToken);
}

internal enum QuoteCheckResult { NotATechnician, NotFound, RequestNotQuotable, Invalid, ModerationUnavailable, ContentRejected, QuoteDuplicate, QuoteQuotaFull, Created }

internal interface IQuoteWriter
{
    Task<QuoteCheckResult> SaveAsync(string requestId, string technicianId, string sub, string trade, QuoteValidationInput body, DateTimeOffset now, CancellationToken cancellationToken);
}

internal sealed record QuoteResponse(string Id, string SolicitudId, string Estado, string TipoPrecio, decimal? Precio,
    decimal? PrecioMin, decimal? PrecioMax, int Tope, bool RequiereAprobacion, DateOnly Dia, string Hora, string? Nota, DateTimeOffset CreadoEn);

internal interface ICreatedQuote
{
    QuoteResponse? CreatedQuote { get; }
}

internal enum NoteModerationResult { Approved, Rejected, Unavailable }

internal interface INoteModerator
{
    Task<NoteModerationResult> ModerateAsync(string note, CancellationToken cancellationToken);
}

internal sealed class UnavailableNoteModerator : INoteModerator
{
    public Task<NoteModerationResult> ModerateAsync(string note, CancellationToken cancellationToken) => Task.FromResult(NoteModerationResult.Unavailable);
}

internal sealed class QuoteService(ITechnicianDirectory technicians, IQuoteRequestReader requests, QuoteValidator validator, TimeProvider clock, INoteModerator moderator, IQuoteWriter writer)
{
    public QuoteResponse? CreatedQuote => (writer as ICreatedQuote)?.CreatedQuote;
    public IDictionary<string, string[]> Errors { get; private set; } = new Dictionary<string, string[]>();
    public async Task<QuoteCheckResult> SendAsync(string sub, string solicitudId, QuoteValidationInput body, CancellationToken cancellationToken = default)
    {
        var technician = await technicians.FindBySubAsync(sub, cancellationToken);
        if (technician is null)
            return await technicians.ExistsAsync(sub, cancellationToken) ? QuoteCheckResult.NotFound : QuoteCheckResult.NotATechnician;
        var request = await requests.FindAsync(solicitudId, cancellationToken);
        if (request is null || await technicians.FindEligibleAsync(sub, request, cancellationToken) is null || technician.Estado != "activo" || technician.Oculto ||
            !technician.Oficios.Contains(request.Trade, StringComparer.Ordinal) || !Covers(technician, request))
            return QuoteCheckResult.NotFound;
        if (!RequestRules.AcceptsQuotes(request.State) || request.ExpiresAt <= clock.GetUtcNow())
            return QuoteCheckResult.RequestNotQuotable;
        var validation = await validator.ValidateAsync(body with { RequestDate = request.Day }, cancellationToken);
        if (!validation.IsValid)
        {
            Errors = validation.Errors.GroupBy(x => char.ToLowerInvariant(x.PropertyName[0]) + x.PropertyName[1..])
                .ToDictionary(x => x.Key, x => x.Select(e => e.ErrorMessage).ToArray());
            return QuoteCheckResult.Invalid;
        }
        if (body.Nota is not null)
        {
            var moderation = await moderator.ModerateAsync(body.Nota, cancellationToken);
            if (moderation == NoteModerationResult.Unavailable) return QuoteCheckResult.ModerationUnavailable;
            if (moderation != NoteModerationResult.Approved) return QuoteCheckResult.ContentRejected;
        }
        return await writer.SaveAsync(solicitudId, technician.Id, sub, request.Trade, body, clock.GetUtcNow(), cancellationToken);
    }

    /// <summary>Cobertura del técnico (colonia base o radio de atención). Internal para que el detalle de solicitudes aplique la misma elegibilidad base (spec 020, R-23).</summary>
    internal static bool Covers(Technician technician, QuoteRequestSnapshot request)
    {
        if (request.Latitude is null || request.Longitude is null)
            return technician.ColoniaBaseId == request.ColoniaId;
        if (technician.Latitude is null || technician.Longitude is null || technician.RadioAtencionKm is < 1 or > 30)
            return false;
        const double radians = Math.PI / 180;
        var a = Math.Pow(Math.Sin((request.Latitude.Value - technician.Latitude.Value) * radians / 2), 2)
            + Math.Cos(request.Latitude.Value * radians) * Math.Cos(technician.Latitude.Value * radians)
            * Math.Pow(Math.Sin((request.Longitude.Value - technician.Longitude.Value) * radians / 2), 2);
        return 12742 * Math.Asin(Math.Sqrt(Math.Clamp(a, 0, 1))) <= technician.RadioAtencionKm;
    }
}
