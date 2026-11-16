using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Services;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Requests.Data;

/// <summary>Cita creada por la elección, con lo que hace falta para armar la respuesta (el cifrado de la dirección se resuelve en el servicio).</summary>
internal sealed record ChosenVisit(
    ObjectId VisitId,
    ObjectId RequestId,
    ObjectId QuoteId,
    string Modality,
    string Day,
    string Time,
    int? Price,
    int Cap,
    ObjectId TechnicianId,
    string? EncryptedClientAddress);

internal sealed record ChooseResult(ChooseStatus Status, ChosenVisit? Visit = null);

/// <summary>Datos públicos de un técnico (spec 021, R-15): nada del celular personal que vive cifrado en Identidad.</summary>
internal sealed record TechnicianPublic(ObjectId Id, string Name, string? PhotoUrl, double? Rating, string VerificationLevel, string? Phone, string? WhatsApp, string? WorkshopAddress);

/// <summary>
/// Lecturas y la transacción de «elegir cotización» (spec 021): las cotizaciones de una solicitud, la proyección pública de <c>tecnicos</c> (solo lectura), las visitas
/// y la elección atómica. Único punto del módulo con acceso a MongoDB para estas operaciones (constitución, principio 10).
/// </summary>
internal sealed class ChoiceStore(
    [FromKeyedServices(RequestsModuleExtensions.ModuleKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> mongoOptions,
    IOutbox outbox,
    VisitCodeFactory codes)
{
    public const string VisitsCollection = "visitas";
    private const string _stateField = "estado";
    private const string _sent = "enviada";

    private static readonly string[] _listedStates = ["enviada", "aceptada", "rechazada"];
    private static readonly string[] _chosenStates = ["cita_confirmada", "terminada", "calificada"];

    private readonly FindOptions _findOptions = new() { MaxTime = mongoOptions.Value.QueryMaxTime };

    private IMongoCollection<BsonDocument> Requests => database.GetCollection<BsonDocument>(RequestsStore.RequestsCollection);

    private IMongoCollection<BsonDocument> Quotes => database.GetCollection<BsonDocument>(RequestsStore.QuotesCollection);

    private IMongoCollection<BsonDocument> Visits => database.GetCollection<BsonDocument>(VisitsCollection);

    /// <summary>Cotizaciones listables (no retiradas) de la solicitud, hasta el tope de cinco que fija la spec 020.</summary>
    public async Task<IReadOnlyList<BsonDocument>> ListQuotesAsync(ObjectId requestId, CancellationToken cancellationToken)
    {
        var filter = Builders<BsonDocument>.Filter;
        return await Quotes.Find(filter.Eq("solicitudId", requestId) & filter.In(_stateField, _listedStates), _findOptions)
            .Sort(Builders<BsonDocument>.Sort.Ascending("creadoEn").Ascending("_id"))
            .Limit(RequestRules.MaxLiveQuotes)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<ObjectId, TechnicianPublic>> FindTechniciansAsync(IReadOnlyCollection<ObjectId> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<ObjectId, TechnicianPublic>();
        }

        var projection = Builders<BsonDocument>.Projection.Include("nombre").Include("fotoUrl").Include("calificacion").Include("nivelVerificacion")
            .Include("contactoPublico").Include("tallerDireccion");
        var documents = await database.GetCollection<BsonDocument>("tecnicos")
            .Find(Builders<BsonDocument>.Filter.In("_id", ids), _findOptions).Project(projection).ToListAsync(cancellationToken);
        return documents.ToDictionary(d => d["_id"].AsObjectId, ToTechnician);
    }

    /// <summary>Identificadores de visita de las solicitudes dadas; las que no tienen cita no salen (R-22).</summary>
    public async Task<IReadOnlyDictionary<ObjectId, ObjectId>> FindVisitIdsAsync(IReadOnlyCollection<ObjectId> requestIds, CancellationToken cancellationToken)
    {
        if (requestIds.Count == 0)
        {
            return new Dictionary<ObjectId, ObjectId>();
        }

        var documents = await Visits.Find(Builders<BsonDocument>.Filter.In("solicitudId", requestIds), _findOptions)
            .Project(Builders<BsonDocument>.Projection.Include("solicitudId")).ToListAsync(cancellationToken);
        return documents.ToDictionary(d => d["solicitudId"].AsObjectId, d => d["_id"].AsObjectId);
    }

    /// <summary>
    /// Elige la cotización en una sola transacción: la solicitud pasa a <c>cita_confirmada</c> solo si está en <c>cotizando</c> y no venció, la cotización elegida a
    /// <c>aceptada</c> solo si estaba <c>enviada</c>, las demás <c>enviada</c> a <c>rechazada</c>, se crea la visita y se escriben los eventos (R-10). Si la base falla,
    /// se reintenta una vez; si vuelve a fallar, <see cref="ChooseStatus.ServiceUnavailable"/> sin haber cambiado nada (R-20).
    /// </summary>
    public async Task<ChooseResult> ChooseAsync(ObjectId requestId, ObjectId quoteId, string clientId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                return await ChooseOnceAsync(requestId, quoteId, clientId, nowUtc, cancellationToken);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return new ChooseResult(ChooseStatus.AlreadyChosen);
            }
            catch (Exception ex) when (ex is MongoException or TimeoutException)
            {
                // Un fallo transitorio se reintenta una vez; el segundo cae al 503 de abajo, sin haber cambiado nada.
            }
        }

        return new ChooseResult(ChooseStatus.ServiceUnavailable);
    }

    private async Task<ChooseResult> ChooseOnceAsync(ObjectId requestId, ObjectId quoteId, string clientId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        using var session = await database.Client.StartSessionAsync(cancellationToken: cancellationToken);
        return await session.WithTransactionAsync(async (transaction, token) =>
            {
                var filter = Builders<BsonDocument>.Filter;
                var request = await Requests.Find(transaction, filter.Eq("_id", requestId) & filter.Eq("clienteId", clientId), _findOptions).FirstOrDefaultAsync(token);
                if (request is null)
                {
                    return new ChooseResult(ChooseStatus.NotFound);
                }

                if (ClassifyRequest(request, nowUtc) is { } blocked)
                {
                    return new ChooseResult(blocked);
                }

                // Lo primero que se escribe es la solicitud, con su condición: así dos elecciones simultáneas chocan aquí (una espera o se reintenta) y la segunda ve
                // la cita ya confirmada, sin depender de lo que haya leído antes.
                var claimed = await Requests.UpdateOneAsync(
                    transaction,
                    filter.Eq("_id", requestId) & filter.Eq(_stateField, "cotizando") & filter.Gt("venceEn", nowUtc),
                    Builders<BsonDocument>.Update.Set(_stateField, "cita_confirmada").Set("cotizacionElegidaId", quoteId).Set("actualizadoEn", nowUtc),
                    cancellationToken: token);
                if (claimed.MatchedCount != 1)
                {
                    var current = await Requests.Find(transaction, filter.Eq("_id", requestId), _findOptions).FirstOrDefaultAsync(token);
                    return new ChooseResult(current is null ? ChooseStatus.NotFound : ClassifyRequest(current, nowUtc) ?? ChooseStatus.AlreadyChosen);
                }

                var accepted = await Quotes.FindOneAndUpdateAsync(
                    transaction,
                    filter.Eq("_id", quoteId) & filter.Eq("solicitudId", requestId) & filter.Eq(_stateField, _sent),
                    Builders<BsonDocument>.Update.Set(_stateField, "aceptada").Set("aceptadaEn", nowUtc).Set("actualizadoEn", nowUtc),
                    new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After },
                    token);
                if (accepted is null)
                {
                    // Se cancela la transacción para deshacer el cambio de la solicitud; la cotización es de otra solicitud (404) o ya no está enviada (409).
                    var belongs = await Quotes.Find(transaction, filter.Eq("_id", quoteId) & filter.Eq("solicitudId", requestId), _findOptions).AnyAsync(token);
                    await transaction.AbortTransactionAsync(token);
                    return new ChooseResult(belongs ? ChooseStatus.QuoteUnavailable : ChooseStatus.NotFound);
                }

                var rejected = await Quotes.Find(transaction, filter.Eq("solicitudId", requestId) & filter.Eq(_stateField, _sent), _findOptions).ToListAsync(token);
                var visit = await WriteChoiceAsync(transaction, request, accepted, rejected, nowUtc, token);
                return new ChooseResult(ChooseStatus.Chosen, visit);
            }, cancellationToken: cancellationToken);
    }

    private async Task<ChosenVisit> WriteChoiceAsync(IClientSessionHandle transaction, BsonDocument request, BsonDocument chosen, List<BsonDocument> rejected, DateTime nowUtc, CancellationToken token)
    {
        var filter = Builders<BsonDocument>.Filter;
        var requestId = request["_id"].AsObjectId;
        var quoteId = chosen["_id"].AsObjectId;

        if (rejected.Count > 0)
        {
            await Quotes.UpdateManyAsync(
                transaction,
                filter.In("_id", rejected.Select(q => q["_id"].AsObjectId)) & filter.Eq(_stateField, _sent),
                Builders<BsonDocument>.Update.Set(_stateField, "rechazada").Set("motivoRechazo", "other_quote_chosen").Set("actualizadoEn", nowUtc),
                cancellationToken: token);
        }

        var visitId = ObjectId.GenerateNewId();
        var modality = request["modalidad"].AsString;
        var day = chosen["dia"].AsString;
        var price = chosen["precio"].IsBsonNull ? (int?)null : chosen["precio"].ToInt32();
        var technicianId = chosen["tecnicoId"].AsObjectId;
        var visit = new BsonDocument
        {
            { "_id", visitId },
            { "solicitudId", requestId },
            { "cotizacionId", quoteId },
            { "clienteId", request["clienteId"].AsString },
            { "tecnicoUsuarioId", chosen["tecnicoUsuarioId"].AsString },
            { "tecnicoId", technicianId },
            { "modalidad", modality },
            { "dia", day },
            { "hora", chosen["hora"].AsString },
            { "precio", price is { } p ? p : BsonNull.Value },
            { "tope", chosen["tope"].ToInt32() },
            { _stateField, "confirmada" },
        };
        if (modality == "domicilio")
        {
            var code = codes.Create(visitId, day);
            visit.Add("codigoCifrado", code.Encrypted);
            visit.Add("codigoVenceEn", code.ExpiresAtUtc);
        }

        visit.Add("codigoUsado", false);
        visit.Add("intentos", 0);
        visit.Add("bloqueada", false);
        visit.Add("creadoEn", nowUtc);
        visit.Add("actualizadoEn", nowUtc);
        await Visits.InsertOneAsync(transaction, visit, cancellationToken: token);

        await WriteEventsAsync(transaction, request, chosen, rejected, visitId, nowUtc, token);
        return new ChosenVisit(
            visitId,
            requestId,
            quoteId,
            modality,
            day,
            chosen["hora"].AsString,
            price,
            chosen["tope"].ToInt32(),
            technicianId,
            request.TryGetValue("direccionCifrada", out var address) && address.IsString ? address.AsString : null);
    }

    /// <summary>Un evento por cotización aceptada, uno por cada rechazada y uno de cita confirmada; solo identificadores y datos no personales (R-18).</summary>
    private async Task WriteEventsAsync(IClientSessionHandle transaction, BsonDocument request, BsonDocument chosen, List<BsonDocument> rejected, ObjectId visitId, DateTime nowUtc, CancellationToken token)
    {
        var requestId = request["_id"].AsObjectId.ToString();
        var quoteId = chosen["_id"].AsObjectId.ToString();
        await outbox.AddAsync(
            transaction,
            OutboxMessage.Create("CotizacionAceptada", quoteId, new BsonDocument { { "cotizacionId", quoteId }, { "solicitudId", requestId }, { "tecnicoUsuarioId", chosen["tecnicoUsuarioId"].AsString } }, nowUtc),
            token);
        foreach (var other in rejected)
        {
            var id = other["_id"].AsObjectId.ToString();
            await outbox.AddAsync(
                transaction,
                OutboxMessage.Create(
                    "CotizacionRechazada",
                    id,
                    new BsonDocument { { "cotizacionId", id }, { "solicitudId", requestId }, { "tecnicoUsuarioId", other["tecnicoUsuarioId"].AsString }, { "motivo", "other_quote_chosen" } },
                    nowUtc),
                token);
        }

        await outbox.AddAsync(
            transaction,
            OutboxMessage.Create(
                "VisitaConfirmada",
                visitId.ToString(),
                new BsonDocument
                {
                    { "visitaId", visitId.ToString() },
                    { "solicitudId", requestId },
                    { "cotizacionId", quoteId },
                    { "clienteId", request["clienteId"].AsString },
                    { "tecnicoUsuarioId", chosen["tecnicoUsuarioId"].AsString },
                    { "modalidad", request["modalidad"].AsString },
                    { "dia", chosen["dia"].AsString },
                    { "hora", chosen["hora"].AsString },
                },
                nowUtc),
            token);
    }

    /// <summary>Estado de la solicitud que impide elegir, o <see langword="null"/> si está <c>cotizando</c> y vigente (R-13).</summary>
    private static ChooseStatus? ClassifyRequest(BsonDocument request, DateTime nowUtc)
    {
        var state = request[_stateField].AsString;
        if (_chosenStates.Contains(state, StringComparer.Ordinal))
        {
            return ChooseStatus.AlreadyChosen;
        }

        if (state == "cancelada")
        {
            return ChooseStatus.Cancelled;
        }

        if (state == "vencida" || request["venceEn"].ToUniversalTime() <= nowUtc)
        {
            return ChooseStatus.Expired;
        }

        return state == "cotizando" ? null : ChooseStatus.WithoutQuotes;
    }

    private static TechnicianPublic ToTechnician(BsonDocument document)
    {
        var contact = document.TryGetValue("contactoPublico", out var value) && value.IsBsonDocument ? value.AsBsonDocument : new BsonDocument();
        return new TechnicianPublic(
            document["_id"].AsObjectId,
            Text(document, "nombre") ?? "Técnico",
            Text(document, "fotoUrl"),
            document.TryGetValue("calificacion", out var rating) && rating.IsNumeric ? rating.ToDouble() : null,
            Text(document, "nivelVerificacion") ?? QuoteComparer.NoVerification,
            Text(contact, "telefono"),
            Text(contact, "whatsapp"),
            Text(document, "tallerDireccion"));
    }

    private static string? Text(BsonDocument document, string name) => document.TryGetValue(name, out var value) && value.IsString ? value.AsString : null;
}
