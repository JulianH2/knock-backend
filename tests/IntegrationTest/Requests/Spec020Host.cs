extern alias MarketApi;

using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Knock.Contracts.Moderation;
using Knock.IntegrationTest.Support;
using MarketApi::Knock.Market.Api.Modules.Requests.Data;
using MarketApi::Knock.Market.Api.Modules.Requests.Dtos;
using MarketApi::Knock.Market.Api.Modules.Requests.Services;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.IntegrationTest.Requests;

/// <summary>Fotos de mentira: dicen que todas están listas (o ninguna) y devuelven enlaces ficticios de 5 minutos. El módulo de archivos real llega con la spec 013, OI-2.</summary>
internal sealed class FakePhotos : IRequestPhotos
{
    public bool Ready { get; set; } = true;

    /// <summary>Dueño de cada foto (archivoId a sub). Si es <see langword="null"/>, solo cuenta <see cref="Ready"/>; si no, una foto es válida únicamente para su dueño.</summary>
    public Dictionary<string, string>? Owners { get; set; }

    /// <summary>Último <c>ownerId</c> con el que el servicio consultó las fotos: debe ser el <c>sub</c> del token.</summary>
    public string? LastOwnerId { get; private set; }

    public Task<bool> AreReadyForAsync(string ownerId, IReadOnlyList<string> photoIds, CancellationToken cancellationToken)
    {
        LastOwnerId = ownerId;
        var owned = Owners is null || photoIds.All(id => Owners.TryGetValue(id, out var owner) && string.Equals(owner, ownerId, StringComparison.Ordinal));
        return Task.FromResult(Ready && owned);
    }

    public Task<IReadOnlyList<RequestPhoto>> ReadLinksAsync(IReadOnlyList<string> photoIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RequestPhoto>>([.. photoIds.Select(id => new RequestPhoto(id, "https://files.test/" + id + "?sig=ficticia", DateTimeOffset.UtcNow.AddMinutes(5)))]);
}

/// <summary>Interruptor de la caída simulada del buzón de salida: mientras <see cref="Fail"/> sea verdadero, escribir en el buzón falla (spec 021, CA-17).</summary>
internal sealed class OutboxFailureSwitch
{
    public bool Fail { get; set; }
}

/// <summary>Buzón de salida que falla mientras el interruptor esté activo y, si no, escribe de verdad.</summary>
internal sealed class FlakyOutbox(IOutbox inner, OutboxFailureSwitch control) : IOutbox
{
    public Task AddAsync(IClientSessionHandle session, OutboxMessage message, CancellationToken cancellationToken) =>
        control.Fail ? throw new TimeoutException("Simulated database failure.") : inner.AddAsync(session, message, cancellationToken);
}

/// <summary>Moderador de mentira: permite, rechaza o no responde según se configure.</summary>
internal sealed class FakeModerator : IContentModerator
{
    public ModerationOutcome Outcome { get; set; } = ModerationOutcome.Allowed;

    public Task<ModerationResult> ReviewTextAsync(string text, CancellationToken cancellationToken) => Task.FromResult(new ModerationResult(Outcome));

    public Task<ModerationResult> ReviewImageAsync(ReadOnlyMemory<byte> image, CancellationToken cancellationToken) => Task.FromResult(new ModerationResult(Outcome));
}

/// <summary>
/// Host de Mercado para las pruebas de la spec 020 sobre MongoDB real: oficios y colonias sembrados en su propia base, fotos y moderación de mentira, llave de cifrado
/// sintética (generada al azar en cada ejecución) y sesiones por <c>sub</c>. Solo datos ficticios.
/// </summary>
internal sealed class Spec020Host : IAsyncDisposable
{
    public const string Plumbing = "plo";
    public const string Workshop = "mec";
    public const string Address = "Calle Ficticia 123, Colonia Piloto";
    public const string Description = "Se rompió el tubo debajo del fregadero y gotea";

    private readonly MarketApiFactory _factory;

    private Spec020Host(MarketApiFactory factory, FakePhotos photos, FakeModerator moderator, OutboxFailureSwitch outboxFailure)
    {
        _factory = factory;
        OutboxFailure = outboxFailure;
        Photos = photos;
        Moderator = moderator;
        Client = factory.CreateClient();
    }

    public HttpClient Client { get; }

    public FakePhotos Photos { get; }

    public FakeModerator Moderator { get; }

    public OutboxFailureSwitch OutboxFailure { get; }

    public IMongoDatabase Database => _factory.Database;

