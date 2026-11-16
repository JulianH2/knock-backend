using Knock.Market.Api.Modules.Catalog.Dtos;

namespace Knock.Market.Api.Modules.Catalog.Services;

internal sealed record WaitlistEntry(string Id, string Cp, string? Municipio, DateTime CreadoEn);

internal sealed record WaitlistResult(WaitlistEntry Entry, bool AlreadyRegistered);

internal interface IWaitlistStore
{
    Task<WaitlistResult> JoinAsync(string user, WaitlistRequest request, CancellationToken cancellationToken);
}

internal sealed class WaitlistService(IWaitlistStore store)
{
    public Task<WaitlistResult> JoinAsync(string user, WaitlistRequest request, CancellationToken cancellationToken) =>
        store.JoinAsync(user, request, cancellationToken);
}
