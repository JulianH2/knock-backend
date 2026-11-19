using Knock.Identity.Api.Modules.Account.Domain;

namespace Knock.Identity.Api.Modules.Account.Services;

/// <summary>
/// Pendientes antes de eliminar la cuenta (spec 039, USR-09; R-24): conteos e identificadores de las citas y cotizaciones abiertas de la propia persona,
/// que Mercado entrega por la consulta interna. Sin paginación y con un máximo de 50 identificadores por grupo; nada de otras personas.
/// </summary>
internal sealed class PendingService(IMarketAccountClient market)
{
    /// <summary>Máximo de identificadores por grupo (R-24).</summary>
    public const int MaxIds = 50;

    public async Task<AccountPending> ListAsync(string sub, CancellationToken cancellationToken)
    {
        var pending = await market.GetPendingAsync(sub, cancellationToken);
        // Mercado ya limita a 50; el tope se vuelve a aplicar aquí para que el contrato no dependa de que el otro servicio lo cumpla.
        var appointments = pending.AppointmentIds.Take(MaxIds).ToArray();
        var quotes = pending.QuoteIds.Take(MaxIds).ToArray();
        return pending with
        {
            AppointmentIds = appointments,
            AppointmentTotal = Math.Max(pending.AppointmentTotal, appointments.Length),
            QuoteIds = quotes,
            QuoteTotal = Math.Max(pending.QuoteTotal, quotes.Length),
        };
    }
}
