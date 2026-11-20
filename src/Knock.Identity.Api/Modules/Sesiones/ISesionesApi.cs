namespace Knock.Identity.Api.Modules.Sesiones;

/// <summary>Una cuenta de la app tal como la ven los demás módulos: su <c>sub</c> y si está suspendida, nada más (spec 033, R-06).</summary>
public sealed record AppAccount(string Sub, bool IsSuspended);

/// <summary>
/// Lo que el módulo de sesiones ofrece a los demás módulos de Identidad sin que lean su colección ni su dominio (spec 002): buscar una cuenta de la app
/// por su <c>sub</c> o por su celular. El celular solo se usa para buscar por su HMAC y nunca se devuelve.
/// </summary>
public interface ISesionesApi
{
    /// <summary>La cuenta de la app de ese <c>sub</c> (<paramref name="subject"/>), o <see langword="null"/> si no existe.</summary>
    Task<AppAccount?> FindAccountBySubAsync(string subject, CancellationToken cancellationToken);

    /// <summary>El <c>sub</c> de la cuenta de la app con ese celular E.164 (+52, ya validado), o <see langword="null"/> si no existe.</summary>
    Task<string?> FindSubByPhoneAsync(string phone, CancellationToken cancellationToken);
}
