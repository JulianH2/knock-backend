using Knock.Identity.Api.Hosting;
using Knock.Identity.Api.Modules.Usuarios.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Identity.Api.Modules.Usuarios.Data;

/// <summary>Lo que el perfil necesita de la colección <c>usuarios</c>; existe para probar el servicio con dobles sin MongoDB.</summary>
internal interface IUsuariosStore
{
    Task<UserProfile?> FindProfileAsync(string sub, CancellationToken cancellationToken);

    /// <summary>
    /// Guarda los cuatro campos en una sola operación condicionada a que la cuenta no esté en eliminación programada: marca el perfil completo, sube
    /// <c>version</c> y fija <c>actualizadoEn</c>. Devuelve el perfil nuevo.
    /// </summary>
    Task<(ProfileUpdateOutcome Outcome, UserProfile? Profile)> UpdateProfileAsync(string sub, ProfileUpdate update, DateTime now, CancellationToken cancellationToken);
}

/// <summary>
/// Lectura y escritura del perfil sobre <c>usuarios</c>. Los filtros son tipados del driver y la identidad es siempre el <c>sub</c> del token
/// (spec 017, R-09). Nunca proyecta el celular cifrado ni su hash.
/// </summary>
internal sealed class UsuariosStore(
    [FromKeyedServices(DataExtensions.DatabaseKey)] IMongoDatabase database,
    IOptions<KnockMongoOptions> mongoOptions) : IUsuariosStore
{
    public const string UsersCollection = "usuarios";
    public const string SubField = "sub";
    public const string StateField = "estado";
    public const string IsTechnicianField = "esTecnico";
    public const string ProfileCompleteField = "perfilCompleto";
    public const string CreatedAtField = "creadoEn";
    public const string VersionField = "version";
    public const string NameField = "nombre";
    public const string PhotoUrlField = "fotoUrl";
    public const string ColoniaField = "colonia";
    public const string PostalCodeField = "codigoPostal";
    public const string UpdatedAtField = "actualizadoEn";

    private static readonly FilterDefinitionBuilder<BsonDocument> _filter = Builders<BsonDocument>.Filter;

    private static readonly ProjectionDefinition<BsonDocument> _profileProjection = Builders<BsonDocument>.Projection
        .Include(SubField)
        .Include(NameField)
        .Include(PhotoUrlField)
        .Include(ColoniaField)
        .Include(PostalCodeField)
        .Include(IsTechnicianField)
        .Include(StateField)
        .Include(ProfileCompleteField)
        .Include(CreatedAtField)
        .Include(VersionField);

    private readonly FindOptions _findOptions = new() { MaxTime = mongoOptions.Value.QueryMaxTime };

    private IMongoCollection<BsonDocument> Users { get; } = database.GetCollection<BsonDocument>(UsersCollection);

    internal Task ValidateDemoAsync(CancellationToken cancellationToken) =>
        DemoSeedGuard.ValidateAsync(mongoOptions.Value.ConnectionString, Users, cancellationToken);

    internal async Task<string?> DemoConflictAsync(BsonDocument desired, CancellationToken cancellationToken)
    {
        var sameIdNonDemo = _filter.And(_filter.Eq("_id", desired["_id"]), _filter.Ne("demo", true));
        if (await Users.Find(sameIdNonDemo, _findOptions).Limit(1).AnyAsync(cancellationToken))
        {
            return "_id";
        }

        foreach (var field in new[] { "sub", "celularHash" })
        {
            var filter = _filter.And(_filter.Ne("_id", desired["_id"]), _filter.Eq(field, desired[field]));
            if (await Users.Find(filter, _findOptions).Limit(1).AnyAsync(cancellationToken))
            {
                return field;
            }
        }

        return null;
    }

    internal async Task<bool> UpsertDemoAsync(BsonDocument desired, CancellationToken cancellationToken)
    {
        var id = desired["_id"];
        var existing = await Users.Find(_filter.Eq("_id", id), _findOptions).FirstOrDefaultAsync(cancellationToken);
        if (existing is null)
        {
            try
            {
                await Users.InsertOneAsync(desired, cancellationToken: cancellationToken);
                return false;
            }
            catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                existing = await Users.Find(_filter.Eq("_id", id), _findOptions).FirstOrDefaultAsync(cancellationToken);
                if (existing is null)
                {
                    throw new InvalidOperationException("Demo seed conflict on a unique index.");
                }
            }
        }

        var compared = desired.DeepClone().AsBsonDocument;
        var previous = existing.DeepClone().AsBsonDocument;
        foreach (var field in new[] { "celularCifrado", "celularCifradoVersion", "version" })
        {
            compared.Remove(field);
            previous.Remove(field);
        }

        if (compared.Equals(previous))
        {
            return false;
        }

        desired["version"] = existing["version"].AsInt32 + 1;
        var result = await Users.ReplaceOneAsync(
            _filter.And(_filter.Eq("_id", id), _filter.Eq("version", existing["version"])), desired,
            cancellationToken: cancellationToken);
        if (result.MatchedCount != 1)
        {
            throw new InvalidOperationException("Demo account changed concurrently.");
        }

        return true;
    }

    public async Task<UserProfile?> FindProfileAsync(string sub, CancellationToken cancellationToken) =>
        ToProfile(await Users.Find(_filter.Eq(SubField, sub), _findOptions).Project(_profileProjection).Limit(1)
            .FirstOrDefaultAsync(cancellationToken));

    public async Task<(ProfileUpdateOutcome Outcome, UserProfile? Profile)> UpdateProfileAsync(
        string sub, ProfileUpdate update, DateTime now, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(update);
        var set = Builders<BsonDocument>.Update;
        var changes = new List<UpdateDefinition<BsonDocument>>
        {
            set.Set(NameField, update.Name),
            Optional(set, PhotoUrlField, update.PhotoUrl),
            Optional(set, ColoniaField, update.Colonia),
            Optional(set, PostalCodeField, update.PostalCode),
            set.Set(ProfileCompleteField, true),
            set.Set(UpdatedAtField, new BsonDateTime(now)),
            set.Inc(VersionField, 1),
        };
        var open = _filter.And(
            _filter.Eq(SubField, sub),
            _filter.Ne(StateField, ProfileStates.DeletionScheduled));
        var saved = await Users.FindOneAndUpdateAsync(
            open,
            set.Combine(changes),
            new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After, Projection = _profileProjection, MaxTime = mongoOptions.Value.QueryMaxTime },
            cancellationToken);
        if (saved is not null)
        {
            return (ProfileUpdateOutcome.Updated, ToProfile(saved));
        }

        // No se guardó: o la cuenta no existe o está en eliminación programada (spec 017, R-12).
        var exists = await Users.Find(_filter.Eq(SubField, sub), _findOptions).Project(Builders<BsonDocument>.Projection.Include(StateField))
            .Limit(1).FirstOrDefaultAsync(cancellationToken);
        return (exists is null ? ProfileUpdateOutcome.NotFound : ProfileUpdateOutcome.DeletionScheduled, null);
    }

    /// <summary>Un opcional vacío se borra del documento (spec 017, R-04: omitirlo o mandarlo vacío lo borra).</summary>
    private static UpdateDefinition<BsonDocument> Optional(UpdateDefinitionBuilder<BsonDocument> set, string field, string? value) =>
        string.IsNullOrEmpty(value) ? set.Unset(field) : set.Set(field, value);

    private static UserProfile? ToProfile(BsonDocument? document) =>
        document is null
            ? null
            : new UserProfile(
                document[SubField].AsString,
                document.GetValue(NameField, string.Empty).AsString,
                document.GetValue(PhotoUrlField, BsonNull.Value).AsNullableString(),
                document.GetValue(ColoniaField, BsonNull.Value).AsNullableString(),
                document.GetValue(PostalCodeField, BsonNull.Value).AsNullableString(),
                document[IsTechnicianField].AsBoolean,
                document[StateField].AsString,
                document[ProfileCompleteField].AsBoolean,
                document[CreatedAtField].ToUniversalTime(),
                document[VersionField].ToInt32());
}

internal static class BsonValueExtensions
{
    public static string? AsNullableString(this BsonValue value) => value.IsBsonNull ? null : value.AsString;
}
