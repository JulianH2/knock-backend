using Knock.Market.Api.Common.Crypto;
using Knock.Market.Api.Common.Errors;
using Knock.Market.Api.Modules.Requests.Data;
using Knock.Market.Api.Modules.Requests.Domain;
using Knock.Market.Api.Modules.Requests.Dtos;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Requests.Services;

/// <summary>
/// Ver la visita según quién pide (spec 022, HU-036): el cliente dueño ve al técnico, el lugar y, cuando corresponde, el código; el técnico elegido ve la dirección y la
/// colonia del cliente y nunca el código, el nombre ni el teléfono. Son dos proyecciones distintas: el código solo se descifra en la del cliente. No toca MongoDB y nunca
/// registra el código ni la dirección (R-14).
/// </summary>
internal sealed class VisitService(VisitStore visits, RequestsStore requests, ChoiceStore choices, FieldCipher cipher, TimeProvider clock)
{
    /// <summary>
    /// La vista del rol que corresponde a <paramref name="sub"/>, o <see langword="null"/> (404 uniforme) si la visita no existe o no es suya ni como cliente ni como técnico.
    /// Un <paramref name="id"/> que no es <c>ObjectId</c> lanza 400.
    /// </summary>
    public async Task<VisitResponse?> GetAsync(string sub, string id, CancellationToken cancellationToken)
    {
        var visit = await FindAsync(id, cancellationToken);
        return visit is null ? null : await ViewForAsync(sub, visit, cancellationToken);
    }

    /// <summary>La vista del técnico (con <c>llegadaEn</c>) tras confirmar su llegada; el llamador ya comprobó que es el técnico elegido.</summary>
    public Task<VisitResponse> TechnicianViewAsync(VisitRecord visit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(visit);
        return BuildTechnicianViewAsync(visit, cancellationToken);
    }

    /// <summary>Lee la visita; <see langword="null"/> si no existe. Un <paramref name="id"/> que no es <c>ObjectId</c> lanza 400.</summary>
    public async Task<VisitRecord?> FindAsync(string id, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out var visitId))
        {
            throw new DomainException(DomainErrorKind.BadRequest, "The visit id is not valid.");
        }

        return await visits.FindAsync(visitId, cancellationToken);
    }

    private async Task<VisitResponse?> ViewForAsync(string sub, VisitRecord visit, CancellationToken cancellationToken)
    {
        if (string.Equals(visit.ClientId, sub, StringComparison.Ordinal))
        {
            return await BuildClientViewAsync(visit, cancellationToken);
        }

        return string.Equals(visit.TechnicianUserId, sub, StringComparison.Ordinal) ? await BuildTechnicianViewAsync(visit, cancellationToken) : null;
    }

    private async Task<VisitResponse> BuildClientViewAsync(VisitRecord visit, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var technician = await FindTechnicianAsync(visit, cancellationToken);
        var name = technician?.Name ?? "Técnico";
        var hidden = VisitRules.CodeHiddenReason(visit.State, now);
        string? code = null;
        if (hidden is null && visit.EncryptedCode is { } encrypted)
        {
            code = cipher.Decrypt(encrypted, VisitCodeFactory.Context(visit.Id));
        }

        var place = visit.Modality == VisitRules.Domicile
            ? new VisitPlace(visit.Modality, await ClientAddressAsync(visit, cancellationToken))
            : new VisitPlace(visit.Modality, technician?.WorkshopAddress);
        return new VisitResponse(
            visit.Id.ToString(),
            "cliente",
            visit.Estado,
            visit.Modality,
            visit.Day,
            visit.Time,
            visit.Price,
            visit.Cap,
            new VisitTechnician(name, technician?.PhotoUrl, technician is not null && technician.VerificationLevel != QuoteComparer.NoVerification, technician?.VerificationLevel ?? QuoteComparer.NoVerification, technician?.Phone, technician?.WhatsApp),
            null,
            place,
            code,
            code is null ? null : Utc(visit.CodeExpiresAtUtc),
            code is null ? hidden : null,
            code is null ? null : VisitRules.SafetyRule(name),
            VisitRules.CanNotify(visit.Estado),
            false,
            null);
    }

    private async Task<VisitResponse> BuildTechnicianViewAsync(VisitRecord visit, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var request = await requests.FindAsync(visit.RequestId, cancellationToken);
        VisitPlace place;
        if (visit.Modality == VisitRules.Domicile)
        {
            place = new VisitPlace(visit.Modality, await ClientAddressAsync(visit, cancellationToken));
        }
        else
        {
            place = new VisitPlace(visit.Modality, (await FindTechnicianAsync(visit, cancellationToken))?.WorkshopAddress);
        }

        return new VisitResponse(
            visit.Id.ToString(),
            "tecnico",
            visit.Estado,
            visit.Modality,
            visit.Day,
            visit.Time,
            visit.Price,
            visit.Cap,
            null,
            new VisitCustomer(request?.ColoniaNombre ?? string.Empty),
            place,
            null,
            null,
            null,
            null,
            null,
            VisitRules.CanArrive(visit.State, now),
            Utc(visit.ArrivedAtUtc));
    }

    private async Task<TechnicianPublic?> FindTechnicianAsync(VisitRecord visit, CancellationToken cancellationToken)
    {
        var found = await choices.FindTechniciansAsync([visit.TechnicianId], cancellationToken);
        return found.GetValueOrDefault(visit.TechnicianId);
    }

    /// <summary>La dirección que el cliente escribió en su solicitud, descifrada; la conserva la cita aunque el cliente la cambie después.</summary>
    private async Task<string?> ClientAddressAsync(VisitRecord visit, CancellationToken cancellationToken)
    {
        var request = await requests.FindAsync(visit.RequestId, cancellationToken);
        return request?.DireccionCifrada is { } encrypted ? cipher.Decrypt(encrypted, SolicitudService.AddressContext(visit.RequestId)) : null;
    }

    private static DateTimeOffset? Utc(DateTime? value) => value is { } utc ? new DateTimeOffset(utc, TimeSpan.Zero) : null;
}
