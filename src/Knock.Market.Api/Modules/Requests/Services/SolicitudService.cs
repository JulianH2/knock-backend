using Knock.Contracts.Moderation;
using Knock.Market.Api.Common.Crypto;
using Knock.Market.Api.Common.Errors;
using Knock.Market.Api.Modules.Catalog;
using Knock.Market.Api.Modules.Requests.Data;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Dtos;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Requests.Services;

/// <summary>Resultado de crear una solicitud. Solo <see cref="Created"/> trae la solicitud; los demás son reglas de negocio con su <c>code</c> (spec 020, §5).</summary>
internal enum CreateRequestStatus
{
    Created,
    TradeInvalid,
    ColoniaNotFound,
    AddressRequired,
    AddressNotApplicable,
    PhotoInvalid,
    ContentRejected,
    ModerationUnavailable,
}

internal sealed record CreateRequestOutcome(CreateRequestStatus Status, RequestResponse? Request = null);

/// <summary>
/// Crear, listar y ver solicitudes (spec 020, HU-024). Aplica las reglas del catálogo, las fotos, la moderación, el cifrado de la dirección y el buzón de salida, y arma
/// la vista del cliente o la del técnico. No toca MongoDB: las lecturas y escrituras pasan por <see cref="RequestsStore"/>. Nunca registra la descripción, la dirección ni las fotos (R-19).
/// </summary>
internal sealed class SolicitudService(
    ICatalogApi catalog,
    IRequestPhotos photos,
    IContentModerator moderator,
    FieldCipher cipher,
    RequestsStore store,
    ITechnicianDirectory technicians,
    IQuoteRequestReader quoteRequests,
    TimeProvider clock)
{
    private const string _domicile = "domicilio";
    private const string _workshop = "taller";
    private const string _createdEvent = "SolicitudCreada";

    public async Task<CreateRequestOutcome> CreateAsync(string clientId, CreateRequestBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);

        var trade = await catalog.FindActiveTradeAsync(body.OficioClave!, cancellationToken);
        if (trade is null)
        {
            return new CreateRequestOutcome(CreateRequestStatus.TradeInvalid);
        }

        var colonia = await catalog.FindNeighborhoodAsync(body.ColoniaId!, cancellationToken);
        if (colonia is null)
        {
            return new CreateRequestOutcome(CreateRequestStatus.ColoniaNotFound);
        }

        var hasAddress = !string.IsNullOrWhiteSpace(body.Direccion);
        if (trade.Value.Modalidad == _domicile && !hasAddress)
        {
            return new CreateRequestOutcome(CreateRequestStatus.AddressRequired);
        }

        if (trade.Value.Modalidad == _workshop && body.Direccion is not null)
        {
            return new CreateRequestOutcome(CreateRequestStatus.AddressNotApplicable);
        }

        var photoIds = body.Fotos!.Select(id => ObjectId.Parse(id).ToString()).ToList();
        if (!await photos.AreReadyForAsync(clientId, photoIds, cancellationToken))
        {
            return new CreateRequestOutcome(CreateRequestStatus.PhotoInvalid);
        }

        var review = await moderator.ReviewTextAsync(body.Descripcion!.Trim(), cancellationToken);
        if (review.Outcome != ModerationOutcome.Allowed)
        {
            return new CreateRequestOutcome(review.Outcome == ModerationOutcome.Rejected ? CreateRequestStatus.ContentRejected : CreateRequestStatus.ModerationUnavailable);
        }

        var now = clock.GetUtcNow();
        var id = ObjectId.GenerateNewId();
        var record = new RequestRecord(
            id,
            clientId,
            body.OficioClave!,
            trade.Value.Nombre,
            trade.Value.Modalidad,
            body.Descripcion.Trim(),
            photoIds,
            body.Dia!,
            body.Franja!,
            ObjectId.Parse(body.ColoniaId!).ToString(),
            colonia.Value.Nombre,
            colonia.Value.Cp,
            hasAddress ? cipher.Encrypt(body.Direccion!.Trim(), AddressContext(id)) : null,
            RequestState.Enviada,
            RequestRules.ExpiresAt(now).UtcDateTime,
            now.UtcDateTime);
        var message = OutboxMessage.Create(
            _createdEvent,
            id.ToString(),
            new BsonDocument { { "solicitudId", id.ToString() }, { "oficioClave", record.OficioClave }, { "coloniaId", record.ColoniaId }, { "venceEn", record.VenceEnUtc } },
            now.UtcDateTime);

        await store.InsertAsync(record, message, cancellationToken);
        return new CreateRequestOutcome(CreateRequestStatus.Created, await ClientViewAsync(record, totalQuotes: 0, cancellationToken));
    }

    /// <summary>Solicitudes del cliente, de la más reciente a la más antigua. Un cursor que no se puede decodificar lanza 400 <c>invalid_cursor</c>.</summary>
    public async Task<RequestPage> ListAsync(string clientId, ListRequestsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        (DateTime, ObjectId)? after = string.IsNullOrEmpty(query.Cursor) ? null : RequestCursor.Decode(query.Cursor);
        var page = await store.ListAsync(clientId, query.Estado, after, query.Limit, cancellationToken);
        var totals = await store.CountLiveQuotesAsync([.. page.Items.Select(item => item.Id)], cancellationToken);
        var items = page.Items.Select(item => new RequestSummary(
            item.Id.ToString(),
            RequestStates.ToStored(item.Estado),
            item.OficioNombre,
            item.ColoniaNombre,
            item.Dia,
            item.Franja,
            item.Fotos.Count,
            totals.GetValueOrDefault(item.Id),
            new DateTimeOffset(item.VenceEnUtc, TimeSpan.Zero),
            new DateTimeOffset(item.CreadoEnUtc, TimeSpan.Zero))).ToList();
        return new RequestPage(items, page.NextCursor);
    }

    /// <summary>
    /// Detalle según quién pide: la vista del cliente para su dueño; la del técnico para uno activo que ya cotizó o que cumple la elegibilidad base (R-23). Cualquier otra
    /// persona, y una solicitud que no existe, devuelven <see langword="null"/> (404 uniforme). Un <paramref name="id"/> que no es <c>ObjectId</c> lanza 400.
    /// </summary>
    public async Task<RequestResponse?> GetAsync(string sub, string id, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            throw new DomainException(DomainErrorKind.BadRequest, "The request id is not valid.");
        }

        var record = await store.FindAsync(objectId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        if (string.Equals(record.ClienteId, sub, StringComparison.Ordinal))
        {
            var totals = await store.CountLiveQuotesAsync([record.Id], cancellationToken);
            return await ClientViewAsync(record, totals.GetValueOrDefault(record.Id), cancellationToken);
        }

        return await TechnicianMaySeeAsync(sub, record, cancellationToken) ? await TechnicianViewAsync(record, cancellationToken) : null;
    }

    /// <summary>Elegibilidad base (R-23), verificada en cada petición: técnico activo y visible, y o ya cotizó, o tiene el oficio, atiende la colonia y la solicitud sigue cotizable.</summary>
    private async Task<bool> TechnicianMaySeeAsync(string sub, RequestRecord record, CancellationToken cancellationToken)
    {
        var technician = await technicians.FindBySubAsync(sub, cancellationToken);
        if (technician is null || technician.Estado != "activo" || technician.Oculto)
        {
            return false;
        }

        if (await store.HasQuotedAsync(sub, record.Id, cancellationToken))
        {
            return true;
        }

        var snapshot = await quoteRequests.FindAsync(record.Id.ToString(), cancellationToken);
        return snapshot is not null
            && technician.Oficios.Contains(snapshot.Trade, StringComparer.Ordinal)
            && QuoteService.Covers(technician, snapshot)
            && RequestRules.AcceptsQuotes(snapshot.State)
            && snapshot.ExpiresAt > clock.GetUtcNow();
    }

    private async Task<RequestResponse> ClientViewAsync(RequestRecord record, int totalQuotes, CancellationToken cancellationToken) =>
        ToResponse(record, await photos.ReadLinksAsync(record.Fotos, cancellationToken), record.DireccionCifrada is null ? null : cipher.Decrypt(record.DireccionCifrada, AddressContext(record.Id)), totalQuotes);

    private async Task<RequestResponse> TechnicianViewAsync(RequestRecord record, CancellationToken cancellationToken) =>
        ToResponse(record, await photos.ReadLinksAsync(record.Fotos, cancellationToken), address: null, totalQuotes: null);

    private static RequestResponse ToResponse(RequestRecord record, IReadOnlyList<RequestPhoto> links, string? address, int? totalQuotes) => new(
        record.Id.ToString(),
        RequestStates.ToStored(record.Estado),
        record.Modalidad,
        new RequestTrade(record.OficioClave, record.OficioNombre),
        new RequestNeighborhood(record.ColoniaId, record.ColoniaNombre, record.ColoniaCp),
        record.Descripcion,
        links,
        record.Dia,
        record.Franja,
        address,
        totalQuotes,
        new DateTimeOffset(record.VenceEnUtc, TimeSpan.Zero),
        new DateTimeOffset(record.CreadoEnUtc, TimeSpan.Zero));

    /// <summary>La dirección va atada a su solicitud: copiada a otro documento no se descifra.</summary>
    private static string AddressContext(ObjectId id) => $"solicitudes|direccion|{id}";
}
