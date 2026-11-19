extern alias IdentityApi;

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using IdentityApi::Knock.Identity.Api.Modules.Safety.Data;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Identity;

/// <summary>Spec 043, BLO-01 a BLO-03: bloquear, desbloquear y listar bloqueos con MongoDB y Redis reales. Sin emulador de Firebase: las cuentas se siembran y el token se arma aquí. Solo datos ficticios.</summary>
public sealed class Spec043BloqueosTests
{
    private const string _list = "/api/v1/me/bloqueos";

    [Fact]
    public async Task Bloquear_Responde_201_Y_La_Persona_Aparece_En_La_Lista_CA_043_01()
    {
        await using var scenario = await Scenario.StartAsync();
        var target = await scenario.SeedUserAsync("Demo Bloqueada");

        using var created = await scenario.SendAsync(HttpMethod.Post, $"/api/v1/usuarios/{target}/bloqueo", scenario.Caller);
        using var list = await scenario.SendAsync(HttpMethod.Get, _list, scenario.Caller);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Headers.CacheControl!.NoStore.Should().BeTrue();
        var items = (await Scenario.ReadAsync(list)).GetProperty("items").EnumerateArray().ToList();
        items.Should().ContainSingle();
        items[0].GetProperty("usuarioId").GetString().Should().Be(target);
        items[0].GetProperty("nombre").GetString().Should().Be("Demo Bloqueada");
        items[0].GetProperty("bloqueadoEn").GetDateTimeOffset().Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        items[0].EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(["usuarioId", "nombre", "bloqueadoEn"], "solo identificador, nombre público y fecha (R-19)");
    }

