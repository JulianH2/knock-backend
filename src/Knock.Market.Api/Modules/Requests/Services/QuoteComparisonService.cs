using Knock.Market.Api.Common.Errors;
using Knock.Market.Api.Modules.Requests.Data;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Dtos;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Requests.Services;

/// <summary>
/// Comparar las cotizaciones de una solicitud propia (spec 021, HU-032 y HU-033): lee las cotizaciones y los datos públicos de sus técnicos, las ordena y etiqueta con
/// <see cref="QuoteComparer"/> y dice si el cliente ya puede elegir. Nunca devuelve teléfono, WhatsApp ni direcciones (R-08). No toca MongoDB.
/// </summary>
internal sealed class QuoteComparisonService(RequestsStore requests, ChoiceStore choices, TimeProvider clock)
{
    /// <summary>
    /// Listado para el cliente dueño; <see langword="null"/> (404 uniforme) si la solicitud no existe o es de otra persona. Un <paramref name="id"/> que no es
    /// <c>ObjectId</c> lanza 400. <paramref name="order"/> ya viene validado (<see cref="QuoteComparer.Orders"/>); sin valor es <c>recomendadas</c>.
    /// </summary>
    public async Task<QuoteComparison?> CompareAsync(string clientId, string id, string? order, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out var requestId))
        {
            throw new DomainException(DomainErrorKind.BadRequest, "The request id is not valid.");
        }

        var request = await requests.FindAsync(requestId, cancellationToken);
        if (request is null || !string.Equals(request.ClienteId, clientId, StringComparison.Ordinal))
        {
            return null;
        }

        var documents = await choices.ListQuotesAsync(requestId, cancellationToken);
        var technicians = await choices.FindTechniciansAsync([.. documents.Select(d => d["tecnicoId"].AsObjectId).Distinct()], cancellationToken);
        var quotes = documents.Select(d => ToQuote(d, technicians)).ToList();
        var compared = QuoteComparer.Compare(quotes, order ?? QuoteComparer.Recommended);

        var visits = await choices.FindVisitIdsAsync([requestId], cancellationToken);
        var visitId = visits.TryGetValue(requestId, out var found) ? found.ToString() : null;
        var block = ChoiceRules.Block(request.Estado, request.VenceEnUtc, clock.GetUtcNow().UtcDateTime, quotes.Count(q => q.Estado == "enviada"));
        var items = compared.Items.Select(ToItem).ToList();
        return new QuoteComparison(requestId.ToString(), items, items.Count, compared.TiposMixtos, block is null, block, block == RequestErrorCodes.RequestAlreadyChosenCode ? visitId : null);
    }

    private static QuoteComparisonItem ToItem(ComparedQuote compared)
    {
        var q = compared.Quote;
        return new QuoteComparisonItem(
            q.Id.ToString(),
            q.Estado,
            q.TipoPrecio,
            q.Precio,
            q.PrecioMin,
            q.PrecioMax,
            q.Tope,
            q.RequiereAprobacion,
            q.Dia,
            q.Hora,
            q.Nota,
            compared.Cubre,
            compared.Etiquetas,
            new QuoteTechnician(q.TecnicoId, q.TecnicoNombre, q.TecnicoFotoUrl, q.Calificacion, q.Verificado, q.NivelVerificacion),
            q.Estado == "rechazada" ? q.MotivoRechazo : null);
    }

    private static QuoteToCompare ToQuote(BsonDocument document, IReadOnlyDictionary<ObjectId, TechnicianPublic> technicians)
    {
        var technicianId = document["tecnicoId"].AsObjectId;
        technicians.TryGetValue(technicianId, out var technician);
        return new QuoteToCompare(
            document["_id"].AsObjectId,
            document["estado"].AsString,
            document["tipoPrecio"].AsString,
            Number(document, "precio"),
            Number(document, "precioMin"),
            Number(document, "precioMax"),
            document["tope"].ToInt32(),
            document["requiereAprobacion"].AsBoolean,
            document["dia"].AsString,
            document["hora"].AsString,
            Text(document, "nota"),
            Text(document, "motivoRechazo"),
            document["creadoEn"].ToUniversalTime(),
            technicianId.ToString(),
            technician?.Name ?? "Técnico",
            technician?.PhotoUrl,
            technician?.Rating,
            technician?.VerificationLevel ?? QuoteComparer.NoVerification);
    }

    private static int? Number(BsonDocument document, string name) => document.TryGetValue(name, out var value) && value.IsNumeric ? value.ToInt32() : null;

    private static string? Text(BsonDocument document, string name) => document.TryGetValue(name, out var value) && value.IsString ? value.AsString : null;
}
