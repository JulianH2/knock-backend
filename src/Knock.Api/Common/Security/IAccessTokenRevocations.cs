namespace Knock.Api.Common.Security;

/// <summary>
/// Lista de access tokens revocados por su <c>jti</c> (constitución, principio 13; spec HU-004). Es <b>opcional</b>: sin una implementación real
/// (Redis, cuyo paquete aún no está aprobado) <see cref="Enabled"/> es falso y el access token vale hasta vencer (15 min, riesgo aceptado en §12).
/// </summary>
internal interface IAccessTokenRevocations
{
    bool Enabled { get; }

    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken);

    /// <summary>Guarda el <c>jti</c> como revocado durante lo que le queda de vida al token.</summary>
    Task RevokeAsync(string jti, TimeSpan timeToLive, CancellationToken cancellationToken);
}

/// <summary>Valor por defecto cuando no hay lista de revocados: nada se revoca ni se consulta.</summary>
internal sealed class NoAccessTokenRevocations : IAccessTokenRevocations
{
    public bool Enabled => false;

    public Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken) => Task.FromResult(false);

    public Task RevokeAsync(string jti, TimeSpan timeToLive, CancellationToken cancellationToken) => Task.CompletedTask;
}
