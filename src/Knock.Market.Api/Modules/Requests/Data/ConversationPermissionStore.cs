using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Requests.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Requests.Data;

/// <summary>Un técnico tal como lo necesita el permiso de conversación: identificadores y datos públicos, nunca contacto (spec 030, R-02 y R-06).</summary>
internal sealed record ConversationTechnician(ObjectId Id, string UserId, string Name, string? PhotoUrl);

/// <summary>
/// Lecturas del permiso de conversación (spec 030, R-02): el técnico por su <c>_id</c> o por su <c>sub</c>, el estado de su cotización en una solicitud y la visita de la cita.
/// Son lecturas, sin escribir nada, sobre <c>tecnicos</c>, <c>cotizaciones</c> y <c>visitas</c>; las solicitudes las lee <see cref="RequestsStore"/>.
/// </summary>
internal sealed class ConversationPermissionStore(
    [FromKeyedServices(RequestsModuleExtensions.ModuleKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> mongoOptions)
{
    private const string _technicians = "tecnicos";
    private const string _idField = "_id";
    private const string _stateField = "estado";

    private static readonly ProjectionDefinition<BsonDocument> _technicianProjection =
        Builders<BsonDocument>.Projection.Include("usuarioId").Include("nombre").Include("fotoUrl");

    private readonly FindOptions _findOptions = new() { MaxTime = mongoOptions.Value.QueryMaxTime };

    public async Task<ConversationTechnician?> FindTechnicianAsync(ObjectId id, CancellationToken cancellationToken) =>
        await FindTechnicianAsync(Builders<BsonDocument>.Filter.Eq(_idField, id), cancellationToken);

    public async Task<ConversationTechnician?> FindTechnicianBySubAsync(string sub, CancellationToken cancellationToken) =>
        await FindTechnicianAsync(Builders<BsonDocument>.Filter.Eq("usuarioId", sub), cancellationToken);

    /// <summary>El estado de la cotización del técnico en la solicitud, o <see langword="null"/> si nunca cotizó (el par es único, spec 020).</summary>
    public async Task<QuoteState?> FindQuoteStateAsync(ObjectId requestId, ObjectId technicianId, CancellationToken cancellationToken)
    {
        var filter = Builders<BsonDocument>.Filter;
        var quote = await database.GetCollection<BsonDocument>(RequestsStore.QuotesCollection)
            .Find(filter.Eq("solicitudId", requestId) & filter.Eq("tecnicoId", technicianId), _findOptions)
            .Project(Builders<BsonDocument>.Projection.Include(_stateField))
            .FirstOrDefaultAsync(cancellationToken);
        return quote is null ? null : ToState(quote[_stateField].AsString);
    }

    /// <summary>La visita de la solicitud con ese técnico, o <see langword="null"/> si no fue el elegido.</summary>
    public async Task<ObjectId?> FindVisitIdAsync(ObjectId requestId, ObjectId technicianId, CancellationToken cancellationToken)
    {
        var filter = Builders<BsonDocument>.Filter;
        var visit = await database.GetCollection<BsonDocument>(ChoiceStore.VisitsCollection)
            .Find(filter.Eq("solicitudId", requestId) & filter.Eq("tecnicoId", technicianId), _findOptions)
            .Project(Builders<BsonDocument>.Projection.Include(_idField))
            .FirstOrDefaultAsync(cancellationToken);
        return visit?[_idField].AsObjectId;
    }

    private static QuoteState ToState(string state) => state switch
    {
        "aceptada" => QuoteState.Aceptada,
        "rechazada" => QuoteState.Rechazada,
        "retirada" => QuoteState.Retirada,
        _ => QuoteState.Enviada,
    };

    private async Task<ConversationTechnician?> FindTechnicianAsync(FilterDefinition<BsonDocument> filter, CancellationToken cancellationToken)
    {
        var document = await database.GetCollection<BsonDocument>(_technicians).Find(filter, _findOptions)
            .Project(_technicianProjection).FirstOrDefaultAsync(cancellationToken);
        if (document is null || !document.TryGetValue("usuarioId", out var userId) || !userId.IsString || !document.TryGetValue("nombre", out var name) || !name.IsString)
        {
            return null;
        }

        var photo = document.TryGetValue("fotoUrl", out var photoValue) && photoValue.IsString ? photoValue.AsString : null;
        return new ConversationTechnician(document[_idField].AsObjectId, userId.AsString, name.AsString, photo);
    }
}
