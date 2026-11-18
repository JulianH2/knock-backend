using Knock.Market.Api.Modules.Trust.Data;
using Knock.Market.Api.Modules.Trust.Domain;
using Knock.Market.Api.Modules.Trust.Dtos;
using MongoDB.Bson;

namespace Knock.Market.Api.Modules.Trust.Services;

/// <summary>El resultado de guardar un técnico de confianza.</summary>
internal enum TrustSaveStatus
{
    Created,
    AlreadySaved,
    TechnicianNotFound,
}

/// <summary>Resultado de guardar: el estado y, salvo que el técnico no exista o esté oculto, el técnico guardado.</summary>
internal sealed record TrustSaveOutcome(TrustSaveStatus Status, TrustedTechnician? Technician = null)
{
    public sealed override string ToString() => nameof(TrustSaveOutcome);
}

/// <summary>
/// Técnicos de confianza de la persona (spec 042, TCO-01 a TCO-03). La persona es siempre la del <c>sub</c> del token (R-12). Guardar es idempotente: 201 la primera vez y 200 al repetir
/// (R-10). Un técnico oculto sale de la lista sin borrar el registro y reaparece al volver a ser visible (R-11). No toca MongoDB. No se guardan contactos de terceros (R-09).
/// </summary>
internal sealed class TrustService(TrustStore store, TimeProvider clock)
{
    /// <summary>La página de técnicos guardados. Un cursor que no se puede leer lanza 400 (<c>invalid_cursor</c>).</summary>
    public async Task<TrustedTechnicianPage> ListAsync(string sub, TrustedListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        (DateTime, ObjectId)? after = string.IsNullOrEmpty(query.Cursor) ? null : TrustCursor.Decode(query.Cursor);
        var page = await store.ListAsync(sub, after, query.Limite, cancellationToken);
        return new TrustedTechnicianPage([.. page.Items.Select(item => ToResponse(item.Technician, item.Saved.SavedAtUtc))], page.NextCursor);
    }

    /// <summary>Guarda al técnico (ya validado como <c>ObjectId</c>); si no existe o no es visible, <see cref="TrustSaveStatus.TechnicianNotFound"/>.</summary>
    public async Task<TrustSaveOutcome> SaveAsync(string sub, string technicianId, CancellationToken cancellationToken)
    {
        var id = ObjectId.Parse(technicianId);
        if (await store.FindVisibleTechnicianAsync(id, cancellationToken) is not { } technician)
        {
            return new TrustSaveOutcome(TrustSaveStatus.TechnicianNotFound);
        }

        // MongoDB guarda milisegundos: se redondea aquí para que la respuesta de la primera vez y la de cada repetición lleven la misma fecha.
        var now = clock.GetUtcNow().UtcDateTime;
        var saved = await store.SaveAsync(sub, id, now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMillisecond)), cancellationToken);
        return new TrustSaveOutcome(saved.Created ? TrustSaveStatus.Created : TrustSaveStatus.AlreadySaved, ToResponse(technician, saved.SavedAtUtc));
    }

    /// <summary>Quita al técnico de la lista; falso si no estaba (404 <c>trusted_technician_not_found</c>).</summary>
    public Task<bool> RemoveAsync(string sub, string technicianId, CancellationToken cancellationToken) =>
        store.RemoveAsync(sub, ObjectId.Parse(technicianId), cancellationToken);

    private static TrustedTechnician ToResponse(TrustTechnicianView technician, DateTime savedAtUtc) =>
        new(technician.Id.ToString(), technician.Name, technician.PhotoUrl, technician.VerificationLevel, savedAtUtc);
}