    [Fact]
    public async Task Sin_Sesion_Las_Tres_Rutas_Responden_401_CA_043_02_CA_043_07_CA_043_11()
    {
        await using var scenario = await Scenario.StartAsync();
        var target = await scenario.SeedUserAsync("Demo Uno");

        using var post = await scenario.SendAsync(HttpMethod.Post, $"/api/v1/usuarios/{target}/bloqueo", sub: null);
        using var delete = await scenario.SendAsync(HttpMethod.Delete, $"/api/v1/usuarios/{target}/bloqueo", sub: null);
        using var list = await scenario.SendAsync(HttpMethod.Get, _list, sub: null);

        post.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        delete.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        list.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await scenario.CountAsync()).Should().Be(0, "no se ejecuta nada sin sesión");
    }

    [Theory]
    [InlineData("no-es-un-id")]
    [InlineData("507f1f77bcf86cd799439011")]
    [InlineData("00000000-0000-0000-0000-00000000000")]
    public async Task Identificador_Invalido_Responde_422_Sin_Repetir_El_Valor_Y_No_Guarda_Nada_CA_043_03_CA_043_08(string invalid)
    {
        await using var scenario = await Scenario.StartAsync();

        using var post = await scenario.SendAsync(HttpMethod.Post, $"/api/v1/usuarios/{invalid}/bloqueo", scenario.Caller);
        using var delete = await scenario.SendAsync(HttpMethod.Delete, $"/api/v1/usuarios/{invalid}/bloqueo", scenario.Caller);

        post.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        delete.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await post.Content.ReadAsStringAsync()).Should().NotContain(invalid, "no repite el valor");
        (await scenario.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Bloquearse_A_Uno_Mismo_Responde_422_Y_No_Guarda_Nada_CA_043_03()
    {
        await using var scenario = await Scenario.StartAsync();
        using var response = await scenario.SendAsync(HttpMethod.Post, $"/api/v1/usuarios/{scenario.Caller}/bloqueo", scenario.Caller);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await scenario.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Persona_Inexistente_Responde_404_Con_Su_Codigo_CA_043_04()
    {
        await using var scenario = await Scenario.StartAsync();

        using var response = await scenario.SendAsync(HttpMethod.Post, $"/api/v1/usuarios/{Guid.NewGuid():D}/bloqueo", scenario.Caller);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Scenario.ReadAsync(response)).GetProperty("code").GetString().Should().Be("user_not_found");
        (await scenario.CountAsync()).Should().Be(0, "no cambia el estado");
    }

    [Fact]
    public async Task Bloquear_Dos_Veces_Responde_409_Y_Queda_Un_Solo_Registro_CA_043_05_CA_043_37()
    {
        await using var scenario = await Scenario.StartAsync();
        var target = await scenario.SeedUserAsync("Demo Dos");

        using var first = await scenario.SendAsync(HttpMethod.Post, $"/api/v1/usuarios/{target}/bloqueo", scenario.Caller);
        using var second = await scenario.SendAsync(HttpMethod.Post, $"/api/v1/usuarios/{target}/bloqueo", scenario.Caller);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Scenario.ReadAsync(second)).GetProperty("code").GetString().Should().Be("already_blocked");
        (await scenario.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Cinco_Bloqueos_Simultaneos_Dejan_Uno_Solo_CA_043_37()
    {
        await using var scenario = await Scenario.StartAsync();
        var target = await scenario.SeedUserAsync("Demo Carrera");

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => scenario.SendAsync(HttpMethod.Post, $"/api/v1/usuarios/{target}/bloqueo", scenario.Caller)));

        var statuses = responses.Select(r => r.StatusCode).ToList();
        foreach (var response in responses)
        {
            response.Dispose();
        }

        statuses.Count(s => s == HttpStatusCode.Created).Should().Be(1);
        statuses.Count(s => s == HttpStatusCode.Conflict).Should().Be(4);
        (await scenario.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Desbloquear_Responde_204_Y_Si_No_Habia_404_Con_Su_Codigo_CA_043_06_CA_043_09()
    {
        await using var scenario = await Scenario.StartAsync();
        var target = await scenario.SeedUserAsync("Demo Tres");
        var other = await scenario.SeedUserAsync("Demo Cuatro");
        (await scenario.SendAsync(HttpMethod.Post, $"/api/v1/usuarios/{target}/bloqueo", scenario.Caller)).EnsureSuccessStatusCode();

        using var removed = await scenario.SendAsync(HttpMethod.Delete, $"/api/v1/usuarios/{target}/bloqueo", scenario.Caller);
        using var notBlocked = await scenario.SendAsync(HttpMethod.Delete, $"/api/v1/usuarios/{other}/bloqueo", scenario.Caller);
        using var again = await scenario.SendAsync(HttpMethod.Delete, $"/api/v1/usuarios/{target}/bloqueo", scenario.Caller);

        removed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        notBlocked.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Scenario.ReadAsync(notBlocked)).GetProperty("code").GetString().Should().Be("block_not_found");
        again.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await scenario.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Lista_Pagina_De_20_Con_Cursor_Y_Rechaza_Limites_Invalidos_CA_043_10_CA_043_12()
    {
        await using var scenario = await Scenario.StartAsync();
        var blocked = new List<string>();
        for (var i = 0; i < 25; i++)
        {
            var target = await scenario.SeedUserAsync($"Demo {i:00}");
            (await scenario.SendAsync(HttpMethod.Post, $"/api/v1/usuarios/{target}/bloqueo", scenario.Caller)).EnsureSuccessStatusCode();
            blocked.Add(target);
        }

        using var firstPage = await scenario.SendAsync(HttpMethod.Get, _list, scenario.Caller);
        var first = await Scenario.ReadAsync(firstPage);
        var cursor = first.GetProperty("nextCursor").GetString()!;
        using var secondPage = await scenario.SendAsync(HttpMethod.Get, $"{_list}?cursor={Uri.EscapeDataString(cursor)}", scenario.Caller);
        var second = await Scenario.ReadAsync(secondPage);
        using var tooMany = await scenario.SendAsync(HttpMethod.Get, $"{_list}?limite=51", scenario.Caller);
        using var badCursor = await scenario.SendAsync(HttpMethod.Get, $"{_list}?cursor=esto-no-es-un-cursor", scenario.Caller);

        first.GetProperty("items").GetArrayLength().Should().Be(20);
        second.GetProperty("items").GetArrayLength().Should().Be(5);
        second.TryGetProperty("nextCursor", out _).Should().BeFalse();
        first.GetProperty("items").EnumerateArray().Concat(second.GetProperty("items").EnumerateArray()).Select(i => i.GetProperty("usuarioId").GetString())
            .Should().OnlyHaveUniqueItems().And.BeEquivalentTo(blocked, "no se repite ni se salta ninguno");
        first.GetProperty("items")[0].GetProperty("usuarioId").GetString().Should().Be(blocked[^1], "el más reciente primero");
        tooMany.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        badCursor.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task La_Lista_Es_Solo_De_Quien_Llama_Y_Un_Id_En_El_Cuerpo_Se_Ignora_CA_043_29_CA_043_34()
    {
        await using var scenario = await Scenario.StartAsync();
        var target = await scenario.SeedUserAsync("Demo Privada");
        var victim = Guid.NewGuid().ToString("D");

        using var block = await scenario.SendAsync(HttpMethod.Post, $"/api/v1/usuarios/{target}/bloqueo", scenario.Caller, rawJson: $"{{\"bloqueaId\":\"{victim}\",\"sub\":\"{victim}\"}}");
        using var mine = await scenario.SendAsync(HttpMethod.Get, _list, scenario.Caller);
        using var theirs = await scenario.SendAsync(HttpMethod.Get, _list, victim);

        block.StatusCode.Should().Be(HttpStatusCode.Created);
        (await Scenario.ReadAsync(mine)).GetProperty("items").GetArrayLength().Should().Be(1);
        (await Scenario.ReadAsync(theirs)).GetProperty("items").GetArrayLength().Should().Be(0, "la lista de otra persona no se toca");
        var saved = await scenario.Blocks.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        saved["bloqueaId"].AsString.Should().Be(scenario.Caller);
    }

    [Fact]
    public async Task Los_Errores_No_Llevan_Datos_Personales_CA_043_35()
    {
        await using var scenario = await Scenario.StartAsync();
        var target = await scenario.SeedUserAsync("Demo Privado Nombre");

        using var notFound = await scenario.SendAsync(HttpMethod.Post, $"/api/v1/usuarios/{Guid.NewGuid():D}/bloqueo", scenario.Caller);
        using var notBlocked = await scenario.SendAsync(HttpMethod.Delete, $"/api/v1/usuarios/{target}/bloqueo", scenario.Caller);
        using var invalid = await scenario.SendAsync(HttpMethod.Post, "/api/v1/usuarios/no-es-un-id/bloqueo", scenario.Caller);

        foreach (var response in new[] { notFound, notBlocked, invalid })
        {
            var body = await response.Content.ReadAsStringAsync();
            body.Should().NotContain(scenario.Caller).And.NotContain(target).And.NotContain("Demo Privado Nombre");
        }
    }

    /// <summary>Un host de Identidad con la colección de bloqueos lista, una persona que llama y cuentas sembradas.</summary>
    private sealed class Scenario : IAsyncDisposable
    {
        private readonly IdentityApiFactory _factory;
        private readonly HttpClient _client;

        private Scenario(IdentityApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        public string Caller { get; } = Guid.NewGuid().ToString("D");

        public IMongoCollection<BsonDocument> Blocks => _factory.Database.GetCollection<BsonDocument>("bloqueos");

        public static async Task<Scenario> StartAsync()
        {
            var factory = await IdentityApiFactory.CreateAsync();
            await factory.Services.GetRequiredService<BloqueosStore>().EnsureIndexesAsync(CancellationToken.None);
            var scenario = new Scenario(factory);
            await scenario.SeedUserAsync("Demo Quien Llama", scenario.Caller);
            return scenario;
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
                { "celularHash", new BsonBinaryData(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)) },
            });
            return id;
        }

        public Task<long> CountAsync() => Blocks.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);

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

        public async ValueTask DisposeAsync()
        {
            _client.Dispose();
            await _factory.DisposeAsync();
        }
    }
}
