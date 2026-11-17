namespace Knock.Market.Api.Modules.Requests.Domain;

/// <summary>
/// <c>code</c> de negocio de las solicitudes (spec 020, §5): en inglés y en <c>snake_case</c>, sin datos de la persona ni de la solicitud. Se declaran como constantes
/// <c>...Code</c> para que la prueba de convención de errores (spec 002, CA-07) los vea.
/// </summary>
internal static class RequestErrorCodes
{
    public const string TradeInvalidCode = "trade_invalid";
    public const string ColoniaNotFoundCode = "colonia_not_found";
    public const string AddressNotApplicableCode = "address_not_applicable";
    public const string PhotoInvalidCode = "photo_invalid";
    public const string ContentRejectedCode = "content_rejected";
    public const string ModerationUnavailableCode = "moderation_unavailable";
    public const string InvalidCursorCode = "invalid_cursor";

    // Comparar y elegir cotización (spec 021, §5).
    public const string RequestAlreadyChosenCode = "request_already_chosen";
    public const string RequestExpiredCode = "request_expired";
    public const string RequestCancelledCode = "request_cancelled";
    public const string RequestWithoutQuotesCode = "request_without_quotes";
    public const string QuoteUnavailableCode = "quote_unavailable";
    public const string ServiceUnavailableCode = "service_unavailable";
}
