namespace Knock.Market.Api.Common.Errors;

/// <summary>
/// <c>code</c> estable de los errores de transporte que no vienen de una excepción de dominio (spec 002, R-08, R-09 y R-11): en inglés y en <c>snake_case</c>,
/// sin datos de la persona ni del recurso. Los escriben la autenticación, el límite de cuerpo, el filtro de validación y el limitador.
/// </summary>
internal static class ErrorCodes
{
    public const string UnauthorizedCode = "unauthorized";
    public const string PayloadTooLargeCode = "payload_too_large";
    public const string ValidationFailedCode = "validation_failed";
    public const string RateLimitedCode = "rate_limited";
}
