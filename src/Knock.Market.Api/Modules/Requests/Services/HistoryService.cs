using Knock.Contracts.Pagination;
using Knock.Market.Api.Modules.Requests.Data;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Dtos;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Requests.Services;

/// <summary>
/// Historial de un elemento de «Mi casa y mi auto» (spec 041, R-17): las visitas terminadas del cliente ligadas a ese <c>elementoId</c>, con fecha, oficio y técnico, de la más reciente
/// a la más antigua y por cursor. Solo lee, nunca devuelve precios ni direcciones y nunca registra el cliente ni el elemento. Un <c>elementoId</c> que no es del cliente no trae nada,
/// porque solo se unen solicitudes cuyo <c>clienteId</c> sea el dado.
/// </summary>
internal sealed class HistoryService(HistoryStore store, ChoiceStore choices)
{
    public const string UnknownTechnician = "Técnico";

    public async Task<CursorPage<HistoryEntryResponse>> ListAsync(string clientId, string elementId, string? cursor, int limit, CancellationToken cancellationToken)
    {
        (string Day, ObjectId Id)? after = string.IsNullOrEmpty(cursor) ? null : HistoryCursor.Decode(cursor);
        var trades = await store.FindLinkedTradesAsync(clientId, ObjectId.Parse(elementId).ToString(), cancellationToken);
        var page = await store.ListFinishedVisitsAsync(trades, after, limit, cancellationToken);
        var technicians = await choices.FindTechniciansAsync([.. page.Items.Select(visit => visit.TechnicianId).Distinct()], withContact: false, cancellationToken);
        var items = page.Items
            .Select(visit => new HistoryEntryResponse(visit.Day, visit.Trade, technicians.TryGetValue(visit.TechnicianId, out var technician) ? technician.Name : UnknownTechnician))
            .ToList();
        return new CursorPage<HistoryEntryResponse>(items, page.Next is { } next ? HistoryCursor.Encode(next.Day, next.Id) : null);
    }
}
