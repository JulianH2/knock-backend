extern alias IdentityApi;

using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IdentityApi::Knock.Identity.Api.Modules.Safety.Data;
using Knock.Contracts.Moderation;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.IntegrationTest.Identity;

/// <summary>Moderación de mentira para las pruebas de reportes: contesta lo que diga <see cref="Outcome"/> y cuenta cuántas veces la llamaron.</summary>
internal sealed class FakeModerator : IContentModerator
{
    public ModerationOutcome Outcome { get; set; } = ModerationOutcome.Allowed;

    public int Calls { get; private set; }

    public Task<ModerationResult> ReviewTextAsync(string text, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(new ModerationResult(Outcome));
    }

    public Task<ModerationResult> ReviewImageAsync(ReadOnlyMemory<byte> image, CancellationToken cancellationToken) =>
        Task.FromResult(new ModerationResult(Outcome));
}

/// <summary>
/// Un host de Identidad para las pruebas de la spec 043 (bloqueos y reportes): MongoDB y Redis reales, índices listos, una persona que llama y cuentas sembradas. Sin emulador de Firebase:
/// las cuentas se siembran y el token se arma aquí. Solo datos ficticios.
/// </summary>
internal sealed class SafetyHost : IAsyncDisposable
{
    private readonly IdentityApiFactory _factory;
    private readonly HttpClient _client;

    private SafetyHost(IdentityApiFactory factory, FakeModerator moderator)
    {
        _factory = factory;
        Moderator = moderator;
        _client = factory.CreateClient();
    }

    public string Caller { get; } = Guid.NewGuid().ToString("D");

    public FakeModerator Moderator { get; }

    public IMongoCollection<BsonDocument> Blocks => _factory.Database.GetCollection<BsonDocument>("bloqueos");

    public IMongoCollection<BsonDocument> Reports => _factory.Database.GetCollection<BsonDocument>("reportes");

    public static async Task<SafetyHost> StartAsync()
    {
        var moderator = new FakeModerator();
        var factory = await IdentityApiFactory.CreateAsync(configureServices: services =>
        {
            services.RemoveAll<IContentModerator>();
            services.AddSingleton<IContentModerator>(moderator);
        });
        await factory.Services.GetRequiredService<BloqueosStore>().EnsureIndexesAsync(CancellationToken.None);
        await factory.Services.GetRequiredService<ReportesStore>().EnsureIndexesAsync(CancellationToken.None);
        var host = new SafetyHost(factory, moderator);
        await host.SeedUserAsync("Demo Quien Llama", host.Caller);
        return host;
    }

    public static async Task<JsonElement> ReadAsync(HttpResponseMessage response) => JsonElement.Parse(await response.Content.ReadAsStringAsync());

    public async Task<string> SeedUserAsync(string name, string? sub = null)
    {
        var id = sub ?? Guid.NewGuid().ToString("D");
        await _factory.Users.InsertOneAsync(new BsonDocument
        {
            { "sub", id }, { "nombre", name }, { "esTecnico", false }, { "estado", "activo" }, { "perfilCompleto", true },
            { "creadoEn", new BsonDateTime(DateTime.UtcNow) }, { "version", 1 },
            // El índice único del celular no admite dos cuentas sin valor: cada cuenta de prueba lleva el suyo, ficticio.
            { "celularHash", new BsonBinaryData(RandomNumberGenerator.GetBytes(32)) },
        });
        return id;
    }

    public Task<long> CountAsync() => Blocks.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);

    public Task<long> CountReportsAsync() => Reports.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);

    /// <summary>Deja una decisión tomada sobre el reporte, como lo haría el panel de la spec 053 (que todavía no existe).</summary>
    public Task SeedDecisionAsync(string reportId) =>
        Reports.UpdateOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(reportId)),
            Builders<BsonDocument>.Update.Set("estado", "decidido").Set("decision", new BsonDocument { { "gravedad", "media" }, { "decididaEn", new BsonDateTime(DateTime.UtcNow) } }));

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? sub, string? rawJson = null)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));
        if (sub is not null)
        {
            var token = GatewayTestSupport.IssueToken(sub: sub, key: Convert.FromBase64String(IdentityApiFactory.SigningKeyBase64));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (rawJson is not null)
        {
            request.Content = new StringContent(rawJson, Encoding.UTF8, "application/json");
        }

        return await _client.SendAsync(request);
    }

    public Task<HttpResponseMessage> PostJsonAsync(string path, string? sub, object body) => SendAsync(HttpMethod.Post, path, sub, JsonSerializer.Serialize(body));

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }
}
