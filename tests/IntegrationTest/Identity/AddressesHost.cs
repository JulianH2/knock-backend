extern alias IdentityApi;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using IdentityApi::Knock.Identity.Api.Common.Crypto;
using IdentityApi::Knock.Identity.Api.Modules.Addresses.Data;
using IdentityApi::Knock.Identity.Api.Modules.Addresses.Services;
using Knock.Contracts.Colonias;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.IntegrationTest.Identity;

/// <summary>Catálogo de colonias de mentira para las pruebas de la spec 040: conoce algunas colonias y puede simular que Mercado no responde.</summary>
internal sealed class FakeColoniaCatalog : IColoniaCatalog
{
    public HashSet<string> Known { get; } = ["Centro", "Tultengo", "Bosques del Valle"];

    public bool Down { get; set; }

    public int Calls { get; private set; }

    public Task<bool> ExistsAsync(string colonia, CancellationToken cancellationToken)
    {
        Calls++;
        return Down ? throw new HttpRequestException("synthetic") : Task.FromResult(Known.Contains(colonia));
    }
}

/// <summary>Una persona de prueba: cuenta sembrada en <c>usuarios</c> y un JWT propio firmado con la llave sintética del host. Sin Firebase.</summary>
internal sealed record Spec040Person(string Sub, ObjectId UserId, string Token);

/// <summary>
/// Host de Identidad para las pruebas de la spec 040 sobre MongoDB real: llave de cifrado de campo sintética (al azar en cada ejecución), catálogo de colonias de mentira,
/// cuentas sembradas directo en <c>usuarios</c> y JWT firmados con la llave del host. Solo datos ficticios.
/// </summary>
internal sealed class AddressesHost : IAsyncDisposable
{
    public const string Route = "/api/v1/me/direcciones";

    private AddressesHost(IdentityApiFactory factory, FakeColoniaCatalog colonias)
    {
        Factory = factory;
        Colonias = colonias;
        Client = factory.CreateClient();
    }

    public IdentityApiFactory Factory { get; }

    public HttpClient Client { get; }

    public FakeColoniaCatalog Colonias { get; }

    public IMongoCollection<BsonDocument> Addresses => Factory.Database.GetCollection<BsonDocument>(AddressesStore.AddressesCollection);

    public IMongoCollection<BsonDocument> Users => Factory.Users;

    public static async Task<AddressesHost> StartAsync(IReadOnlyDictionary<string, string?>? overrides = null)
    {
        var colonias = new FakeColoniaCatalog();
        var factory = await IdentityApiFactory.CreateAsync(overrides, services => services.AddSingleton<IColoniaCatalog>(colonias));
        await factory.Services.GetRequiredService<AddressesStore>().EnsureIndexesAsync(CancellationToken.None);
        return new AddressesHost(factory, colonias);
    }

    public async Task<Spec040Person> NewPersonAsync()
    {
        var sub = Guid.NewGuid().ToString("D");
        var userId = ObjectId.GenerateNewId();
        await Users.InsertOneAsync(new BsonDocument
        {
            ["_id"] = userId,
            ["sub"] = sub,
            ["celularHash"] = new BsonBinaryData(RandomNumberGenerator.GetBytes(32)),
            ["estado"] = "activa",
            ["esTecnico"] = false,
            ["perfilCompleto"] = false,
            ["creadoEn"] = new BsonDateTime(DateTime.UtcNow),
            ["version"] = 1,
        });
        return new Spec040Person(sub, userId, Token(sub));
    }

    public static string Token(string sub, TimeSpan? lifetime = null, string? key = null, string? sid = null)
    {
        var now = DateTime.UtcNow;
        var expired = lifetime is { } span && span < TimeSpan.Zero;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "knock-api",
            Audience = "knock-app",
            Claims = new Dictionary<string, object> { ["sub"] = sub, ["sid"] = sid ?? ObjectId.GenerateNewId().ToString() },
            IssuedAt = expired ? now.AddMinutes(-30) : now,
            NotBefore = expired ? now.AddMinutes(-30) : now,
            Expires = expired ? now.Add(lifetime!.Value) : now.Add(lifetime ?? TimeSpan.FromMinutes(15)),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Convert.FromBase64String(key ?? IdentityApiFactory.SigningKeyBase64)),
                SecurityAlgorithms.HmacSha256),
        });
    }

    /// <summary>Siembra una dirección directo en MongoDB, cifrada como lo hace el servicio (para probar la lectura sin pasar por la escritura).</summary>
    public async Task<ObjectId> SeedAsync(
        Spec040Person owner, string street, string colonia, string? references, bool principal, DateTime? createdAt = null)
    {
        var id = ObjectId.GenerateNewId();
        var cipher = Factory.Services.GetRequiredService<FieldCipher>();
        var document = new BsonDocument
        {
            ["_id"] = id,
            ["usuarioId"] = owner.UserId,
            ["calleNumero"] = cipher.Encrypt(street, AddressCrypto.StreetContext(id)),
            ["colonia"] = colonia,
            ["principal"] = principal,
            ["creadoEn"] = new BsonDateTime(createdAt ?? DateTime.UtcNow),
        };
        if (references is not null)
        {
            document["referencias"] = cipher.Encrypt(references, AddressCrypto.ReferencesContext(id));
        }

        await Addresses.InsertOneAsync(document);
        return id;
    }

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? token, object? body = null)
    {
        using var request = Build(method, path, token, body);
        return await Client.SendAsync(request);
    }

    public async Task<HttpResponseMessage> SendRawAsync(HttpMethod method, string path, string? token, string json)
    {
        using var request = Build(method, path, token, null);
        request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        return await Client.SendAsync(request);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
    }

    private static HttpRequestMessage Build(HttpMethod method, string path, string? token, object? body)
    {
        var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }
}
