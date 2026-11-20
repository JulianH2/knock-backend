using Knock.Identity.Api.Modules.Panel.Data;
using Knock.Identity.Api.Modules.Panel.Domain;
using Knock.Identity.Api.Modules.Sesiones.Data;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Knock.UnitTest.Panel;

/// <summary>Reloj de pruebas que se mueve a mano.</summary>
internal sealed class PanelClock(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}

internal sealed class PanelEnvironment(string name) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = name;

    public string ApplicationName { get; set; } = "Knock.Identity.Api";

    public string ContentRootPath { get; set; } = "/";

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

/// <summary>Doble en memoria de <c>panel_cuentas</c> con la misma semántica de las actualizaciones condicionadas del almacén real.</summary>
internal sealed class FakePanelCuentas : IPanelCuentasStore
{
    private readonly Lock _gate = new();

    public Dictionary<string, CuentaPanel> Accounts { get; } = [];

    public bool Unavailable { get; set; }

    public int Reads { get; private set; }

    public void Seed(CuentaPanel account) => Accounts[account.Sub] = account;

    public Task<CuentaPanel?> FindBySubAsync(string sub, CancellationToken cancellationToken)
    {
        CheckAvailable();
        lock (_gate)
        {
            Reads++;
            return Task.FromResult(Accounts.GetValueOrDefault(sub));
        }
    }

    public Task<bool> InsertAsync(NuevaCuentaPanel account, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (Accounts.ContainsKey(account.Sub))
            {
                return Task.FromResult(false);
            }

            var configured = account.TotpSecretoCifrado is { Length: > 0 };
            Accounts[account.Sub] = new CuentaPanel(
                Guid.NewGuid().ToString("N")[..24], account.Sub, account.Nombre, account.Roles, PanelCuentaEstado.Activa, 1, account.TotpSecretoCifrado,
                account.TotpVersionLlave, 0, configured ? PanelTotpEstado.Configurado : PanelTotpEstado.Pendiente, 0, null, null);
            return Task.FromResult(true);
        }
    }

    public Task<CuentaPanel?> SetRolesAsync(string sub, IReadOnlyList<string> roles, DateTime now, CancellationToken cancellationToken) =>
        Change(sub, account => account with { Roles = roles });

    public Task<CuentaPanel?> SetEstadoAsync(string sub, string estado, DateTime now, CancellationToken cancellationToken) =>
        Change(sub, account => account with { Estado = estado });

    public Task<CuentaPanel?> ResetTotpAsync(string sub, DateTime now, CancellationToken cancellationToken) =>
        Change(sub, account => account with
        {
            TotpEstado = PanelTotpEstado.Pendiente,
            TotpSecretoCifrado = null,
            TotpVersionLlave = 0,
            TotpUltimoPaso = 0,
            IntentosFallidos = 0,
            VentanaFallosInicia = null,
            BloqueadoHasta = null,
        });

    public Task<bool> TryAcceptStepAsync(string sub, long step, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (!Accounts.TryGetValue(sub, out var account) || account.TotpUltimoPaso >= step)
            {
                return Task.FromResult(false);
            }

            Accounts[sub] = account with { TotpUltimoPaso = step };
            return Task.FromResult(true);
        }
    }

    public Task<CuentaPanel?> RegisterFailureAsync(string sub, DateTime now, CancellationToken cancellationToken)
    {
        CheckAvailable();
        lock (_gate)
        {
            if (!Accounts.TryGetValue(sub, out var account))
            {
                return Task.FromResult<CuentaPanel?>(null);
            }

            if (PanelSesionReglas.FailureWindowEnded(account.VentanaFallosInicia, now))
            {
                account = account with { VentanaFallosInicia = now, IntentosFallidos = 0, BloqueadoHasta = null };
            }

            account = account with { IntentosFallidos = account.IntentosFallidos + 1 };
            if (account.IntentosFallidos >= PanelSesionReglas.MaxAccountFailures && !account.EstaBloqueada(now))
            {
                account = account with { BloqueadoHasta = PanelSesionReglas.LockUntil(account.VentanaFallosInicia!.Value) };
            }

            Accounts[sub] = account;
            return Task.FromResult<CuentaPanel?>(account);
        }
    }

    public Task ResetFailuresAsync(string sub, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (Accounts.TryGetValue(sub, out var account))
            {
                Accounts[sub] = account with { IntentosFallidos = 0, VentanaFallosInicia = null, BloqueadoHasta = null };
            }
        }

        return Task.CompletedTask;
    }

    private Task<CuentaPanel?> Change(string sub, Func<CuentaPanel, CuentaPanel> change)
    {
        lock (_gate)
        {
            if (!Accounts.TryGetValue(sub, out var account))
            {
                return Task.FromResult<CuentaPanel?>(null);
            }

            account = change(account) with { PermisosVersion = account.PermisosVersion + 1 };
            Accounts[sub] = account;
            return Task.FromResult<CuentaPanel?>(account);
        }
    }

    private void CheckAvailable()
    {
        if (Unavailable)
        {
            throw new TimeoutException("mongodb://usuario:clave@host");
        }
    }
}

