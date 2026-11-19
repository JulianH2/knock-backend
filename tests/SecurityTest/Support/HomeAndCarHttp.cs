using System.Net.Http.Json;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.SecurityTest.Support;

/// <summary>Llamadas HTTP y datos de prueba de «Mi casa y mi auto» (spec 041). Datos ficticios.</summary>
internal static class HomeAndCarHttp
{
    public const string Route = "/api/v1/me/casa-y-auto";

    public static Task<HttpResponseMessage> SendAsync(SecurityHosts hosts, HttpMethod method, string path, string? token, object? body = null) =>
        SecureHttp.SendAsync(hosts.IdentityClient, method, path, body is null ? null : JsonContent.Create(body), token);

    public static Task<HttpResponseMessage> SendRawAsync(SecurityHosts hosts, HttpMethod method, string path, string? token, string json) =>
        SecureHttp.SendRawJsonAsync(hosts.IdentityClient, method, path, json, token);

    public static IMongoCollection<BsonDocument> Items(SecurityHosts hosts) =>
        hosts.Identity.Database.GetCollection<BsonDocument>("mi_casa_y_auto");

    /// <summary>Inserta un elemento directamente en la colección y devuelve su <c>_id</c>.</summary>
    public static async Task<ObjectId> SeedAsync(SecurityHosts hosts, string sub, string tipo = "auto", string nombre = "Auto de prueba", int? anio = 2020, DateTime? createdAt = null)
    {
        var id = ObjectId.GenerateNewId();
        var document = new BsonDocument { ["_id"] = id, ["usuarioId"] = sub, ["tipo"] = tipo, ["nombre"] = nombre };
        if (anio is not null)
        {
            document["anio"] = anio.Value;
        }

        document["creadoEn"] = new BsonDateTime(createdAt ?? DateTime.UtcNow);
        await Items(hosts).InsertOneAsync(document);
        return id;
    }

    public static Task<long> CountAsync(SecurityHosts hosts, string sub) =>
        Items(hosts).CountDocumentsAsync(new BsonDocument("usuarioId", sub));
}
