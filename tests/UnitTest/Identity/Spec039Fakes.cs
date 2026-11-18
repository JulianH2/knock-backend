using Knock.Identity.Api.Common.Errors;

namespace Knock.UnitTest.Identity;

/// <summary>Reloj fijo de las pruebas de la spec 039.</summary>
internal sealed class Spec039Clock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

/// <summary>Verificador de Firebase doblado: devuelve la identidad indicada o <c>null</c> (token rechazado).</summary>
internal sealed class Spec039Verifier(FirebaseIdentity? identity) : IFirebaseIdTokenVerifier
{
    public FirebaseIdentity? Identity { get; set; } = identity;

    public int Calls { get; private set; }

    public Task<FirebaseIdentity?> VerifyAsync(string idToken, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(Identity);
    }
}

/// <summary>Lista de sesiones canceladas doblada: guarda los ids y la vida que se les pidió.</summary>
internal sealed class Spec039Cancelled : ISessionCanceller
{
    public List<(string SessionId, TimeSpan Lifetime)> Cancelled { get; } = [];

    public Task CancelAsync(string sessionId, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        Cancelled.Add((sessionId, lifetime));
        return Task.CompletedTask;
    }
}

/// <summary>Buzón de salida doblado: guarda en orden lo que se le pidió, para comprobar que el aviso va antes del cierre.</summary>
internal sealed class Spec039Outbox : IAccountOutbox
{
    public List<string> RevokedNotices { get; } = [];

    public List<string> ExportTasks { get; } = [];

    /// <summary>Si es verdadero, programar la exportación falla (la tarea no se pudo encolar).</summary>
    public bool FailExport { get; set; }

    public Task EnqueueExportAsync(string userId, CancellationToken cancellationToken)
    {
        if (FailExport)
        {
            throw new InvalidOperationException("outbox down");
        }

        ExportTasks.Add(userId);
        return Task.CompletedTask;
    }

    public Task EnqueueSessionRevokedAsync(string sessionId, CancellationToken cancellationToken)
    {
        RevokedNotices.Add(sessionId);
        return Task.CompletedTask;
    }
}

/// <summary>Mercado doblado: devuelve los pendientes indicados y anota las cancelaciones.</summary>
internal sealed class Spec039Market : IMarketAccountClient
{
    public AccountPending Pending { get; set; } = new(0, [], 0, [], false);

    public List<string> Cancelled { get; } = [];

    public bool Down { get; set; }

    public Task<AccountPending> GetPendingAsync(string sub, CancellationToken cancellationToken)
    {
        EnsureUp();
        return Task.FromResult(Pending);
    }

    public Task CancelOpenAsync(string sub, CancellationToken cancellationToken)
    {
        EnsureUp();
        Cancelled.Add(sub);
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
