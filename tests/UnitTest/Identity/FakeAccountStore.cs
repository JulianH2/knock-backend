using Knock.Identity.Api.Modules.Account.Data;
using Knock.Identity.Api.Modules.Account.Domain;

namespace Knock.UnitTest.Identity;

/// <summary>Doble en memoria de <see cref="IAccountStore"/> (spec 039): guarda lo que el servicio escribe y deja simular carreras.</summary>
internal sealed class FakeAccountStore : IAccountStore
{
    public List<AccountSnapshot> Accounts { get; } = [];

    public Task<AccountSnapshot?> FindBySubAsync(string sub, CancellationToken cancellationToken) =>
        Task.FromResult(Accounts.FirstOrDefault(a => a.Sub == sub));

    public Task<bool> PhoneHashExistsAsync(byte[] phoneHash, CancellationToken cancellationToken) =>
        Task.FromResult(Accounts.Any(a => a.PhoneHash.AsSpan().SequenceEqual(phoneHash)));
}
