using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Knock.Market.Api.Common.Crypto;
using Knock.Market.Api.Modules.Requests;
using Knock.Market.Api.Modules.Tecnicos.Data;
using Knock.Market.Api.Modules.Tecnicos.Domain;
using Knock.Market.Api.Modules.Tecnicos.Dtos;
using Microsoft.AspNetCore.WebUtilities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.Market.Api.Modules.Tecnicos.Services;

internal sealed class TecnicoTrabajoService(TrabajoStore store, FieldCipher cipher, IRequestsApi requestsApi, TimeProvider clock)
{
    public async Task<(TrabajoResponse? Response, string? Error)> GetAsync(string sub, string? cursor, int limit, CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 20) return (null, "invalid_limit");
        if (!TryDecode(cursor, out var after)) return (null, "invalid_cursor");
        try
        {
            var technician = await store.ReadTechnicianAsync(sub, cancellationToken);
            if (technician is null || Text(technician, "estado") != "activo") return (null, "not_a_technician");
            var verification = await store.ReadVerificationAsync(sub, cancellationToken);
            if (verification?.ErrorCode is not null || verification?.Estado is null) return (null, "unavailable");
            var upcoming = await store.ReadUpcomingAsync(sub, cancellationToken);
            var quotes = await store.ReadQuotesAsync(sub, cancellationToken);
            var finished = await store.ReadFinishedAsync(sub, after, limit, cancellationToken);
            var count = await store.CountFinishedAsync(sub, cancellationToken);
            var ids = upcoming.Concat(quotes).Concat(finished).Select(RequestId).Where(x => x.HasValue).Select(x => x!.Value);
            var requests = await store.ReadRequestsAsync(ids, cancellationToken);
            var now = clock.GetUtcNow().UtcDateTime;
            var visits = upcoming.Select(x => ToVisit(x, requests, technician, now)).ToArray();
            var sent = quotes.Select(x => ToQuote(x, requests)).ToArray();
            var ordered = finished.OrderBy(x => x, Comparer<BsonDocument>.Create((left, right) =>
                TrabajoRules.CompareFinished(Date(left, "fechaTrabajo") ?? throw new FormatException(), left["_id"].AsObjectId.ToString(),
                    Date(right, "fechaTrabajo") ?? throw new FormatException(), right["_id"].AsObjectId.ToString()))).ToArray();
            var items = ordered.Take(limit).Select(x => ToFinished(x, requests)).ToArray();
            var next = ordered.Length > limit ? Encode(ordered[limit - 1]) : null;
            return (new TrabajoResponse(TechnicianRules.IsAvailable(Date(technician, "disponibleHasta"), now),
                new TrabajoVerificationResponse(verification.Estado), null, visits, sent,
                new TrabajoFinishedPage(items, next), new TrabajoSummary(count, null)), null);
        }
        catch (Exception ex) when (ex is MongoException or TimeoutException or CryptographicException or FormatException or InvalidOperationException)
        {
            return (null, "unavailable");
        }
    }

    private TrabajoVisitResponse ToVisit(BsonDocument visit, IReadOnlyDictionary<ObjectId, BsonDocument> requests, BsonDocument technician, DateTime now)
    {
        var id = RequestId(visit);
        var request = id is { } key ? requests.GetValueOrDefault(key) : null;
        var state = Text(visit, "estado");
        var modality = Text(visit, "modalidad");
        var blocked = Bool(visit, "bloqueada");
        string? address = null;
        if (TrabajoRules.CanShowAddress(state) && !blocked)
        {
            var encrypted = TextOrNull(request, "direccionCifrada");
            if (modality == "domicilio" && encrypted is not null && id is { } requestId)
                address = cipher.Decrypt(encrypted, $"solicitudes|direccion|{requestId}");
            if (modality == "taller" && TextOrNull(technician, "tallerDireccion") is { } workshop)
                address = Bool(technician, "demo") ? workshop : cipher.Decrypt(workshop, WorkshopAddressCipher.Context(technician["_id"].AsObjectId));
        }
        var day = Text(visit, "dia");
        return new TrabajoVisitResponse(visit["_id"].AsObjectId.ToString(), id?.ToString() ?? string.Empty, day, Text(visit, "hora"), modality,
            new TrabajoClient(Text(request, "coloniaNombre")), new TrabajoPlace(modality, address),
            Number(visit, "precio"), Number(visit, "tope") ?? 0, state,
            requestsApi.CanArrive(state, modality, day, blocked, Bool(visit, "codigoUsado"), Date(visit, "codigoVenceEn"), now));
    }

    private static TrabajoQuoteResponse ToQuote(BsonDocument quote, IReadOnlyDictionary<ObjectId, BsonDocument> requests)
    {
        var id = RequestId(quote);
        var request = id is { } key ? requests.GetValueOrDefault(key) : null;
        var state = Text(quote, "estado");
        return new TrabajoQuoteResponse(quote["_id"].AsObjectId.ToString(), id?.ToString() ?? string.Empty, Trade(request), Text(quote, "tipoPrecio"),
            Number(quote, "precio"), Number(quote, "precioMin"), Number(quote, "precioMax"), Number(quote, "tope") ?? 0,
            state, TrabajoRules.RejectionReason(state, TextOrNull(quote, "motivoRechazo")), Date(quote, "creadoEn") ?? throw new FormatException());
    }

    private static TrabajoFinishedItem ToFinished(BsonDocument visit, IReadOnlyDictionary<ObjectId, BsonDocument> requests)
    {
        var id = RequestId(visit);
        var request = id is { } key ? requests.GetValueOrDefault(key) : null;
        return new TrabajoFinishedItem(id?.ToString() ?? string.Empty, Trade(request),
            Date(visit, "fechaTrabajo") ?? throw new FormatException(), Number(visit, "precio"), null);
    }

    private static string Trade(BsonDocument? doc) => TextOrNull(doc, "oficioNombre") ?? Text(doc, "oficioClave");
    private static ObjectId? RequestId(BsonDocument doc) => doc.TryGetValue("solicitudId", out var value) && value.IsObjectId ? value.AsObjectId : null;
    private static string Text(BsonDocument? doc, string name) => TextOrNull(doc, name) ?? string.Empty;
    private static string? TextOrNull(BsonDocument? doc, string name) => doc is not null && doc.TryGetValue(name, out var value) && value.IsString ? value.AsString : null;
    private static int? Number(BsonDocument doc, string name) => doc.TryGetValue(name, out var value) && value.IsNumeric ? value.ToInt32() : null;
    private static bool Bool(BsonDocument doc, string name) => doc.TryGetValue(name, out var value) && value.IsBoolean && value.AsBoolean;
    private static DateTime? Date(BsonDocument doc, string name) => doc.TryGetValue(name, out var value) && value.IsBsonDateTime ? value.ToUniversalTime() : null;
    private static string Encode(BsonDocument doc) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes($"{Date(doc, "fechaTrabajo")!.Value.Ticks}:{doc["_id"].AsObjectId}"));

    private static bool TryDecode(string? value, out (DateTime Date, ObjectId Id)? cursor)
    {
        cursor = null;
        if (value is null) return true;
        try
        {
            var parts = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(value)).Split(':');
            if (parts.Length != 2 || !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
                || ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks || !ObjectId.TryParse(parts[1], out var id)) return false;
            cursor = (new DateTime(ticks, DateTimeKind.Utc), id);
            return true;
        }
        catch (FormatException) { return false; }
    }
}
