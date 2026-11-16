using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Validators;

namespace Knock.Market.Api.Modules.Requests.Services;

internal sealed record Technician(string UsuarioId, string Estado, bool Oculto, IReadOnlyList<string> Oficios,
    string? ColoniaBaseId, double? Latitude, double? Longitude, int RadioAtencionKm);

internal sealed record QuoteRequestSnapshot(string Trade, string ColoniaId, RequestState State,
    DateTimeOffset ExpiresAt, DateOnly Day, double? Latitude, double? Longitude);

internal interface ITechnicianDirectory
{
    Task<Technician?> FindBySubAsync(string sub, CancellationToken cancellationToken);
}

internal interface IQuoteRequestReader
{
    Task<QuoteRequestSnapshot?> FindAsync(string id, CancellationToken cancellationToken);
}

// Created means preliminary checks passed, not that a quote was persisted.
internal enum QuoteCheckResult { NotATechnician, NotFound, RequestNotQuotable, Invalid, Created }

internal sealed class QuoteService(ITechnicianDirectory technicians, IQuoteRequestReader requests, QuoteValidator validator, TimeProvider clock)
{
    public async Task<QuoteCheckResult> SendAsync(string sub, string solicitudId, QuoteValidationInput body, CancellationToken cancellationToken = default)
    {
        var technician = await technicians.FindBySubAsync(sub, cancellationToken);
        if (technician is null) return QuoteCheckResult.NotATechnician;
        var request = await requests.FindAsync(solicitudId, cancellationToken);
        if (request is null || technician.Estado != "activo" || technician.Oculto ||
            !technician.Oficios.Contains(request.Trade, StringComparer.Ordinal) || !Covers(technician, request))
            return QuoteCheckResult.NotFound;
        if (!RequestRules.AcceptsQuotes(request.State) || request.ExpiresAt <= clock.GetUtcNow())
            return QuoteCheckResult.RequestNotQuotable;
        return (await validator.ValidateAsync(body with { RequestDate = request.Day }, cancellationToken)).IsValid
            ? QuoteCheckResult.Created : QuoteCheckResult.Invalid;
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
