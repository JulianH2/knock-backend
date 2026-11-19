extern alias IdentityApi;

using System.Collections.Concurrent;
using IdentityApi::Knock.Identity.Api.Common.Errors;
using IdentityApi::Knock.Identity.Api.Modules.Account.Domain;
using IdentityApi::Knock.Identity.Api.Modules.Account.Services;

namespace Knock.SecurityTest.Support;

/// <summary>
/// Doble de la consulta interna a Mercado (spec 039, R-24 y R-25): las rutas internas con el token de la spec 037 todavía no existen, así que el host de
/// pruebas de Identidad usa este doble. Cada <c>sub</c> tiene sus pendientes y se anotan las cancelaciones pedidas. Solo datos ficticios.
/// </summary>
internal sealed class FakeMarketAccountClient : IMarketAccountClient
{
    private readonly ConcurrentDictionary<string, AccountPending> _pending = new(StringComparer.Ordinal);

    /// <summary>Los <c>sub</c> para los que se pidió cancelar lo abierto, en orden.</summary>
    public ConcurrentQueue<string> CancelRequests { get; } = new();

    /// <summary>Si es verdadero, Mercado no responde: las dos operaciones fallan con 503.</summary>
    public bool Down { get; set; }

    public void Set(string sub, AccountPending pending) => _pending[sub] = pending;

    public Task<AccountPending> GetPendingAsync(string sub, CancellationToken cancellationToken)
    {
        EnsureUp();
        return Task.FromResult(_pending.GetValueOrDefault(sub) ?? new AccountPending(0, [], 0, [], false));
    }

    public Task CancelOpenAsync(string sub, CancellationToken cancellationToken)
    {
        EnsureUp();
        CancelRequests.Enqueue(sub);
        _pending[sub] = new AccountPending(0, [], 0, [], false);
        return Task.CompletedTask;
    }

    private void EnsureUp()
    {
        if (Down)
        {
            throw new DomainException(DomainErrorKind.ServiceUnavailable, "Market is down.", AccountErrorCodes.MarketUnavailable);
        }
    }
}
