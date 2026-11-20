extern alias MarketApi;

using System.Net;
using AwesomeAssertions;
using MarketApi::Knock.Market.Api.Common.TareasProgramadas;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.IntegrationTest.Requests;

public sealed class Spec028RepartoTests
{
    [Fact]
    public async Task Create_SchedulesFirstWave_AndExecutorSendsSelectedCandidates_CA_028_01()
    {
        var clock = new TestClock();
        await using var host = await StartAsync(clock, true);
        var technicians = host.Database.GetCollection<BsonDocument>("tecnicos");
        for (var i = 0; i < 10; i++)
        {
            await technicians.InsertOneAsync(Technician(host, clock, i));
        }
        var id = await CreateAsync(host);
        var task = await Tasks(host).Find(new BsonDocument("tipo", "RepartirOleada")).SingleAsync();
        task["claveNegocio"].AsString.Should().Be(id + ":1");
        task["estado"].AsString.Should().Be("pendiente");
        await RunAsync(host);
        var sends = await Sends(host).Find(new BsonDocument("solicitudId", ObjectId.Parse(id))).ToListAsync();
        sends.Should().HaveCount(5);
        sends.Count(x => x["motivo"] == "mejor_ordenado").Should().Be(3);
        sends.Count(x => x["motivo"] == "nuevo").Should().Be(1);
        (await Outbox(host).CountDocumentsAsync(new BsonDocument("tipo", "SolicitudEnviadaATecnico"))).Should().Be(5);
    }

    [Fact]
    public async Task Review_WithTooFewQuotes_ExpandsWithoutPreviousRecipients_CA_028_02()
    {
        var clock = new TestClock();
        await using var host = await StartAsync(clock, true);
        for (var i = 0; i < 10; i++) await host.Database.GetCollection<BsonDocument>("tecnicos").InsertOneAsync(Technician(host, clock, i));
        await CreateAsync(host);
        await RunAsync(host);
        clock.Advance(TimeSpan.FromMinutes(15));
        await RunAsync(host);
        await RunAsync(host);
        var sends = await Sends(host).Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        sends.Should().HaveCount(10);
        sends.Select(x => x["tecnicoId"].AsObjectId).Distinct().Should().HaveCount(10);
    }

    [Fact]
    public async Task Review_WithTwoQuotes_DoesNotExpand_CA_028_03()
    {
        var clock = new TestClock();
        await using var host = await StartAsync(clock, true);
        var id = await CreateAsync(host);
        await RunAsync(host);
        await host.Database.GetCollection<BsonDocument>("cotizaciones").InsertManyAsync([
            new BsonDocument { ["solicitudId"] = ObjectId.Parse(id), ["tecnicoId"] = ObjectId.GenerateNewId(), ["estado"] = "enviada" },
            new BsonDocument { ["solicitudId"] = ObjectId.Parse(id), ["tecnicoId"] = ObjectId.GenerateNewId(), ["estado"] = "aceptada" },
        ]);
        clock.Advance(TimeSpan.FromMinutes(15));
        await RunAsync(host);
        (await Tasks(host).CountDocumentsAsync(new BsonDocument("tipo", "RepartirOleada"))).Should().Be(1);
    }

    [Fact]
    public async Task ThreeEmptyWaves_EmitOneEvent_AndNeverScheduleFourth_CA_028_10()
    {
        var clock = new TestClock();
        await using var host = await StartAsync(clock, true);
        await CreateAsync(host);
        for (var i = 0; i < 3; i++)
        {
            await RunAsync(host);
            clock.Advance(TimeSpan.FromMinutes(15));
            await RunAsync(host);
        }
        (await Tasks(host).CountDocumentsAsync(new BsonDocument("tipo", "RepartirOleada"))).Should().Be(3);
        (await Outbox(host).CountDocumentsAsync(new BsonDocument("tipo", "SolicitudSinTecnicos"))).Should().Be(1);
    }

    [Fact]
    public async Task Disabled_DoesNotSchedule_CA_028_16()
    {
        var clock = new TestClock();
        await using var host = await StartAsync(clock, false);
        await CreateAsync(host);
        (await Tasks(host).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(0);
    }

    [Fact]
    public async Task CancelledRequest_IsNotDistributed_CA_028_09()
    {
        var clock = new TestClock();
        await using var host = await StartAsync(clock, true);
        var id = await CreateAsync(host);
        await host.Database.GetCollection<BsonDocument>("solicitudes").UpdateOneAsync(
            new BsonDocument("_id", ObjectId.Parse(id)), new BsonDocument("$set", new BsonDocument("estado", "cancelada")));
        await RunAsync(host);
        (await Sends(host).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(0);
        (await Outbox(host).CountDocumentsAsync(new BsonDocument("tipo", "SolicitudEnviadaATecnico"))).Should().Be(0);
    }

    [Fact]
    public async Task ConcurrentExecutors_DistributeOnce_CA_028_05()
    {
        var clock = new TestClock();
        await using var host = await StartAsync(clock, true);
        for (var i = 0; i < 10; i++) await host.Database.GetCollection<BsonDocument>("tecnicos").InsertOneAsync(Technician(host, clock, i));
        await CreateAsync(host);
        var results = await Task.WhenAll(TryRunAsync(host), TryRunAsync(host));
        results.Count(x => x).Should().Be(1);
        (await Sends(host).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(5);
        (await Outbox(host).CountDocumentsAsync(new BsonDocument("tipo", "SolicitudEnviadaATecnico"))).Should().Be(5);
    }

    private static Task<Spec020Host> StartAsync(TestClock clock, bool enabled) =>
        Spec020Host.StartAsync(new Dictionary<string, string?>
        {
            ["Oleadas:Habilitado"] = enabled.ToString(),
            ["TareasProgramadas:IntervaloSegundos"] = "3600",
        }, services => services.AddSingleton<TimeProvider>(clock));

    private static async Task<string> CreateAsync(Spec020Host host)
    {
        using var response = await host.CreateAsync(host.ValidBody());
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await Spec020Host.ReadAsync(response)).GetProperty("id").GetString()!;
    }

    private static async Task RunAsync(Spec020Host host)
    {
        (await TryRunAsync(host)).Should().BeTrue();
    }

    private static async Task<bool> TryRunAsync(Spec020Host host)
    {
        using var scope = host.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<EjecutorDeTareasProgramadas>().ExecuteOneAsync(CancellationToken.None);
    }

    private static BsonDocument Technician(Spec020Host host, TestClock clock, int i) => new()
    {
        ["_id"] = ObjectId.GenerateNewId(),
        ["usuarioId"] = Guid.NewGuid().ToString(),
        ["estado"] = "activo",
        ["oculto"] = false,
        ["oficios"] = new BsonArray { Spec020Host.Plumbing },
        ["coloniaBaseId"] = host.ColoniaId.ToString(),
        ["disponibleHasta"] = clock.GetUtcNow().UtcDateTime.AddDays(1),
        ["calificacion"] = 10 - i,
        ["trabajosTerminados"] = i == 4 ? 0 : 5,
    };

    private static IMongoCollection<BsonDocument> Tasks(Spec020Host host) => host.Database.GetCollection<BsonDocument>("tareas_programadas");
    private static IMongoCollection<BsonDocument> Sends(Spec020Host host) => host.Database.GetCollection<BsonDocument>("envios");
    private static IMongoCollection<BsonDocument> Outbox(Spec020Host host) => host.Database.GetCollection<BsonDocument>("outbox");

    private sealed class TestClock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now += duration;
    }
}
