using System.Globalization;
using Knock.Market.Api.Common.Errors;
using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Dtos;
using Knock.Market.Api.Modules.Requests.Services;
using Knock.Market.Api.Modules.Requests.Validators;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Requests.Data;

internal sealed class QuoteStore(
    [FromKeyedServices(RequestsModuleExtensions.ModuleKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> options,
    IOptions<QuotePricingOptions> pricing,
    IOptions<OleadasOptions> waves,
    EnviosStore sends) : IQuoteWriter, ICreatedQuote
{
    public QuoteResponse? CreatedQuote { get; private set; }
    private static readonly string[] _openStates = ["enviada", "cotizando"];
    private static readonly string[] _liveStates = ["enviada", "aceptada"];

    public async Task<MyQuotePage> ListAsync(string subject, string? state, (DateTime CreatedAtUtc, ObjectId Id)? after, int limit, CancellationToken cancellationToken)
    {
        var filter = Builders<BsonDocument>.Filter;
        var conditions = new List<FilterDefinition<BsonDocument>> { filter.Eq("tecnicoUsuarioId", subject) };
        if (state is not null) conditions.Add(filter.Eq("estado", state));
        if (after is { } cursor)
            conditions.Add(filter.Or(filter.Lt("creadoEn", cursor.CreatedAtUtc),
                filter.And(filter.Eq("creadoEn", cursor.CreatedAtUtc), filter.Lt("_id", cursor.Id))));
        var documents = await database.GetCollection<BsonDocument>("cotizaciones")
            .Find(filter.And(conditions), new FindOptions { MaxTime = options.Value.QueryMaxTime })
            .Sort(Builders<BsonDocument>.Sort.Descending("creadoEn").Descending("_id"))
            .Limit(limit + 1).ToListAsync(cancellationToken);
        var items = documents.Take(limit).Select(ToMyQuote).ToList();
        var next = documents.Count > limit ? RequestCursor.Encode(documents[limit - 1]["creadoEn"].ToUniversalTime(), documents[limit - 1]["_id"].AsObjectId) : null;
        return new MyQuotePage(items, next);
    }

    private static MyQuote ToMyQuote(BsonDocument document)
    {
        static int? Amount(BsonDocument doc, string field) => doc.TryGetValue(field, out var value) && !value.IsBsonNull ? value.ToInt32() : null;
        return new MyQuote(document["_id"].ToString()!, document["solicitudId"].ToString()!, document["estado"].AsString,
            document["tipoPrecio"].AsString, Amount(document, "precio"), Amount(document, "precioMin"), Amount(document, "precioMax"),
            document["tope"].ToInt32(), document["dia"].AsString, document["hora"].AsString, document["creadoEn"].ToUniversalTime());
    }
    public Task<QuoteCheckResult> SaveAsync(string requestId, string technicianId, string sub, string trade, QuoteValidationInput body, DateTimeOffset now, CancellationToken cancellationToken) =>
        SaveAsync(ObjectId.Parse(requestId), ObjectId.Parse(technicianId), sub, trade, body, now, cancellationToken);

    public async Task<QuoteCheckResult> SaveAsync(ObjectId requestId, ObjectId technicianId, string sub, string trade, QuoteValidationInput body, DateTimeOffset now, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                return await SaveOnceAsync(requestId, technicianId, sub, trade, body, now, cancellationToken);
            }
            catch (MongoCommandException ex) when (ex.Code == 112)
            {
                await Task.Yield();
            }
        }
        return await SaveOnceAsync(requestId, technicianId, sub, trade, body, now, cancellationToken);
    }

    private async Task<QuoteCheckResult> SaveOnceAsync(ObjectId requestId, ObjectId technicianId, string sub, string trade, QuoteValidationInput body, DateTimeOffset now, CancellationToken cancellationToken)
    {
        using var session = await database.Client.StartSessionAsync(cancellationToken: cancellationToken);
        try
        {
            return await session.WithTransactionAsync(async (transaction, token) =>
            {
                var quotes = database.GetCollection<BsonDocument>("cotizaciones");
                var filters = Builders<BsonDocument>.Filter;
                var pair = filters.Eq("solicitudId", requestId) & filters.Eq("tecnicoId", technicianId);
                var findOptions = new FindOptions { MaxTime = options.Value.QueryMaxTime };
                if (waves.Value.Habilitado && !await sends.MarkQuotedAsync(transaction, requestId, technicianId, now.UtcDateTime, token))
                    return QuoteCheckResult.NotFound;
                if (await quotes.Find(transaction, pair, findOptions).FirstOrDefaultAsync(token) is not null) throw new DomainException(DomainErrorKind.Conflict, "quote_duplicate");
                await ReserveQuotaAsync(transaction, requestId, now, token);
                var quoteId = ObjectId.GenerateNewId();
                var quote = CreateQuote(quoteId, requestId, technicianId, sub, trade, body, now);
                await quotes.InsertOneAsync(transaction, quote, cancellationToken: token);
                var message = new BsonDocument { ["_id"] = quoteId, ["tipo"] = "CotizacionEnviada", ["estado"] = "pendiente", ["creadoEn"] = now.UtcDateTime, ["solicitudId"] = requestId, ["cotizacionId"] = quoteId };
                await database.GetCollection<BsonDocument>("outbox").InsertOneAsync(transaction, message, cancellationToken: token);
                CreatedQuote = new QuoteResponse(quoteId.ToString(), requestId.ToString(), "enviada", body.TipoPrecio!, body.Precio,
                    body.PrecioMin, body.PrecioMax, quote["tope"].AsInt32, quote["requiereAprobacion"].AsBoolean,
                    body.Dia!.Value, body.Hora!, body.Nota, now);
                return QuoteCheckResult.Created;
            }, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return QuoteCheckResult.QuoteDuplicate;
        }
        catch (DomainException ex) when (ex.Kind == DomainErrorKind.Conflict && ex.Message is "quote_duplicate" or "quote_quota_full" or "request_not_quotable")
        {
            return ex.Message switch { "quote_duplicate" => QuoteCheckResult.QuoteDuplicate, "request_not_quotable" => QuoteCheckResult.RequestNotQuotable, _ => QuoteCheckResult.QuoteQuotaFull };
        }
    }

    private async Task ReserveQuotaAsync(IClientSessionHandle session, ObjectId requestId, DateTimeOffset now, CancellationToken token)
    {
        var requests = database.GetCollection<BsonDocument>("solicitudes");
        var quotes = database.GetCollection<BsonDocument>("cotizaciones");
        var f = Builders<BsonDocument>.Filter;
        var uninitialized = f.Eq("_id", requestId) & (f.Exists("cotizacionesActivas", false) | f.Eq("cotizacionesActivas", 0));
        if (await requests.Find(session, uninitialized).AnyAsync(token))
        {
            var live = f.Eq("solicitudId", requestId) & f.In("estado", _liveStates);
            var count = await quotes.CountDocumentsAsync(session, live, new CountOptions { MaxTime = options.Value.QueryMaxTime }, token);
            await requests.UpdateOneAsync(session, uninitialized, Builders<BsonDocument>.Update.Set("cotizacionesActivas", checked((int)count)), cancellationToken: token);
        }
        var open = f.Eq("_id", requestId) & f.In("estado", _openStates) & f.Gt("venceEn", now.UtcDateTime);
        var changed = await requests.UpdateOneAsync(session, open & f.Lt("cotizacionesActivas", waves.Value.MaximoCotizaciones),
            Builders<BsonDocument>.Update.Set("estado", "cotizando").Set("actualizadoEn", now.UtcDateTime).Inc("cotizacionesActivas", 1), cancellationToken: token);
        if (changed.MatchedCount != 0) return;
        if (!await requests.Find(session, open).AnyAsync(token))
            throw new DomainException(DomainErrorKind.Conflict, "request_not_quotable");
        throw new DomainException(DomainErrorKind.Conflict, "quote_quota_full");
    }

    private BsonDocument CreateQuote(ObjectId id, ObjectId requestId, ObjectId technicianId, string sub, string trade, QuoteValidationInput body, DateTimeOffset now)
    {
        var type = body.TipoPrecio switch { "fijo" => PriceType.Fijo, "rango" => PriceType.Rango, "visita" => PriceType.Visita, _ => throw new InvalidOperationException("Invalid quote type.") };
        static int? Amount(decimal? value) => value.HasValue ? decimal.ToInt32(value.Value) : null;
        var cap = QuotePricing.Calculate(type, trade, Amount(body.Precio), Amount(body.PrecioMin), Amount(body.PrecioMax), pricing.Value);
        return new BsonDocument
        {
            ["_id"] = id,
            ["solicitudId"] = requestId,
            ["tecnicoId"] = technicianId,
            ["tecnicoUsuarioId"] = sub,
            ["tipoPrecio"] = body.TipoPrecio,
            ["precio"] = BsonValue.Create(Amount(body.Precio)),
            ["precioMin"] = BsonValue.Create(Amount(body.PrecioMin)),
            ["precioMax"] = BsonValue.Create(Amount(body.PrecioMax)),
            ["tope"] = cap.Tope,
            ["requiereAprobacion"] = cap.RequiereAprobacion,
            ["dia"] = body.Dia!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["hora"] = body.Hora,
            ["nota"] = BsonValue.Create(body.Nota),
            ["estado"] = "enviada",
            ["creadoEn"] = now.UtcDateTime,
            ["actualizadoEn"] = now.UtcDateTime,
        };
    }

}
