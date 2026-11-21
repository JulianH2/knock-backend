namespace Knock.Messaging.Api.Common.Errors;

/// <summary>
/// <c>code</c> estable de los errores de transporte de Mensajería que no vienen de una excepción de dominio: en inglés y en <c>snake_case</c>, sin datos de la persona ni del recurso.
/// Los escriben la autenticación, el filtro de validación y el limitador. Los de negocio de cada módulo se declaran en el propio módulo, una sola vez por servicio.
/// </summary>
internal static class ErrorCodes
{
    public const string UnauthorizedCode = "unauthorized";
    public const string ValidationFailedCode = "validation_failed";
    public const string RateLimitedCode = "rate_limited";

    /// <summary>Mercado no respondió y no hay permiso en caché (spec 030, R-09): nunca se asume que la conversación está abierta.</summary>
    public const string MarketUnavailableCode = "market_unavailable";
}
