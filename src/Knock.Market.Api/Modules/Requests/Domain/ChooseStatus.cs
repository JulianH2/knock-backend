namespace Knock.Market.Api.Modules.Requests.Domain;

/// <summary>Resultado de elegir una cotización (spec 021, R-10 a R-13 y R-20). Solo <see cref="Chosen"/> trae la cita.</summary>
internal enum ChooseStatus
{
    Chosen,
    NotFound,
    AlreadyChosen,
    Expired,
    Cancelled,
    WithoutQuotes,
    QuoteUnavailable,
    ServiceUnavailable,
}
