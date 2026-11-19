namespace Knock.Identity.Api.Common.Errors;

/// <summary>Códigos de error de dominio que usan varios módulos: viven aquí para que un módulo no dependa de los servicios de otro.</summary>
internal static class CommonErrorCodes
{
    /// <summary>Mercado no respondió; sin poder validar la colonia no se guarda nada.</summary>
    public const string MarketUnavailable = "market_unavailable";
}
