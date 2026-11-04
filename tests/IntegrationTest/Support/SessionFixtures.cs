using Knock.Api.Modules.Identity.Data;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.IntegrationTest.Support;

/// <summary>Sesión abierta por un canje real contra el emulador de Firebase: lo que una app tendría guardado.</summary>
internal sealed record OpenedSession(string AccessToken, string RefreshToken, string DeviceId);

/// <summary>Ayudantes de las pruebas de renovación y cierre de sesión (HU-003 y HU-004). Solo datos ficticios.</summary>
internal static class SessionFixtures
{
    /// <summary>Abre una sesión con un celular de prueba del emulador y un <c>deviceId</c> nuevo (o el dado).</summary>
    public static async Task<OpenedSession> OpenAsync(HttpClient client, string phone, string? deviceId = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        var (idToken, _) = await FirebaseEmulator.SignInWithPhoneAsync(phone);
        var device = deviceId ?? Guid.NewGuid().ToString("D");
        using var response = await AuthHttp.ExchangeAsync(client, idToken, device);
        response.EnsureSuccessStatusCode();
        var (accessToken, refreshToken) = await AuthHttp.ReadSessionAsync(response);
        return new OpenedSession(accessToken, refreshToken, device);
    }

    public static IMongoCollection<BsonDocument> RefreshTokens(KnockApiFactory factory) =>
        IdentityTestDatabase.DatabaseOf(factory).GetCollection<BsonDocument>(IdentityStore.RefreshTokensCollection);

    public static IMongoCollection<BsonDocument> Users(KnockApiFactory factory) =>
        IdentityTestDatabase.DatabaseOf(factory).GetCollection<BsonDocument>(IdentityStore.UsersCollection);

    public static Task<List<BsonDocument>> AllRefreshTokensAsync(KnockApiFactory factory) =>
        RefreshTokens(factory).Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
}
