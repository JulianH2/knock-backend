namespace Knock.Market.Api.Modules.Requests.Domain;

/// <summary>Decisiones puras para los cierres de la spec 029.</summary>
internal static class TransicionesDeSolicitud
{
    public static bool CanClose(RequestState current, RequestState destination) =>
        RequestRules.AcceptsQuotes(current) && destination is RequestState.Cancelada or RequestState.Vencida;

    public static string? CancellationError(RequestState current) => current switch
    {
        RequestState.CitaConfirmada or RequestState.Terminada or RequestState.Calificada => "request_with_appointment",
        RequestState.Vencida => RequestErrorCodes.RequestExpiredCode,
        RequestState.Enviada or RequestState.Cotizando or RequestState.Cancelada => null,
        _ => throw new ArgumentOutOfRangeException(nameof(current), current, "Unknown request state."),
    };
}
