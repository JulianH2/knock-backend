using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Addresses.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Identity.Api.Modules.Addresses.Data;

/// <summary>Lo que las direcciones necesitan de MongoDB; existe para probar el servicio con dobles sin base de datos.</summary>
internal interface IAddressesStore
{
    /// <summary>El <c>_id</c> de la cuenta del <c>sub</c> del token, o <see langword="null"/> si no existe.</summary>
    Task<ObjectId?> FindUserIdAsync(string sub, CancellationToken cancellationToken);

    /// <summary>Las direcciones de la persona, la más reciente primero.</summary>
    Task<IReadOnlyList<StoredAddress>> ListAsync(ObjectId userId, CancellationToken cancellationToken);

    /// <summary>
    /// Lee las direcciones de la persona, pide a <paramref name="decide"/> los cambios y los aplica en una sola transacción, serializada por persona. Devuelve el motivo del
    /// rechazo (de <see cref="AddressRejections"/>) o <see langword="null"/> si se aplicó. <paramref name="decide"/> puede ejecutarse más de una vez (reintento por
    /// conflicto): debe ser una función pura de las direcciones que recibe.
    /// </summary>
    Task<string?> ApplyAsync(ObjectId userId, Func<IReadOnlyList<StoredAddress>, AddressChanges> decide, CancellationToken cancellationToken);
}

