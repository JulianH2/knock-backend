using System.Globalization;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.IntegrationTest.Requests;

/// <summary>Una visita confirmada para las pruebas de la spec 022: su id, el técnico elegido, la solicitud y el código en claro (leído por la vista del cliente).</summary>
internal sealed record ConfirmedVisit(ObjectId VisitId, SeededTechnician Technician, ObjectId RequestId, string? Code);

/// <summary>Apoyo de las pruebas de la spec 022 sobre el host de la 020 y el flujo de la 021 (MongoDB real, solo datos ficticios).</summary>
internal static class Spec022Host
{
    /// <summary>Crea la solicitud, deja una cotización, la elige por la API y lee el código por la vista del cliente. En taller no hay código.</summary>
    public static async Task<ConfirmedVisit> ConfirmedVisitAsync(this Spec020Host host, bool workshop = false, string level = "basica")
    {
        var requestId = await host.CreateRequestAsync(workshop);
        await host.SetRequestAsync(requestId, "cotizando");
        var technician = await host.SeedPublicTechnicianAsync(level, 4.5, withWorkshop: workshop);
        var quote = await host.SeedQuoteAsync(requestId, technician);
        (await host.ChooseAsync(requestId, quote)).EnsureSuccessStatusCode();
        var visit = await host.Database.GetCollection<BsonDocument>("visitas").Find(Builders<BsonDocument>.Filter.Eq("solicitudId", requestId)).SingleAsync();
        var visitId = visit["_id"].AsObjectId;
        string? code = null;
        if (!workshop)
        {
            using var view = await host.VisitAsync(visitId);
            code = (await Spec020Host.ReadAsync(view)).GetProperty("codigo").GetString();
        }

        return new ConfirmedVisit(visitId, technician, requestId, code);
    }

    /// <summary>Pone el día de la cita en hoy (hora de Ciudad de México) y el código vigente un día más, para que la ventana de llegada esté abierta.</summary>
    public static Task OpenWindowAsync(this Spec020Host host, ObjectId visitId) =>
        host.SetVisitAsync(visitId, Builders<BsonDocument>.Update.Set("dia", Today()).Set("codigoVenceEn", DateTime.UtcNow.AddDays(1)));

    public static Task SetVisitAsync(this Spec020Host host, ObjectId visitId, UpdateDefinition<BsonDocument> update) =>
        host.Database.GetCollection<BsonDocument>("visitas").UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", visitId), update);

    public static Task<BsonDocument> VisitDocAsync(this Spec020Host host, ObjectId visitId) => host.OneAsync("visitas", visitId);

    public static Task<HttpResponseMessage> VisitAsync(this Spec020Host host, ObjectId visitId, string? sub = null) =>
        host.SendAsync(HttpMethod.Get, $"/api/v1/visitas/{visitId}", sub ?? host.ClientSub);

    public static Task<HttpResponseMessage> ArriveAsync(this Spec020Host host, ObjectId visitId, string sub, string? code) =>
        host.SendAsync(HttpMethod.Post, $"/api/v1/visitas/{visitId}/llegada", sub, new { codigo = code });

    public static Task<HttpResponseMessage> NoticeAsync(this Spec020Host host, ObjectId visitId, string? sub = null) =>
        host.SendAsync(HttpMethod.Post, $"/api/v1/visitas/{visitId}/aviso", sub ?? host.ClientSub, rawJson: "{}");

    public static async Task<long> EventsAsync(this Spec020Host host, string type) =>
        await host.CountAsync("outbox", Builders<BsonDocument>.Filter.Eq("tipo", type));

    /// <summary>Un código de cuatro dígitos distinto del verdadero.</summary>
    public static string WrongCode(string real) => real == "0000" ? "1111" : "0000";

    private static string Today() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City")).DateTime).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
