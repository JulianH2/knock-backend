using Knock.Market.Api.Common.Crypto;
using Knock.Market.Api.Common.Errors;
using Knock.Market.Api.Modules.Requests.Data;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Dtos;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Requests.Services;

/// <summary>Resultado de elegir: solo <see cref="ChooseStatus.Chosen"/> trae la cita; los demás son reglas de negocio con su <c>code</c> (spec 021, §5).</summary>
internal sealed record ChooseOutcome(ChooseStatus Status, Appointment? Appointment = null);

/// <summary>
/// Elegir una cotización (spec 021, HU-033): delega la transacción atómica en <see cref="ChoiceStore"/> y arma la cita con el contacto público del técnico y el lugar
/// (la dirección del cliente descifrada para él, o la del taller). El código de visita no se devuelve (R-17) y nada de esto se registra (R-19).
/// </summary>
internal sealed class ChooseQuoteService(ChoiceStore choices, FieldCipher cipher, TimeProvider clock)
{
    private const string _domicile = "domicilio";

    /// <summary>
    /// <see cref="ChooseStatus.NotFound"/> si la solicitud no existe, es de otra persona o la cotización no pertenece a ella (404 uniforme). Un <paramref name="id"/> que no
    /// es <c>ObjectId</c> lanza 400; <paramref name="quoteId"/> ya viene validado como <c>ObjectId</c>.
    /// </summary>
    public async Task<ChooseOutcome> ChooseAsync(string clientId, string id, string quoteId, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out var requestId))
        {
            throw new DomainException(DomainErrorKind.BadRequest, "The request id is not valid.");
        }

        var result = await choices.ChooseAsync(requestId, ObjectId.Parse(quoteId), clientId, clock.GetUtcNow().UtcDateTime, cancellationToken);
        if (result.Visit is not { } visit)
        {
            return new ChooseOutcome(result.Status);
        }

        var technicians = await choices.FindTechniciansAsync([visit.TechnicianId], cancellationToken);
        technicians.TryGetValue(visit.TechnicianId, out var technician);
        var address = technician?.WorkshopAddress;
        if (visit.Modality == _domicile)
        {
            address = visit.EncryptedClientAddress is { } encrypted ? cipher.Decrypt(encrypted, SolicitudService.AddressContext(visit.RequestId)) : null;
        }

        return new ChooseOutcome(
            ChooseStatus.Chosen,
            new Appointment(
                visit.VisitId.ToString(),
                visit.RequestId.ToString(),
                visit.QuoteId.ToString(),
                "confirmada",
                visit.Modality,
                visit.Day,
                visit.Time,
                visit.Price,
                visit.Cap,
                new ChosenTechnician(
                    visit.TechnicianId.ToString(),
                    technician?.Name ?? "Técnico",
                    technician?.PhotoUrl,
                    technician is not null && technician.VerificationLevel != QuoteComparer.NoVerification,
                    technician?.VerificationLevel ?? QuoteComparer.NoVerification,
                    technician?.Phone,
                    technician?.WhatsApp),
                new AppointmentPlace(visit.Modality, address)));
    }
}