    /// <summary>Servicios del host, para sembrar valores cifrados con la misma llave que usa la API.</summary>
    public IServiceProvider Services => _factory.Services;

    public string ClientSub { get; } = Guid.NewGuid().ToString("D");

    public ObjectId ColoniaId { get; private set; }

    public static async Task<Spec020Host> StartAsync(IReadOnlyDictionary<string, string?>? overrides = null, Action<IServiceCollection>? configure = null)
    {
        var photos = new FakePhotos();
        var moderator = new FakeModerator();
        var outboxFailure = new OutboxFailureSwitch();
        var settings = new Dictionary<string, string?>
        {
            ["Security:FieldEncryption:ActiveVersion"] = "1",
            ["Security:FieldEncryption:Keys:1"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
        };
        foreach (var (key, value) in overrides ?? new Dictionary<string, string?>())
        {
            settings[key] = value;
        }

        var factory = await MarketApiFactory.CreateAsync(settings, services =>
        {
            services.AddSingleton<IRequestPhotos>(photos);
            services.AddSingleton<IContentModerator>(moderator);
            services.AddScoped<IOutbox>(sp => new FlakyOutbox(new MongoOutbox(sp.GetRequiredKeyedService<IMongoDatabase>("Requests")), outboxFailure));
            configure?.Invoke(services);
        });
        var host = new Spec020Host(factory, photos, moderator, outboxFailure);
        await host.SeedTradeAsync(Plumbing, "Plomería", "domicilio", active: true);
        await host.SeedTradeAsync(Workshop, "Mecánica", "taller", active: true);
        host.ColoniaId = await host.SeedColoniaAsync("Centro", "42800");
        return host;
    }

    public Task SeedTradeAsync(string key, string name, string modality, bool active) =>
        Database.GetCollection<BsonDocument>("oficios").InsertOneAsync(new BsonDocument
        {
            { "clave", key }, { "nombre", name }, { "nombreBusqueda", name.ToUpperInvariant() }, { "modalidad", modality }, { "activo", active }, { "version", 1 },
        });

    public async Task<ObjectId> SeedColoniaAsync(string name, string postalCode)
    {
        var id = ObjectId.GenerateNewId();
        await Database.GetCollection<BsonDocument>("colonias").InsertOneAsync(new BsonDocument
        {
            { "_id", id }, { "nombre", name }, { "nombreBusqueda", name.ToUpperInvariant() }, { "cp", postalCode }, { "municipio", "Ficticio" }, { "version", 1 },
        });
        return id;
    }

    public Task SeedTechnicianAsync(string sub, string state, bool hidden, string[] trades, ObjectId baseColonia) =>
        Database.GetCollection<BsonDocument>("tecnicos").InsertOneAsync(new BsonDocument
        {
            { "usuarioId", sub }, { "estado", state }, { "oculto", hidden }, { "oficios", new BsonArray(trades) }, { "coloniaBaseId", baseColonia.ToString() }, { "radioAtencionKm", 5 },
        });

    public static string Token(string sub) =>
        GatewayTestSupport.IssueToken(sub: sub, key: Convert.FromBase64String(MarketApiFactory.SigningKeyBase64));

    /// <summary>Día válido para la solicitud: mañana en la hora de Ciudad de México.</summary>
    public static string Tomorrow() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City")).DateTime).AddDays(1)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public Dictionary<string, object?> ValidBody(string trade = Plumbing, string? address = Address) => new()
    {
        ["oficioClave"] = trade,
        ["descripcion"] = Description,
        ["fotos"] = new[] { ObjectId.GenerateNewId().ToString() },
        ["dia"] = Tomorrow(),
        ["franja"] = "tarde",
        ["coloniaId"] = ColoniaId.ToString(),
        ["direccion"] = address,
    };

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? sub = null, object? body = null, string? rawJson = null)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));
        if (sub is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token(sub));
        }

        if (rawJson is not null || body is not null)
        {
            request.Content = new StringContent(rawJson ?? JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        }

        return await Client.SendAsync(request);
    }

    public Task<HttpResponseMessage> CreateAsync(object body, string? sub = null) => SendAsync(HttpMethod.Post, "/api/v1/solicitudes", sub ?? ClientSub, body);

    public async Task<long> CountRequestsAsync() => await Database.GetCollection<BsonDocument>("solicitudes").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);

    public static async Task<JsonElement> ReadAsync(HttpResponseMessage response) => JsonElement.Parse(await response.Content.ReadAsStringAsync());

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }
}
