namespace Knock.Market.Api.Modules.Requests.Domain;

/// <summary>Motivos por los que una conversación está cerrada (spec 030, R-03). Son códigos en inglés y <c>snake_case</c>, sin datos de nadie.</summary>
internal static class ConversationClosures
{
    public const string RequestCancelled = "request_cancelled";
    public const string RequestExpired = "request_expired";
    public const string OtherQuoteChosen = "other_quote_chosen";
    public const string Dismissed = "dismissed";
    public const string ServiceFinished = "service_finished";
}

/// <summary>Si la conversación de un par (solicitud, técnico) admite mensajes y, si no, por qué (spec 030, R-03).</summary>
internal readonly record struct ConversationOpening(bool Open, string? Reason)
{
    public static ConversationOpening Opened { get; } = new(true, null);

    public static ConversationOpening Closed(string reason) => new(false, reason);
}

/// <summary>
/// Regla pura de apertura de una conversación (spec 030, R-03): con la solicitud en <c>enviada</c> o <c>cotizando</c> está abierta mientras el técnico siga en juego; con la cita
/// confirmada solo para el técnico elegido; en cualquier otro caso está cerrada con su motivo. Una conversación cerrada se puede leer pero no recibe mensajes.
/// </summary>
internal static class ConversationRules
{
    /// <param name="request">Estado de la solicitud.</param>
    /// <param name="quote">Estado de la cotización de ese técnico en esa solicitud.</param>
    public static ConversationOpening Evaluate(RequestState request, QuoteState quote)
    {
        var chosen = quote == QuoteState.Aceptada;
        return request switch
        {
            RequestState.Cancelada => ConversationOpening.Closed(ConversationClosures.RequestCancelled),
            RequestState.Vencida => ConversationOpening.Closed(ConversationClosures.RequestExpired),
            RequestState.Enviada or RequestState.Cotizando => OpenWhileInPlay(quote),
            RequestState.CitaConfirmada => chosen ? ConversationOpening.Opened : ConversationOpening.Closed(ConversationClosures.OtherQuoteChosen),
            _ => ConversationOpening.Closed(chosen ? ConversationClosures.ServiceFinished : ConversationClosures.OtherQuoteChosen),
        };
    }

    private static ConversationOpening OpenWhileInPlay(QuoteState quote) =>
        quote is QuoteState.Retirada or QuoteState.Rechazada ? ConversationOpening.Closed(ConversationClosures.Dismissed) : ConversationOpening.Opened;
}
