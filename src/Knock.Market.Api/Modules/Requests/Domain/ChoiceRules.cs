namespace Knock.Market.Api.Modules.Requests.Domain;

/// <summary>Regla pura de «¿ya puede elegir?» del listado (spec 021, R-08): el motivo usa el mismo nombre que el error que daría la elección.</summary>
internal static class ChoiceRules
{
    /// <summary>
    /// Motivo por el que no se puede elegir (<c>request_already_chosen</c>, <c>request_cancelled</c>, <c>request_expired</c> o <c>request_without_quotes</c>), o
    /// <see langword="null"/> si la solicitud está <c>cotizando</c>, vigente y con al menos una cotización <c>enviada</c>.
    /// </summary>
    public static string? Block(RequestState state, DateTime expiresAtUtc, DateTime nowUtc, int sentQuotes)
    {
        if (state is RequestState.CitaConfirmada or RequestState.Terminada or RequestState.Calificada)
        {
            return RequestErrorCodes.RequestAlreadyChosenCode;
        }

        if (state == RequestState.Cancelada)
        {
            return RequestErrorCodes.RequestCancelledCode;
        }

        if (state == RequestState.Vencida || expiresAtUtc <= nowUtc)
        {
            return RequestErrorCodes.RequestExpiredCode;
        }

        return state == RequestState.Cotizando && sentQuotes > 0 ? null : RequestErrorCodes.RequestWithoutQuotesCode;
    }
}
