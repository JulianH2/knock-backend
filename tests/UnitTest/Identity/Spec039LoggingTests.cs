using AwesomeAssertions;
using Knock.Identity.Api.Common.Errors;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Knock.UnitTest.Identity;

/// <summary>Spec 039, R-26 (CA-39): ninguna de las rutas escribe celulares, tokens ni motivos en los registros. Sin MongoDB.</summary>
public class Spec039LoggingTests
{
    private const string _old = "+525512345678";
    private const string _new = "+525598765432";
    private const string _token = "synthetic-secret-token";

    internal sealed class Capture<T> : ILogger<T>
    {
        public List<string> Lines { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Lines.Add(formatter(state, exception) + exception);
    }

    [Fact]
    public async Task PhoneChangeAndRecovery_NeverLogPhonesOrTokens_CA_039_39()
    {
        var phones = new PhoneProtector(PhoneProtectionTestKeys.Keys());
        var store = new FakeAccountStore();
        store.Accounts.Add(new AccountSnapshot("665f1b2c3d4e5f6071829304", "f0000000-0000-8000-8000-000000000001", AccountState.Active, false, phones.Hash(_old).Value, null));
        store.Sessions.Add(new FakeAccountSession("665f1b2c3d4e5f6071829a01", "665f1b2c3d4e5f6071829304"));
        var clock = new Spec039Clock(new DateTimeOffset(2026, 11, 17, 12, 0, 0, TimeSpan.Zero));
        var verifier = new Spec039Verifier(new FirebaseIdentity("uid-1", _new));
        var changeLog = new Capture<PhoneChangeService>();
        var recoveryLog = new Capture<AccessRecoveryService>();
        var change = new PhoneChangeService(store, verifier, phones, new Spec039Cancelled(), new Spec039Outbox(), clock, changeLog);
        using var limiter = new RecoveryAttemptLimiter();
        var recovery = new AccessRecoveryService(store, verifier, phones, limiter, clock, recoveryLog);

        var taken = () => change.EnsureAvailableAsync(_old, CancellationToken.None);
        await taken.Should().ThrowAsync<DomainException>();
        await change.ConfirmAsync("f0000000-0000-8000-8000-000000000001", "665f1b2c3d4e5f6071829a01", _token, CancellationToken.None);
        verifier.Identity = null;
        await change.ConfirmAsync("f0000000-0000-8000-8000-000000000001", "665f1b2c3d4e5f6071829a01", _token, CancellationToken.None);
        await recovery.RequestAsync(new AccessRecoveryInput(_token, _old, "cliente"), CancellationToken.None);

        var all = string.Join('\n', changeLog.Lines.Concat(recoveryLog.Lines));
        all.Should().NotBeEmpty();
        all.Should().NotContain("5512345678").And.NotContain("5598765432").And.NotContain(_token);
    }
}
