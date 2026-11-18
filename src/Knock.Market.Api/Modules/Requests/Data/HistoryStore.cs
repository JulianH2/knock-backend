using Knock.Market.Api.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Requests.Data;

/// <summary>Una visita terminada ligada a un elemento de «Mi casa y mi auto» (spec 041, R-17): lo mínimo para armar fecha, oficio y técnico. Sin precios ni direcciones.</summary>
internal sealed record HistoryVisit(ObjectId Id, string Day, string Trade, ObjectId TechnicianId);

/// <summary>Una página de visitas terminadas y la posición de la siguiente (<c>Day</c> y <c>Id</c> de la última fila), o <see langword="null"/> en la última.</summary>
internal sealed record HistoryVisitPage(IReadOnlyList<HistoryVisit> Items, (string Day, ObjectId Id)? Next);

/// <summary>
/// Lectura del historial de un elemento (spec 041, R-17): las solicitudes del cliente que traen ese <c>elementoId</c> y sus visitas <c>terminada</c>. Solo lee; es el único punto del
/// módulo con acceso a MongoDB para esta consulta (constitución, principio 10). Los filtros son tipados y cada consulta lleva <c>maxTimeMS</c>.
/// </summary>
internal sealed class HistoryStore(
    [FromKeyedServices(RequestsModuleExtensions.ModuleKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> mongoOptions)
{
    /// <summary>Tope de solicitudes de un mismo elemento que se unen con sus visitas: acota la consulta sin recortar un uso normal (20 elementos por persona).</summary>
    public const int MaxLinkedRequests = 500;

    public const string FinishedState = "terminada";

    private readonly FindOptions _findOptions = new() { MaxTime = mongoOptions.Value.QueryMaxTime };

    private IMongoCollection<BsonDocument> Requests => database.GetCollection<BsonDocument>(RequestsStore.RequestsCollection);

    private IMongoCollection<BsonDocument> Visits => database.GetCollection<BsonDocument>(ChoiceStore.VisitsCollection);

    /// <summary>Nombre del oficio de cada solicitud del cliente ligada a ese elemento. Solo las del cliente dado: un <c>elementoId</c> ajeno nunca trae lo de otra persona (spec 041, §5).</summary>
    public async Task<IReadOnlyDictionary<ObjectId, string>> FindLinkedTradesAsync(string clientId, string elementId, CancellationToken cancellationToken)
    {
        var filter = Builders<BsonDocument>.Filter;
        var documents = await Requests.Find(filter.Eq("clienteId", clientId) & filter.Eq("elementoId", elementId), _findOptions)
            .Project(Builders<BsonDocument>.Projection.Include("oficioNombre").Include("oficioClave"))
            .Limit(MaxLinkedRequests)
            .ToListAsync(cancellationToken);
        return documents.ToDictionary(
            document => document["_id"].AsObjectId,
            document => document.TryGetValue("oficioNombre", out var name) && name.IsString ? name.AsString : document["oficioClave"].AsString);
    }

    /// <summary>
    /// Visitas terminadas de esas solicitudes, de la más reciente a la más antigua (<c>dia</c> y <c>_id</c> descendentes). Pide una fila de más para saber si hay otra página y arma la
    /// posición siguiente con la última fila devuelta.
    /// </summary>
    public async Task<HistoryVisitPage> ListFinishedVisitsAsync(
        IReadOnlyDictionary<ObjectId, string> trades, (string Day, ObjectId Id)? after, int limit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(trades);
        if (trades.Count == 0)
        {
            return new HistoryVisitPage([], null);
        }

        var filter = Builders<BsonDocument>.Filter;
        var conditions = new List<FilterDefinition<BsonDocument>> { filter.In("solicitudId", trades.Keys), filter.Eq("estado", FinishedState) };
        if (after is { } cursor)
        {
            conditions.Add(filter.Or(filter.Lt("dia", cursor.Day), filter.And(filter.Eq("dia", cursor.Day), filter.Lt("_id", cursor.Id))));
        }

        var documents = await Visits.Find(filter.And(conditions), _findOptions)
            .Project(Builders<BsonDocument>.Projection.Include("solicitudId").Include("dia").Include("tecnicoId"))
            .Sort(Builders<BsonDocument>.Sort.Descending("dia").Descending("_id"))
            .Limit(limit + 1)
            .ToListAsync(cancellationToken);
        var page = documents.Take(limit)
            .Select(document => new HistoryVisit(document["_id"].AsObjectId, document["dia"].AsString, trades[document["solicitudId"].AsObjectId], document["tecnicoId"].AsObjectId))
            .ToList();
        return new HistoryVisitPage(page, documents.Count > limit ? (page[^1].Day, page[^1].Id) : null);
    }
}
