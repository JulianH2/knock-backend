using Knock.Market.Api.Hosting;
using Knock.Market.Api.Modules.Trust.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Trust.Data;

/// <summary>Un técnico visible tal como lo necesita la lista de confianza: identificador y datos públicos (spec 042, R-12).</summary>
internal sealed record TrustTechnicianView(ObjectId Id, string Name, string? PhotoUrl, string VerificationLevel);

/// <summary>Un técnico guardado: el registro de la persona, la fecha en que lo guardó y el identificador del registro (base del cursor).</summary>
internal sealed record TrustSaved(ObjectId TechnicianId, DateTime SavedAtUtc, ObjectId RecordId);

/// <summary>El resultado de guardar: cuándo se guardó y si fue la primera vez (201) o una repetición (200).</summary>
internal sealed record TrustSaveResult(DateTime SavedAtUtc, bool Created);

/// <summary>
/// Colección <c>tecnicos_confianza</c> (spec 042, §6) y lectura de <c>tecnicos</c> para saber quién es visible. Es el único acceso a MongoDB del módulo. Toda lectura y escritura
/// lleva el <c>clienteId</c> del <c>sub</c> del token, así una lista ajena no existe para quien la pide (R-12). El índice único (clienteId, tecnicoId) impide guardar dos veces.
/// </summary>
internal sealed class TrustStore(
    [FromKeyedServices(TrustModuleExtensions.ModuleKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> mongoOptions)
{
    public const string TrustedCollection = "tecnicos_confianza";
    public const string TechniciansCollection = "tecnicos";
    private const string _clientField = "clienteId";
    private const string _technicianField = "tecnicoId";
    private const string _savedField = "guardadoEn";
    private const string _idField = "_id";
    private const int _duplicateKey = 11000;
    private const int _maxScans = 10;

    private readonly FindOptions _findOptions = new() { MaxTime = mongoOptions.Value.QueryMaxTime };

    private IMongoCollection<BsonDocument> Trusted => database.GetCollection<BsonDocument>(TrustedCollection);

    private IMongoCollection<BsonDocument> Technicians => database.GetCollection<BsonDocument>(TechniciansCollection);

    /// <summary>Índice único de la spec 042 (§6). Idempotente; lo usan las pruebas y el script de <c>db/</c> hace lo mismo.</summary>
    public Task EnsureIndexesAsync(CancellationToken cancellationToken) =>
        Trusted.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending(_clientField).Ascending(_technicianField),
                new CreateIndexOptions { Unique = true, Name = "clienteId_1_tecnicoId_1" }),
            cancellationToken: cancellationToken);

    /// <summary>El técnico si existe y es visible (activo y no oculto); si no, <see langword="null"/>.</summary>
    public async Task<TrustTechnicianView?> FindVisibleTechnicianAsync(ObjectId id, CancellationToken cancellationToken)
    {
        var views = await FindVisibleAsync([id], cancellationToken);
        return views.GetValueOrDefault(id);
    }

    /// <summary>Guarda al técnico para la persona: lo crea o, si ya estaba, lo deja como está. Una carrera entre dos peticiones iguales la decide el índice único.</summary>
    public async Task<TrustSaveResult> SaveAsync(string clientId, ObjectId technicianId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var filter = Builders<BsonDocument>.Filter.Eq(_clientField, clientId) & Builders<BsonDocument>.Filter.Eq(_technicianField, technicianId);
        try
        {
            var result = await Trusted.UpdateOneAsync(
                filter,
                Builders<BsonDocument>.Update.SetOnInsert(_savedField, new BsonDateTime(nowUtc)),
                new UpdateOptions { IsUpsert = true },
                cancellationToken);
            if (result.UpsertedId is not null)
            {
                return new TrustSaveResult(nowUtc, Created: true);
            }
        }
        catch (Exception exception) when (IsDuplicateKey(exception))
        {
            // La otra petición ya lo guardó: esta es una repetición.
        }

        var existing = await Trusted.Find(filter, _findOptions).Project(Builders<BsonDocument>.Projection.Include(_savedField)).FirstAsync(cancellationToken);
        return new TrustSaveResult(existing[_savedField].ToUniversalTime(), Created: false);
    }

    /// <summary>Quita al técnico de la lista de la persona; falso si no estaba.</summary>
    public async Task<bool> RemoveAsync(string clientId, ObjectId technicianId, CancellationToken cancellationToken)
    {
        var filter = Builders<BsonDocument>.Filter.Eq(_clientField, clientId) & Builders<BsonDocument>.Filter.Eq(_technicianField, technicianId);
        return (await Trusted.DeleteOneAsync(filter, cancellationToken)).DeletedCount > 0;
    }

    /// <summary>
    /// Una página de la lista, del más reciente al más antiguo. Un técnico oculto o inactivo no aparece pero su registro se conserva (R-11): se leen los guardados en tandas y se dejan
    /// solo los visibles hasta completar la página; el cursor sale del último guardado leído, así nunca se repite ni se salta un elemento.
    /// </summary>
    public async Task<(IReadOnlyList<(TrustSaved Saved, TrustTechnicianView Technician)> Items, string? NextCursor)> ListAsync(
        string clientId, (DateTime SavedAtUtc, ObjectId Id)? after, int limit, CancellationToken cancellationToken)
    {
        var items = new List<(TrustSaved Saved, TrustTechnicianView Technician)>();
        var position = after;
        for (var scan = 0; scan < _maxScans && items.Count <= limit; scan++)
        {
            var batch = await ReadBatchAsync(clientId, position, limit + 1, cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            var visible = await FindVisibleAsync([.. batch.Select(row => row.TechnicianId)], cancellationToken);
            items.AddRange(batch.Where(row => visible.ContainsKey(row.TechnicianId)).Select(row => (row, visible[row.TechnicianId])));
            position = (batch[^1].SavedAtUtc, batch[^1].RecordId);
            if (batch.Count < limit + 1)
            {
                break;
            }
        }

        if (items.Count <= limit)
        {
            return (items, null);
        }

        var page = items.Take(limit).ToList();
        var last = page[^1].Saved;
        return (page, TrustCursor.Encode(last.SavedAtUtc, last.RecordId));
    }

    private static bool IsDuplicateKey(Exception exception) =>
        exception is MongoWriteException { WriteError.Category: ServerErrorCategory.DuplicateKey }
        || exception is MongoCommandException { Code: _duplicateKey };

    private async Task<List<TrustSaved>> ReadBatchAsync(
        string clientId, (DateTime SavedAtUtc, ObjectId Id)? after, int size, CancellationToken cancellationToken)
    {
        var filters = Builders<BsonDocument>.Filter;
        var filter = filters.Eq(_clientField, clientId);
        if (after is { } cursor)
        {
            filter &= filters.Lt(_savedField, new BsonDateTime(cursor.SavedAtUtc))
                | (filters.Eq(_savedField, new BsonDateTime(cursor.SavedAtUtc)) & filters.Lt(_idField, cursor.Id));
        }

        var documents = await Trusted.Find(filter, _findOptions)
            .Sort(Builders<BsonDocument>.Sort.Descending(_savedField).Descending(_idField))
            .Limit(size)
            .ToListAsync(cancellationToken);
        return [.. documents.Select(d => new TrustSaved(d[_technicianField].AsObjectId, d[_savedField].ToUniversalTime(), d[_idField].AsObjectId))];
    }

    private async Task<Dictionary<ObjectId, TrustTechnicianView>> FindVisibleAsync(IReadOnlyCollection<ObjectId> ids, CancellationToken cancellationToken)
    {
        var filters = Builders<BsonDocument>.Filter;
        var documents = await Technicians
            .Find(filters.In(_idField, ids) & filters.Eq("oculto", false) & filters.Eq("estado", "activo"), _findOptions)
            .Project(Builders<BsonDocument>.Projection.Include("nombre").Include("fotoUrl").Include("nivelVerificacion"))
            .ToListAsync(cancellationToken);
        return documents
            .Where(d => d.TryGetValue("nombre", out var name) && name.IsString)
            .ToDictionary(
                d => d[_idField].AsObjectId,
                d => new TrustTechnicianView(
                    d[_idField].AsObjectId,
                    d["nombre"].AsString,
                    d.TryGetValue("fotoUrl", out var photo) && photo.IsString ? photo.AsString : null,
                    d.TryGetValue("nivelVerificacion", out var level) && level.IsString ? level.AsString : "ninguno"));
    }
}
