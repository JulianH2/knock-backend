using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.HomeAndCar.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Identity.Api.Modules.HomeAndCar.Data;

/// <summary>Lo que «Mi casa y mi auto» necesita de la colección <c>mi_casa_y_auto</c>; existe para probar el servicio con dobles. Cada método recibe el <c>sub</c> del token y filtra por él (spec 041, R-16).</summary>
internal interface IHomeAndCarStore
{
    /// <summary>Los elementos de la persona, del más reciente al más antiguo.</summary>
    Task<IReadOnlyList<HomeAndCarItem>> ListAsync(string ownerId, CancellationToken cancellationToken);

    /// <summary>Un elemento de la persona, o <see langword="null"/> si no existe o es de otra (404 uniforme).</summary>
    Task<HomeAndCarItem?> FindAsync(string ownerId, ObjectId id, CancellationToken cancellationToken);

    /// <summary>Guarda el elemento si la persona tiene menos de <paramref name="maxItems"/>; si no, no deja nada guardado.</summary>
    Task<AddItemOutcome> AddAsync(HomeAndCarItem item, int maxItems, CancellationToken cancellationToken);

    /// <summary>
    /// Reemplaza tipo, nombre y año de un elemento propio. Solo se aplica si el tipo guardado sigue siendo <paramref name="expectedType"/> (así dos cambios a la vez no dejan un año
    /// en un elemento que ya no es auto). Devuelve el elemento nuevo o <see langword="null"/> si ya no existe o cambió.
    /// </summary>
    Task<HomeAndCarItem?> UpdateAsync(string ownerId, ObjectId id, string expectedType, string type, string name, int? year, CancellationToken cancellationToken);

    /// <summary>Quita un elemento propio; <see langword="false"/> si no existe o es de otra persona.</summary>
    Task<bool> DeleteAsync(string ownerId, ObjectId id, CancellationToken cancellationToken);
}

/// <summary>
/// Lectura y escritura de <c>mi_casa_y_auto</c> (spec 041, §6). Los filtros son tipados del driver y la persona es siempre el <c>sub</c> del token. El límite de 20 no se cuenta antes
/// de guardar (dos peticiones a la vez lo rebasarían): se guarda y se comprueba que el elemento quede entre los 20 primeros de la persona; si no, se retira (R-15, CA-25).
/// </summary>
internal sealed class HomeAndCarStore(
    [FromKeyedServices(DataExtensions.DatabaseKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> mongoOptions) : IHomeAndCarStore
{
    public const string CollectionName = "mi_casa_y_auto";
    public const string OwnerField = "usuarioId";
    public const string TypeField = "tipo";
    public const string NameField = "nombre";
    public const string YearField = "anio";
    public const string CreatedAtField = "creadoEn";

    private static readonly FilterDefinitionBuilder<BsonDocument> _filter = Builders<BsonDocument>.Filter;

    private readonly FindOptions _findOptions = new() { MaxTime = mongoOptions.Value.QueryMaxTime };

    private IMongoCollection<BsonDocument> Items { get; } = database.GetCollection<BsonDocument>(CollectionName);

    /// <summary>Crea el índice de §6 (usado por las pruebas; en los entornos lo aplica <c>db/041-mi-casa-y-mi-auto.js</c>).</summary>
    public Task EnsureIndexesAsync(CancellationToken cancellationToken) =>
        Items.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending(OwnerField).Ascending(TypeField),
                new CreateIndexOptions { Name = "usuarioId_1_tipo_1" }),
            cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<HomeAndCarItem>> ListAsync(string ownerId, CancellationToken cancellationToken)
    {
        var documents = await Items.Find(_filter.Eq(OwnerField, ownerId), _findOptions)
            .Sort(Builders<BsonDocument>.Sort.Descending(CreatedAtField).Descending("_id"))
            .Limit(HomeAndCarRules.MaxItemsPerPerson)
            .ToListAsync(cancellationToken);
        return [.. documents.Select(ToItem)];
    }

    public async Task<HomeAndCarItem?> FindAsync(string ownerId, ObjectId id, CancellationToken cancellationToken) =>
        ToItemOrNull(await Items.Find(Own(ownerId, id), _findOptions).Limit(1).FirstOrDefaultAsync(cancellationToken));

    public async Task<AddItemOutcome> AddAsync(HomeAndCarItem item, int maxItems, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        var owner = _filter.Eq(OwnerField, item.OwnerId);
        if (await Items.CountDocumentsAsync(owner, new CountOptions { Limit = maxItems, MaxTime = mongoOptions.Value.QueryMaxTime }, cancellationToken) >= maxItems)
        {
            return AddItemOutcome.LimitReached;
        }

        await Items.InsertOneAsync(ToDocument(item), cancellationToken: cancellationToken);
        var kept = await Items.Find(owner, _findOptions)
            .Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
            .Limit(maxItems)
            .Project(Builders<BsonDocument>.Projection.Include("_id"))
            .ToListAsync(cancellationToken);
        if (kept.Any(document => document["_id"].AsObjectId == item.Id))
        {
            return AddItemOutcome.Created;
        }

        // Otra petición simultánea llenó el cupo antes: este elemento no cabe y se retira. El orden por _id es el mismo para todas, así que nunca quedan más de 20.
        await Items.DeleteOneAsync(Own(item.OwnerId, item.Id), cancellationToken);
        return AddItemOutcome.LimitReached;
    }

    public async Task<HomeAndCarItem?> UpdateAsync(string ownerId, ObjectId id, string expectedType, string type, string name, int? year, CancellationToken cancellationToken)
    {
        var update = Builders<BsonDocument>.Update;
        var changes = new List<UpdateDefinition<BsonDocument>>
        {
            update.Set(TypeField, type),
            update.Set(NameField, name),
            year is null ? update.Unset(YearField) : update.Set(YearField, year.Value),
        };
        var saved = await Items.FindOneAndUpdateAsync(
            _filter.And(Own(ownerId, id), _filter.Eq(TypeField, expectedType)),
            update.Combine(changes),
            new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After, MaxTime = mongoOptions.Value.QueryMaxTime },
            cancellationToken);
        return ToItemOrNull(saved);
    }

    public async Task<bool> DeleteAsync(string ownerId, ObjectId id, CancellationToken cancellationToken) =>
        (await Items.DeleteOneAsync(Own(ownerId, id), cancellationToken)).DeletedCount == 1;

    private static FilterDefinition<BsonDocument> Own(string ownerId, ObjectId id) =>
        _filter.And(_filter.Eq("_id", id), _filter.Eq(OwnerField, ownerId));

    private static BsonDocument ToDocument(HomeAndCarItem item)
    {
        var document = new BsonDocument
        {
            ["_id"] = item.Id,
            [OwnerField] = item.OwnerId,
            [TypeField] = item.Type,
            [NameField] = item.Name,
        };
        if (item.Year is { } year)
        {
            document[YearField] = year;
        }

        document[CreatedAtField] = new BsonDateTime(item.CreatedAt);
        return document;
    }

    private static HomeAndCarItem? ToItemOrNull(BsonDocument? document) => document is null ? null : ToItem(document);

    private static HomeAndCarItem ToItem(BsonDocument document) => new(
        document["_id"].AsObjectId,
        document[OwnerField].AsString,
        document[TypeField].AsString,
        document[NameField].AsString,
        document.TryGetValue(YearField, out var year) && year.IsNumeric ? year.ToInt32() : null,
        document[CreatedAtField].ToUniversalTime());
}
