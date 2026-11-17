using Knock.Market.Api.Common.Errors;

namespace Knock.Market.Api.Modules.Requests.Domain;

/// <summary>Reglas puras de la solicitud y sus cotizaciones (spec 020, R-01, R-16 y R-20). Sin acceso a datos: el servicio las aplica y la base las hace cumplir.</summary>
internal static class RequestRules
{
    /// <summary>Horas de vida de una solicitud sin elegir cotización (R-01).</summary>
    public const int LifetimeHours = 48;

    public const string LifetimeConfigurationKey = "Solicitudes:HorasVencimiento";

    /// <summary>Máximo de cotizaciones vivas (<c>enviada</c> o <c>aceptada</c>) por solicitud (R-20).</summary>
    public const int MaxLiveQuotes = 5;

    /// <summary>Vencimiento a partir del instante único de creación (<c>creadoEn</c>), sin una segunda lectura del reloj (R-01).</summary>
    public static DateTimeOffset ExpiresAt(DateTimeOffset createdAt, int hours = LifetimeHours)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(hours, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(hours, 168);
        return createdAt.AddHours(hours);
    }

    /// <summary>Solo una solicitud <c>enviada</c> o <c>cotizando</c> recibe cotizaciones nuevas.</summary>
    public static bool AcceptsQuotes(RequestState state) => state is RequestState.Enviada or RequestState.Cotizando;

    /// <summary>Estado de la solicitud tras recibir una cotización: la primera la pasa de <c>enviada</c> a <c>cotizando</c> (R-16).</summary>
    public static RequestState AfterFirstQuote(RequestState state) => AcceptsQuotes(state)
        ? RequestState.Cotizando
        : throw new DomainException(DomainErrorKind.Conflict, "The request cannot receive a quote.");

    public static bool IsLive(QuoteState state) => state is QuoteState.Enviada or QuoteState.Aceptada;

    public static bool CanAddQuote(int liveQuotes) => liveQuotes < MaxLiveQuotes;
}
