extern alias MarketApi;

using System.Globalization;
using MarketApi::Knock.Market.Api.Common.Crypto;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.IntegrationTest.Requests;

/// <summary>Un técnico sembrado para las pruebas de la spec 021: su <c>tecnicos._id</c> y su <c>sub</c>.</summary>
internal sealed record SeededTechnician(ObjectId Id, string Sub);

/// <summary>Apoyo de las pruebas de la spec 021 sobre el host de la 020: técnicos con datos públicos, cotizaciones y solicitudes en el estado que haga falta (MongoDB real, solo datos ficticios).</summary>
internal static class Spec021Host
{
    public const string TechnicianPhone = "5512345678";
    public const string TechnicianWhatsApp = "5598765432";
    public const string WorkshopAddress = "Taller Ficticio 45, Colonia Piloto";
    public const string TechnicianName = "Demo Tecnico Uno";

    private static int _sequence;

    /// <summary>Crea una solicitud por la API (domicilio con dirección, o taller sin ella) y devuelve su id.</summary>
    public static async Task<ObjectId> CreateRequestAsync(this Spec020Host host, bool workshop = false, string? clientSub = null)
    {
        var body = workshop ? host.ValidBody(Spec020Host.Workshop, address: null) : host.ValidBody();
        using var response = await host.CreateAsync(body, clientSub);
        response.EnsureSuccessStatusCode();
        return ObjectId.Parse((await Spec020Host.ReadAsync(response)).GetProperty("id").GetString()!);
    }

    public static async Task<SeededTechnician> SeedPublicTechnicianAsync(this Spec020Host host, string level = "ninguno", double? rating = null, bool withContact = true, bool withWorkshop = false, string name = TechnicianName)
    {
        var sub = Guid.NewGuid().ToString("D");
        var id = ObjectId.GenerateNewId();
        var document = new BsonDocument
        {
            { "_id", id }, { "usuarioId", sub }, { "nombre", name }, { "estado", "activo" }, { "oculto", false },
            { "oficios", new BsonArray { Spec020Host.Plumbing, Spec020Host.Workshop } }, { "coloniaBaseId", host.ColoniaId.ToString() }, { "radioAtencionKm", 5 },
            { "nivelVerificacion", level },
        };
        if (rating is { } value)
        {
            document.Add("calificacion", value);
        }

        if (withContact)
        {
            document.Add("contactoPublico", new BsonDocument { { "telefono", TechnicianPhone }, { "whatsapp", TechnicianWhatsApp } });
        }

        if (withWorkshop)
        {
            // Spec 046, §6: la dirección del taller se guarda cifrada y ligada al documento, como la escribe el registro de técnicos.
            document.Add("tallerDireccion", WorkshopAddressCipher.Encrypt(host.Services.GetRequiredService<FieldCipher>(), id, WorkshopAddress));
        }

        await host.Database.GetCollection<BsonDocument>("tecnicos").InsertOneAsync(document);
        return new SeededTechnician(id, sub);
    }

    /// <summary>Inserta una cotización directamente (la spec 020 ya prueba el envío); <c>creadoEn</c> crece con cada llamada para que el orden de desempate sea predecible.</summary>
    public static async Task<ObjectId> SeedQuoteAsync(this Spec020Host host, ObjectId requestId, SeededTechnician technician, string type = "fijo", int? price = 1000, int? min = null, int? max = null, int? cap = null, string day = "", string time = "10:00", string state = "enviada")
    {
        var id = ObjectId.GenerateNewId();
        var created = DateTime.UtcNow.AddSeconds(Interlocked.Increment(ref _sequence) - 100_000);
        await host.Database.GetCollection<BsonDocument>("cotizaciones").InsertOneAsync(new BsonDocument
        {
            { "_id", id }, { "solicitudId", requestId }, { "tecnicoId", technician.Id }, { "tecnicoUsuarioId", technician.Sub }, { "tipoPrecio", type },
            { "precio", price is { } p ? p : BsonNull.Value }, { "precioMin", min is { } a ? a : BsonNull.Value }, { "precioMax", max is { } b ? b : BsonNull.Value },
            { "tope", cap ?? price ?? max ?? 0 }, { "requiereAprobacion", false }, { "dia", day.Length == 0 ? Spec020Host.Tomorrow() : day }, { "hora", time },
            { "nota", BsonNull.Value }, { "estado", state }, { "creadoEn", created }, { "actualizadoEn", created },
        });
        return id;
    }

    public static async Task SetRequestAsync(this Spec020Host host, ObjectId requestId, string state, DateTime? expiresAtUtc = null)
    {
        var update = Builders<BsonDocument>.Update.Set("estado", state);
        if (expiresAtUtc is { } expires)
        {
            update = update.Set("venceEn", expires);
        }

        await host.Database.GetCollection<BsonDocument>("solicitudes").UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", requestId), update);
    }

    /// <summary>Una solicitud creada y puesta en <c>cotizando</c> con una cotización por técnico dado.</summary>
    public static async Task<(ObjectId RequestId, List<ObjectId> QuoteIds, List<SeededTechnician> Technicians)> QuotedRequestAsync(this Spec020Host host, int quotes = 3, bool workshop = false)
    {
        var requestId = await host.CreateRequestAsync(workshop);
        await host.SetRequestAsync(requestId, "cotizando");
        var technicians = new List<SeededTechnician>();
        var ids = new List<ObjectId>();
        for (var i = 0; i < quotes; i++)
        {
            var technician = await host.SeedPublicTechnicianAsync(withWorkshop: workshop, name: TechnicianName + " " + i.ToString(CultureInfo.InvariantCulture));
            technicians.Add(technician);
            ids.Add(await host.SeedQuoteAsync(requestId, technician, price: 900 + (i * 200)));
        }

        return (requestId, ids, technicians);
    }

    public static Task<HttpResponseMessage> CompareAsync(this Spec020Host host, ObjectId requestId, string? order = null, string? sub = null) =>
        host.SendAsync(HttpMethod.Get, $"/api/v1/solicitudes/{requestId}/cotizaciones" + (order is null ? string.Empty : "?orden=" + order), sub ?? host.ClientSub);

    public static Task<HttpResponseMessage> ChooseAsync(this Spec020Host host, ObjectId requestId, ObjectId quoteId, string? sub = null) =>
        host.SendAsync(HttpMethod.Post, $"/api/v1/solicitudes/{requestId}/elegir", sub ?? host.ClientSub, new { cotizacionId = quoteId.ToString() });

    public static Task<long> CountAsync(this Spec020Host host, string collection, FilterDefinition<BsonDocument>? filter = null) =>
        host.Database.GetCollection<BsonDocument>(collection).CountDocumentsAsync(filter ?? FilterDefinition<BsonDocument>.Empty);

    public static Task<BsonDocument> OneAsync(this Spec020Host host, string collection, ObjectId id) =>
        host.Database.GetCollection<BsonDocument>(collection).Find(Builders<BsonDocument>.Filter.Eq("_id", id)).SingleAsync();
}