/// <summary>
/// Colección <c>direcciones</c> de la base del servicio (spec 040, §6). Es el único punto del módulo con <c>IMongoCollection</c> (spec 002): los filtros son tipados del
/// driver y toda lectura y escritura lleva el <c>usuarioId</c> de la cuenta del token, así una dirección ajena no existe para quien la pide (R-14 y R-15). Nada se cachea
/// (R-13). Lee <c>usuarios</c> solo para convertir el <c>sub</c> del token en el <c>_id</c> de la cuenta.
/// </summary>
internal sealed class AddressesStore(
    [FromKeyedServices(DataExtensions.DatabaseKey)] IMongoDatabase database,
    IMongoClient client,
    IOptions<KnockMongoOptions> mongoOptions) : IAddressesStore
{
    public const string AddressesCollection = "direcciones";
    public const string UsersCollection = "usuarios";
    public const string UserIdField = "usuarioId";
    public const string StreetField = "calleNumero";
    public const string ColoniaField = "colonia";
    public const string ReferencesField = "referencias";
    public const string PrincipalField = "principal";
    public const string CreatedAtField = "creadoEn";
    public const string AddressesVersionField = "versionDirecciones";
    private const string _idField = "_id";
    private const string _subField = "sub";

    /// <summary>Código de «write conflict» de MongoDB y documentdb-local para escrituras concurrentes en transacciones.</summary>
    private const int _writeConflictCode = 112;

    private const int _maxWriteConflictAttempts = 6;

    private static readonly FilterDefinitionBuilder<BsonDocument> _filter = Builders<BsonDocument>.Filter;

    private readonly FindOptions _findOptions = new() { MaxTime = mongoOptions.Value.QueryMaxTime };

    private IMongoCollection<BsonDocument> Addresses { get; } = database.GetCollection<BsonDocument>(AddressesCollection);

    private IMongoCollection<BsonDocument> Users { get; } = database.GetCollection<BsonDocument>(UsersCollection);

    /// <summary>Índice de la spec 040 (§6). Idempotente; lo invocan las pruebas y el script <c>db/040-direcciones-del-cliente.js</c> hace lo mismo.</summary>
    public Task EnsureIndexesAsync(CancellationToken cancellationToken) =>
        Addresses.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending(UserIdField)),
            cancellationToken: cancellationToken);

    public async Task<ObjectId?> FindUserIdAsync(string sub, CancellationToken cancellationToken)
    {
        var user = await Users.Find(_filter.Eq(_subField, sub), _findOptions)
            .Project(Builders<BsonDocument>.Projection.Include(_idField))
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken);
        return user?[_idField].AsObjectId;
    }

    public async Task<IReadOnlyList<StoredAddress>> ListAsync(ObjectId userId, CancellationToken cancellationToken)
    {
        var documents = await Addresses.Find(_filter.Eq(UserIdField, userId), _findOptions)
            .Sort(Builders<BsonDocument>.Sort.Descending(CreatedAtField).Descending(_idField))
            .ToListAsync(cancellationToken);
        return [.. documents.Select(ToStored)];
    }

    public async Task<string?> ApplyAsync(
        ObjectId userId, Func<IReadOnlyList<StoredAddress>, AddressChanges> decide, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(decide);
        var attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                using var session = await client.StartSessionAsync(cancellationToken: cancellationToken);
                return await session.WithTransactionAsync(
                    (transaction, token) => ApplyInTransactionAsync(transaction, userId, decide, token),
                    cancellationToken: cancellationToken);
            }
            catch (MongoCommandException exception) when (exception.Code == _writeConflictCode && attempt < _maxWriteConflictAttempts)
            {
                // documentdb-local responde 112 sin la etiqueta de error transitorio, así que el driver no reintenta solo.
                await Task.Delay(TimeSpan.FromMilliseconds(50 * attempt), cancellationToken);
            }
        }
    }

    private async Task<string?> ApplyInTransactionAsync(
        IClientSessionHandle transaction, ObjectId userId, Func<IReadOnlyList<StoredAddress>, AddressChanges> decide, CancellationToken token)
    {
        // Dos peticiones de la misma persona tocan documentos distintos y no chocan solas: ambas verían la misma lista y podrían pasar del tope o guardar la misma dirección
        // dos veces. Tocar el documento de la cuenta fuerza el conflicto de escritura (112) y las serializa; el reintento ya ve la otra.
        var touched = await Users.UpdateOneAsync(
            transaction,
            _filter.Eq(_idField, userId),
            Builders<BsonDocument>.Update.Inc(AddressesVersionField, 1),
            cancellationToken: token);
        if (touched.MatchedCount == 0)
        {
            await transaction.AbortTransactionAsync(token);
            return AddressRejections.NoAccount;
        }

        var documents = await Addresses.Find(transaction, _filter.Eq(UserIdField, userId)).ToListAsync(token);
        var changes = decide([.. documents.Select(ToStored)]);
        if (changes.Rejection is not null)
        {
            await transaction.AbortTransactionAsync(token);
            return changes.Rejection;
        }

        foreach (var address in changes.Upserts)
        {
            await Addresses.ReplaceOneAsync(
                transaction,
                _filter.And(_filter.Eq(_idField, address.Id), _filter.Eq(UserIdField, userId)),
                ToDocument(address),
                new ReplaceOptions { IsUpsert = true },
                token);
        }

        foreach (var id in changes.Deletes)
        {
            await Addresses.DeleteOneAsync(transaction, _filter.And(_filter.Eq(_idField, id), _filter.Eq(UserIdField, userId)), cancellationToken: token);
        }

        return null;
    }

    internal static BsonDocument ToDocument(StoredAddress address)
    {
        var document = new BsonDocument
        {
            [_idField] = address.Id,
            [UserIdField] = address.UserId,
            [StreetField] = address.StreetCipher,
            [ColoniaField] = address.Colonia,
            [PrincipalField] = address.Principal,
            [CreatedAtField] = new BsonDateTime(address.CreatedAt),
        };
        if (address.ReferencesCipher is not null)
        {
            document[ReferencesField] = address.ReferencesCipher;
        }

        return document;
    }

    internal static StoredAddress ToStored(BsonDocument document) => new(
        document[_idField].AsObjectId,
        document[UserIdField].AsObjectId,
        document[StreetField].AsString,
        document[ColoniaField].AsString,
        document.TryGetValue(ReferencesField, out var references) && !references.IsBsonNull ? references.AsString : null,
        document[PrincipalField].AsBoolean,
        document[CreatedAtField].ToUniversalTime());
}
