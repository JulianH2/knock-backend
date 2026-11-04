using System.Security.Cryptography;
using Knock.Api.Hosting;
using Knock.Api.Modules.Identity.Data;
using Knock.Api.Modules.Identity.Domain;
using Knock.Api.Modules.Identity.Services;
using Knock.IntegrationTest.Support;
using Knock.UnitTest.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.DatabaseTest.Support;

/// <summary>
/// Base de Identity sobre documentdb-local (contenedor compartido), con un nombre propio por instancia para que las pruebas no se pisen.
/// Arma <see cref="IdentityStore"/> a mano con el cliente de la instancia. Solo datos y llaves ficticios.
/// </summary>
internal sealed class IdentityDatabase : IAsyncDisposable
{
    public const string Phone = "+525512345678";
    public const string OtherPhone = "+525598765432";

    private readonly MongoClient _client;

    private IdentityDatabase(string connectionString)
    {
        _client = new MongoClient(connectionString);
        DatabaseName = "identity_test_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        Database = _client.GetDatabase(DatabaseName);
        Store = new IdentityStore(Database, _client, Options.Create(new KnockMongoOptions()));
        Protector = new PhoneProtector(PhoneProtectionTestKeys.Keys());
    }

    public string DatabaseName { get; }

    public IMongoDatabase Database { get; }

    public IdentityStore Store { get; }

    public PhoneProtector Protector { get; }

    public static async Task<IdentityDatabase> CreateAsync(bool withIndexes = true)
    {
        var container = await DocumentDbFixture.SharedAsync();
        var database = new IdentityDatabase(container.ConnectionString);
        if (withIndexes)
        {
            await database.Store.EnsureIndexesAsync(CancellationToken.None);
        }

        return database;
    }

    /// <summary>Alta con celular cifrado y hash reales, <c>sub</c> y refresh token aleatorios; devuelve también el refresh token en claro.</summary>
    public (NewAccount Account, string RefreshToken) BuildAccount(string phone = Phone, string? sub = null, byte[]? fingerprint = null)
    {
        var id = ObjectId.GenerateNewId().ToString();
        var refreshToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenFingerprint.TokenBytes));
        var createdAt = DateTime.UtcNow;
        var protectedPhone = Protector.Protect(phone, id);
        var hash = Protector.Hash(phone);
        var refresh = new RefreshTokenRecord(
            id,
            ObjectId.GenerateNewId().ToString(),
            fingerprint ?? RefreshTokenFingerprint.Compute(refreshToken),
            Guid.NewGuid().ToString("D"),
            createdAt);
        var account = new NewAccount(
            id,
            sub ?? Guid.NewGuid().ToString("D"),
            protectedPhone.Ciphertext,
            protectedPhone.KeyVersion,
            hash.Value,
            hash.KeyVersion,
            refresh.DeviceId,
            "correlation-" + id,
            "trace-" + id,
            createdAt,
            refresh);
        return (account, refreshToken);
    }

    public async Task<IReadOnlyDictionary<string, long>> CountsAsync() => new Dictionary<string, long>
    {
        [IdentityStore.UsersCollection] = await Database.GetCollection<BsonDocument>(IdentityStore.UsersCollection).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty),
        [IdentityStore.ConsentsCollection] = await Database.GetCollection<BsonDocument>(IdentityStore.ConsentsCollection).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty),
        [IdentityStore.AuditCollection] = await Database.GetCollection<BsonDocument>(IdentityStore.AuditCollection).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty),
        [IdentityStore.RefreshTokensCollection] = await Database.GetCollection<BsonDocument>(IdentityStore.RefreshTokensCollection).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty),
    };

    public async ValueTask DisposeAsync()
    {
        await _client.DropDatabaseAsync(DatabaseName);
        _client.Dispose();
    }
}
