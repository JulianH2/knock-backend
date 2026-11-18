using Knock.Identity.Api.Modules.Account.Data;
using Knock.Identity.Api.Modules.Account.Domain;
using Knock.Identity.Api.Modules.Sesiones.Services;

namespace Knock.UnitTest.Identity;

/// <summary>Una sesión del doble: dueño, si sigue abierta.</summary>
internal sealed class FakeAccountSession(string id, string userId)
{
    public string Id { get; } = id;

    public string UserId { get; } = userId;

    public bool Revoked { get; set; }
}

/// <summary>Doble en memoria de <see cref="IAccountStore"/> (spec 039): guarda lo que el servicio escribe y deja simular carreras.</summary>
internal sealed class FakeAccountStore : IAccountStore
{
    public List<AccountSnapshot> Accounts { get; } = [];

    public List<FakeAccountSession> Sessions { get; } = [];

    /// <summary>Si es verdadero, el cambio de celular lo rechaza el índice único (otra cuenta ganó la carrera).</summary>
    public bool PhoneTakenOnChange { get; set; }

    /// <summary>Celulares guardados por <c>_id</c> de usuario: el último cifrado y su hash.</summary>
    public Dictionary<string, (ProtectedPhone Phone, PhoneHash Hash)> StoredPhones { get; } = [];

    public Task<AccountSnapshot?> FindBySubAsync(string sub, CancellationToken cancellationToken) =>
        Task.FromResult(Accounts.FirstOrDefault(a => a.Sub == sub));

    public Task<bool> PhoneHashExistsAsync(byte[] phoneHash, CancellationToken cancellationToken) =>
        Task.FromResult(Accounts.Any(a => a.PhoneHash.AsSpan().SequenceEqual(phoneHash)));

    public Task<IReadOnlyList<string>> FindOtherOpenSessionIdsAsync(string userId, string currentSessionId, DateTime now, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>([.. Sessions.Where(s => s.UserId == userId && !s.Revoked && s.Id != currentSessionId).Select(s => s.Id)]);

    public Task<(PhoneChangeOutcome Outcome, IReadOnlyList<string> RevokedSessionIds)> ChangePhoneAsync(
        string userId, ProtectedPhone phone, PhoneHash hash, string currentSessionId, DateTime now, CancellationToken cancellationToken)
    {
        if (PhoneTakenOnChange)
        {
            return Task.FromResult<(PhoneChangeOutcome, IReadOnlyList<string>)>((PhoneChangeOutcome.PhoneTaken, []));
        }

        var index = Accounts.FindIndex(a => a.Id == userId);
        Accounts[index] = Accounts[index] with { PhoneHash = hash.Value };
        StoredPhones[userId] = (phone, hash);
        var revoked = Sessions.Where(s => s.UserId == userId && !s.Revoked && s.Id != currentSessionId).ToList();
        revoked.ForEach(s => s.Revoked = true);
        return Task.FromResult<(PhoneChangeOutcome, IReadOnlyList<string>)>((PhoneChangeOutcome.Changed, [.. revoked.Select(s => s.Id)]));
    }

    /// <summary>Casos de recuperación guardados, en orden.</summary>
    public List<RecoveryCase> RecoveryCases { get; } = [];

    public Task<AccountSnapshot?> FindByPhoneHashAsync(byte[] phoneHash, CancellationToken cancellationToken) =>
        Task.FromResult(Accounts.FirstOrDefault(a => a.PhoneHash.AsSpan().SequenceEqual(phoneHash)));

    public Task<bool> CreateRecoveryCaseIfNoneOpenAsync(RecoveryCase recoveryCase, CancellationToken cancellationToken)
    {
        if (RecoveryCases.Any(c => c.UserId == recoveryCase.UserId))
        {
            return Task.FromResult(false);
        }

        RecoveryCases.Add(recoveryCase);
        return Task.FromResult(true);
    }
}