/// <summary>Doble en memoria de <c>panel_sesiones</c>.</summary>
internal sealed class FakePanelSesiones : IPanelSesionesStore
{
    private readonly Lock _gate = new();

    public List<SesionPanel> Sessions { get; } = [];

    public bool Unavailable { get; set; }

    public Task<IReadOnlyList<string>> CreateAsync(NuevaSesionPanel session, CancellationToken cancellationToken)
    {
        CheckAvailable();
        lock (_gate)
        {
            Sessions.Add(new SesionPanel(
                session.Id, session.Sub, session.HuellaRenovacion, [], session.CreadaEn, session.PermisosVersion, session.CreadaEn, session.CreadaEn,
                session.RenovacionVenceEn, session.VenceEn, false));
            var open = Sessions.Where(s => s.Sub == session.Sub && !s.Revocada && s.VenceEn > session.CreadaEn).OrderBy(s => s.CreadaEn).ToList();
            var revoke = open.Take(Math.Max(0, open.Count - PanelSesionReglas.MaxOpenSessions)).Select(s => s.Id).ToList();
            foreach (var id in revoke)
            {
                Replace(id, s => s with { Revocada = true });
            }

            return Task.FromResult<IReadOnlyList<string>>(revoke);
        }
    }

    public Task<SesionPanel?> FindByFingerprintAsync(string fingerprint, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(Sessions.FirstOrDefault(s => s.HuellaRenovacion == fingerprint));
        }
    }

    public Task<SesionPanel?> FindByUsedFingerprintAsync(string fingerprint, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(Sessions.FirstOrDefault(s => s.HuellasUsadas.Contains(fingerprint)));
        }
    }

    public Task<SesionPanel?> FindByIdAsync(string sid, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(Sessions.FirstOrDefault(s => s.Id == sid));
        }
    }

    public Task<bool> RotateAsync(string sid, string currentFingerprint, string newFingerprint, DateTime now, DateTime renewalExpiresAt, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var session = Sessions.FirstOrDefault(s => s.Id == sid);
            if (session is null || session.HuellaRenovacion != currentFingerprint || session.Revocada || session.VenceEn <= now || session.RenovacionVenceEn <= now)
            {
                return Task.FromResult(false);
            }

            Replace(
                sid,
                s => s with
                {
                    HuellaRenovacion = newFingerprint,
                    HuellasUsadas = [.. s.HuellasUsadas.Append(currentFingerprint).TakeLast(PanelSesionReglas.MaxUsedFingerprints)],
                    RotadaEn = now,
                    UltimaActividad = now,
                    RenovacionVenceEn = renewalExpiresAt,
                });
            return Task.FromResult(true);
        }
    }

    public Task<IReadOnlyList<string>> RevokeAllAsync(string sub, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var ids = Sessions.Where(s => s.Sub == sub && !s.Revocada).Select(s => s.Id).ToList();
            foreach (var id in ids)
            {
                Replace(id, s => s with { Revocada = true });
            }

            return Task.FromResult<IReadOnlyList<string>>(ids);
        }
    }

    public Task<bool> RevokeAsync(string sid, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var session = Sessions.FirstOrDefault(s => s.Id == sid);
            if (session is null || session.Revocada)
            {
                return Task.FromResult(false);
            }

            Replace(sid, s => s with { Revocada = true });
            return Task.FromResult(true);
        }
    }

    private void Replace(string id, Func<SesionPanel, SesionPanel> change)
    {
        var index = Sessions.FindIndex(s => s.Id == id);
        Sessions[index] = change(Sessions[index]);
    }

    private void CheckAvailable()
    {
        if (Unavailable)
        {
            throw new TimeoutException("mongodb://usuario:clave@host");
        }
    }
}

/// <summary>Doble en memoria de <c>panel_activaciones</c>; el consumo cambia la cuenta del doble de cuentas como lo hace la transacción real.</summary>
internal sealed class FakePanelActivaciones(FakePanelCuentas accounts) : IPanelActivacionesStore
{
    private readonly Lock _gate = new();

    public List<(string AccountId, byte[] Fingerprint, DateTime ExpiresAt, bool Consumed)> Invitations { get; } = [];

    public Task CreateAsync(string accountId, byte[] fingerprint, DateTime now, DateTime expiresAt, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            Invitations.RemoveAll(i => i.AccountId == accountId);
            Invitations.Add((accountId, fingerprint, expiresAt, false));
        }

