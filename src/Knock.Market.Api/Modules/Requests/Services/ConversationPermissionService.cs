using Knock.Contracts.Conversations;
using Knock.Market.Api.Common.Errors;
using Knock.Market.Api.Modules.Requests.Data;
using Knock.Market.Api.Modules.Requests.Domain;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Requests.Services;

/// <summary>
/// Permiso de conversación por par (solicitud, técnico) (spec 030, R-02 y R-03). La persona sale siempre del <c>sub</c> del token: el cliente dueño debe decir el técnico y el técnico
/// no (si lo manda, se ignora: nunca habla en nombre de otro). Con una solicitud o un par que no existen, o de otra persona, devuelve <see langword="null"/> (404 uniforme). No toca MongoDB.
/// </summary>
internal sealed class ConversationPermissionService(RequestsStore requests, ConversationPermissionStore store)
{
    private const string _clientRole = "cliente";
    private const string _technicianRole = "tecnico";

    /// <summary>
    /// El permiso o <see langword="null"/> (404). Un <paramref name="requestId"/> o, para el cliente, un <paramref name="technicianId"/> que no son <c>ObjectId</c> lanzan 400.
    /// Sin envíos (spec 028 todavía sin colección <c>envios</c>), el técnico participa solo si cotizó la solicitud.
    /// </summary>
    public async Task<PermisoConversacion?> GetAsync(string sub, string requestId, string? technicianId, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(requestId, out var requestObjectId))
        {
            throw new DomainException(DomainErrorKind.BadRequest, "The request id is not valid.");
        }

        var request = await requests.FindAsync(requestObjectId, cancellationToken);
        if (request is null)
        {
            return null;
        }

        var isClient = string.Equals(request.ClienteId, sub, StringComparison.Ordinal);
        var technician = isClient
            ? await store.FindTechnicianAsync(ParseTechnician(technicianId), cancellationToken)
            : await store.FindTechnicianBySubAsync(sub, cancellationToken);
        if (technician is null || await store.FindQuoteStateAsync(requestObjectId, technician.Id, cancellationToken) is not { } quote)
        {
            return null;
        }

        var opening = ConversationRules.Evaluate(request.Estado, quote);
        var visitId = await store.FindVisitIdAsync(requestObjectId, technician.Id, cancellationToken);
        return new PermisoConversacion(
            requestObjectId.ToString(),
            technician.Id.ToString(),
            request.ClienteId,
            technician.UserId,
            isClient ? _clientRole : _technicianRole,
            opening.Open,
            opening.Reason,
            quote == QuoteState.Aceptada,
            visitId?.ToString(),
            request.OficioClave,
            request.ColoniaNombre,
            new TecnicoPublicoConversacion(technician.Name, technician.PhotoUrl));
    }

    private static ObjectId ParseTechnician(string? technicianId) =>
        ObjectId.TryParse(technicianId, out var parsed) ? parsed : throw new DomainException(DomainErrorKind.BadRequest, "The technician id is required and must be valid.");
}
