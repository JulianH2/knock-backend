using Knock.Api.Modules.Identity.Domain;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Api.Modules.Identity.Data;

// Perfil (HU-002): parte aparte de la clase para no mezclar sus cambios con el canje y la sesión.
internal sealed partial class IdentityStore : IProfileStore
{
    public const string NameField = "nombre";

    private static readonly ProjectionDefinition<BsonDocument> _profileProjection = Builders<BsonDocument>.Projection
        .Include(SubField)
        .Include(StateField)
        .Include(NameField)
        .Include(ProfileCompleteField);

    public async Task<UserProfile?> FindProfileAsync(string sub, CancellationToken cancellationToken) =>
        ToProfile(await Users.Find(_filter.Eq(SubField, sub), _findOptions).Project(_profileProjection).Limit(1).FirstOrDefaultAsync(cancellationToken));

    /// <summary>
    /// El filtro es solo por <c>sub</c> (que sale del JWT) y exige <c>estado: "activo"</c>; ningún dato del cliente forma parte de él. La escritura
    /// solo ocurre si el contenido cambia, así que repetir el mismo <c>PUT</c> no sube <c>version</c>.
    /// </summary>
    public async Task<UserProfile?> SaveNameAsync(string sub, string name, CancellationToken cancellationToken)
    {
        var active = _filter.And(_filter.Eq(SubField, sub), _filter.Eq(StateField, UserState.Active));
        var changes = _filter.And(active, _filter.Or(_filter.Ne(NameField, name), _filter.Ne(ProfileCompleteField, true)));
        var update = Builders<BsonDocument>.Update.Set(NameField, name).Set(ProfileCompleteField, true).Inc(VersionField, 1);
        var saved = await Users.FindOneAndUpdateAsync(
            changes,
            update,
            new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After, Projection = _profileProjection },
            cancellationToken);
        return saved is not null ? ToProfile(saved) : await FindProfileAsync(sub, cancellationToken);
    }

    private static UserProfile? ToProfile(BsonDocument? document)
    {
        if (document is null)
        {
            return null;
        }

        var name = document.TryGetValue(NameField, out var stored) && stored.IsString ? stored.AsString : null;
        return new UserProfile(document[SubField].AsString, document[StateField].AsString, name, document[ProfileCompleteField].AsBoolean);
    }
}