        return Task.CompletedTask;
    }

    public Task<InvitacionVigente?> FindLiveAsync(byte[] fingerprint, DateTime now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var invitation = Invitations.FirstOrDefault(i => i.Fingerprint.AsSpan().SequenceEqual(fingerprint) && !i.Consumed && i.ExpiresAt > now);
            var account = invitation.AccountId is null ? null : accounts.Accounts.Values.FirstOrDefault(a => a.Id == invitation.AccountId && a.TotpEstado == PanelTotpEstado.Pendiente);
            return Task.FromResult(account is null ? null : new InvitacionVigente(account.Id, account.Sub));
        }
    }

    public Task<ActivacionResultado> ConsumeAsync(byte[] fingerprint, string accountId, ProtectedSecretForStore secret, DateTime now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var index = Invitations.FindIndex(i => i.Fingerprint.AsSpan().SequenceEqual(fingerprint) && i.AccountId == accountId && !i.Consumed && i.ExpiresAt > now);
            var account = accounts.Accounts.Values.FirstOrDefault(a => a.Id == accountId && a.TotpEstado == PanelTotpEstado.Pendiente);
            if (index < 0 || account is null)
            {
                return Task.FromResult(ActivacionResultado.Invalida);
            }

            Invitations[index] = Invitations[index] with { Consumed = true };
            accounts.Accounts[account.Sub] = account with
            {
                TotpEstado = PanelTotpEstado.Configurado,
                TotpSecretoCifrado = secret.Ciphertext,
                TotpVersionLlave = secret.KeyVersion,
                TotpUltimoPaso = 0,
                PermisosVersion = account.PermisosVersion + 1,
            };
            return Task.FromResult(ActivacionResultado.Consumida);
        }
    }
}

internal sealed class FakePanelAuditoria : IPanelAuditoriaStore
{
    public List<PanelAuditEntry> Entries { get; } = [];

    public bool Fails { get; set; }

    public Task InsertAsync(PanelAuditEntry entry, CancellationToken cancellationToken)
    {
        if (Fails)
        {
            throw new TimeoutException("mongodb://usuario:clave@host");
        }

        lock (Entries)
        {
            Entries.Add(entry);
        }

        return Task.CompletedTask;
    }
}

/// <summary>Desafíos y fallos por IP en memoria; el consumo es atómico como el script de Redis.</summary>
internal sealed class FakePanelDesafios : IPanelDesafios
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, (string Sub, DateTime? Expires)> _challenges = [];
    private readonly Dictionary<string, long> _ipFailures = [];

    public bool Unavailable { get; set; }

    public int Created => _challenges.Count;

    public Task CreateAsync(string challengeId, string sub, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        CheckAvailable();
        lock (_gate)
        {
            _challenges[challengeId] = (sub, null);
        }

        return Task.CompletedTask;
    }

    public Task<string?> PeekAsync(string challengeId, CancellationToken cancellationToken)
    {
        CheckAvailable();
        lock (_gate)
        {
            return Task.FromResult(_challenges.TryGetValue(challengeId, out var value) ? value.Sub : null);
        }
    }

    public Task<string?> ConsumeAsync(string challengeId, CancellationToken cancellationToken)
    {
        CheckAvailable();
        lock (_gate)
        {
            if (_challenges.Remove(challengeId, out var value))
            {
                return Task.FromResult<string?>(value.Sub);
            }

            return Task.FromResult<string?>(null);
        }
    }

    public Task<long> IpFailuresAsync(string ip, CancellationToken cancellationToken)
    {
        CheckAvailable();
        lock (_gate)
        {
            return Task.FromResult(_ipFailures.GetValueOrDefault(ip));
        }
    }

    public Task RegisterIpFailureAsync(string ip, TimeSpan window, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _ipFailures[ip] = _ipFailures.GetValueOrDefault(ip) + 1;
        }

        return Task.CompletedTask;
    }

    /// <summary>Simula que el desafío venció en Redis (el TTL lo borra).</summary>
    public void Expire(string challengeId)
    {
        lock (_gate)
        {
            _challenges.Remove(challengeId);
        }
    }

    public void SetIpFailures(string ip, long count)
    {
        lock (_gate)
        {
            _ipFailures[ip] = count;
        }
    }

    private void CheckAvailable()
    {
        if (Unavailable)
        {
            throw new TimeoutException("redis://clave@host");
        }
    }
}

internal sealed class FakePanelCache : IPanelCuentaCache
{
    public Dictionary<string, CuentaPanelEnCache> Entries { get; } = [];

    public bool Unavailable { get; set; }

    public int Deletes { get; private set; }

    public Task<CuentaPanelEnCache?> GetAsync(string sub, CancellationToken cancellationToken)
    {
        CheckAvailable();
        return Task.FromResult(Entries.GetValueOrDefault(sub));
    }

    public Task SetAsync(string sub, CuentaPanelEnCache value, CancellationToken cancellationToken)
    {
        CheckAvailable();
        Entries[sub] = value;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string sub, CancellationToken cancellationToken)
    {
        CheckAvailable();
        Entries.Remove(sub);
        Deletes++;
        return Task.CompletedTask;
    }

    private void CheckAvailable()
    {
        if (Unavailable)
        {
            throw new TimeoutException("redis://clave@host");
        }
    }
}

internal sealed class FakePanelCancelled : IPanelSidCanceller
{
    public List<(string SessionId, TimeSpan Lifetime)> Entries { get; } = [];

    public Task CancelAsync(string sessionId, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        Entries.Add((sessionId, lifetime));
        return Task.CompletedTask;
    }

    public Task<bool> IsCancelledAsync(string sessionId, CancellationToken cancellationToken) => Task.FromResult(Entries.Exists(entry => entry.SessionId == sessionId));
}
