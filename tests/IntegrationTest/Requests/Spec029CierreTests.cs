extern alias MarketApi;

using AwesomeAssertions;
using MarketApi::Knock.Market.Api.Modules.Requests.Services;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Knock.IntegrationTest.Requests;

public sealed class Spec029CierreTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cierre_Transaccional_Actualiza_Cascada_R_029_03_R_029_09(bool expire)
    {
        await using var host = await Spec020Host.StartAsync();
        using var created = await host.CreateAsync(host.ValidBody());
        var id = ObjectId.Parse((await Spec020Host.ReadAsync(created)).GetProperty("id").GetString());
        var quotes = host.Database.GetCollection<BsonDocument>("cotizaciones");
        var deliveries = host.Database.GetCollection<BsonDocument>("envios");
        var technician = ObjectId.GenerateNewId();
        await quotes.InsertOneAsync(new BsonDocument { { "solicitudId", id }, { "tecnicoId", technician }, { "estado", "enviada" } });
        await deliveries.InsertOneAsync(new BsonDocument { { "solicitudId", id }, { "estado", "activo" } });
        if (expire)
        {
            await host.Database.GetCollection<BsonDocument>("solicitudes").UpdateOneAsync(
                new BsonDocument("_id", id), new BsonDocument("$set", new BsonDocument("venceEn", DateTime.UtcNow.AddHours(-1))));
        }

        using var scope = host.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<CierreDeSolicitudService>();
        if (expire)
        {
            await service.ExpireAsync(id, CancellationToken.None);
        }
        else
        {
            await service.CancelAsync(id, host.ClientSub, "other", CancellationToken.None);
        }

        var reason = expire ? "request_expired" : "request_cancelled";
        var request = await host.Database.GetCollection<BsonDocument>("solicitudes").Find(new BsonDocument("_id", id)).SingleAsync();
        request["estado"].AsString.Should().Be(expire ? "vencida" : "cancelada");
        var quote = await quotes.Find(new BsonDocument("solicitudId", id)).SingleAsync();
        quote["estado"].AsString.Should().Be("rechazada");
        quote["motivoRechazo"].AsString.Should().Be(reason);
        var delivery = await deliveries.Find(new BsonDocument("solicitudId", id)).SingleAsync();
        delivery["estado"].AsString.Should().Be("cerrado");
        delivery["cerradoMotivo"].AsString.Should().Be(reason);
        var type = expire ? "SolicitudVencida" : "SolicitudCancelada";
        var message = await host.Database.GetCollection<BsonDocument>("outbox").Find(new BsonDocument("_id", type + ":" + id)).SingleAsync();
        message["datos"]["tecnicosAvisar"].AsBsonArray.Should().Contain(technician.ToString());
        message["datos"].AsBsonDocument.ElementCount.Should().Be(expire ? 3 : 2);
        if (expire)
        {
            message["datos"]["clienteId"].AsString.Should().Be(host.ClientSub);
        }
    }

    [Fact]
    public async Task Fallo_De_Buzon_Revierte_Cascada_R_029_03()
    {
        await using var host = await Spec020Host.StartAsync();
        using var created = await host.CreateAsync(host.ValidBody());
        var id = ObjectId.Parse((await Spec020Host.ReadAsync(created)).GetProperty("id").GetString());
        var quotes = host.Database.GetCollection<BsonDocument>("cotizaciones");
        var deliveries = host.Database.GetCollection<BsonDocument>("envios");
        await quotes.InsertOneAsync(new BsonDocument { { "solicitudId", id }, { "tecnicoId", ObjectId.GenerateNewId() }, { "estado", "enviada" } });
        await deliveries.InsertOneAsync(new BsonDocument { { "solicitudId", id }, { "estado", "activo" } });
        host.OutboxFailure.Fail = true;
        using var scope = host.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<CierreDeSolicitudService>();
        var act = () => service.CancelAsync(id, host.ClientSub, "other", CancellationToken.None);
        await act.Should().ThrowAsync<TimeoutException>();
        (await host.Database.GetCollection<BsonDocument>("solicitudes").Find(new BsonDocument("_id", id)).SingleAsync())["estado"].AsString.Should().Be("enviada");
        (await quotes.Find(new BsonDocument("solicitudId", id)).SingleAsync())["estado"].AsString.Should().Be("enviada");
        (await deliveries.Find(new BsonDocument("solicitudId", id)).SingleAsync())["estado"].AsString.Should().Be("activo");
    }
}
