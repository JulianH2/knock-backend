using System.Security.Cryptography;
using Knock.Identity.Api.Modules.Panel.Cli;
using Knock.Identity.Api.Modules.Panel.Data;
using Knock.Identity.Api.Modules.Panel.Domain;
using Knock.Identity.Api.Modules.Panel.Services;
using Knock.Identity.Api.Modules.Sesiones.Domain;
using Knock.Identity.Api.Modules.Sesiones.Services;
using Knock.UnitTest.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace Knock.UnitTest.Panel;

/// <summary>Consola de pruebas de la herramienta interna.</summary>
internal sealed class FakePanelConsole(bool interactive = true, bool redirected = false) : IPanelCliConsole
{
    private readonly System.Text.StringBuilder _out = new();
    private readonly System.Text.StringBuilder _error = new();

    public bool IsOutputRedirected => redirected;

    public bool IsInteractive => interactive;

    public TextWriter Out => new StringWriter(_out);

    public TextWriter Error => new StringWriter(_error);

    public string Output => _out.ToString();

    public string Errors => _error.ToString();
}

internal sealed class NoHttpContext : IHttpContextAccessor
{
    public HttpContext? HttpContext { get; set; }
}

/// <summary>Verificador de Firebase de pruebas: acepta solo los <c>idToken</c> registrados.</summary>
internal sealed class FakeFirebaseVerifier : IFirebaseIdTokenVerifier
{
    public Dictionary<string, FirebaseIdentity> Tokens { get; } = [];

    public bool Unavailable { get; set; }

    public Task<FirebaseIdentity?> VerifyAsync(string idToken, CancellationToken cancellationToken)
    {
        if (Unavailable)
        {
            throw new HttpRequestException("https://www.googleapis.com/ no responde");
        }

        return Task.FromResult(Tokens.GetValueOrDefault(idToken));
    }
}

/// <summary>Todos los dobles del panel y los servicios armados sobre ellos, con datos ficticios.</summary>
internal sealed class PanelWorld
{
    public const string Origin = "https://panel.example";
    public const string Phone = "+525555550211";
    public const string OtherPhone = "+525555550212";
    public const string Uid = "uid-ana";
    public const string OtherUid = "uid-beto";
    public static readonly string AppSub = UserIdDerivation.Derive(Uid).ToString("D");
    public static readonly string OtherAppSub = UserIdDerivation.Derive(OtherUid).ToString("D");

    public PanelWorld(string environment = "Development")
    {
        Clock = new PanelClock(new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero));
        Invitations = new FakePanelActivaciones(Accounts);
        Environment = new PanelEnvironment(environment);
        Phones = new PhoneProtector(PhoneProtectionTestKeys.Keys());
        var totpOptions = new PanelTotpOptions { KeyVersion = 1 };
        totpOptions.Keys[1] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        Totp = new TotpService(PanelTotpKeys.TryCreate(totpOptions, out _)!);
        Settings = new PanelSettings("knock-api", PanelSettings.DefaultAudience, new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)), Origin, true);
        AppAccounts.Users.Add(new UserAccount("665f1b2c3d4e5f6071829301", AppSub, AccountState.Active, true, Phones.Hash(Phone).Value));
        Audit = new AuditoriaPanel(AuditStore, new PanelIpHasher(PhoneProtectionTestKeys.Keys()), new NoHttpContext(), Clock, NullLogger<AuditoriaPanel>.Instance);
        Admin = new PanelAdminService(Accounts, Sessions, Invitations, Cache, AppAccounts, Cancelled, Phones, Totp, Audit, Clock);
        Issuer = new PanelTokenIssuer(Settings, Clock);
        SessionService = new SesionPanelService(Sessions, Cancelled, Issuer, Clock, NullLogger<SesionPanelService>.Instance);
        Access = new AccesoPanelService(Verifier, Accounts, AppAccounts, Challenges, Totp, SessionService, Audit, Settings, Clock);
        Activation = new ActivacionPanelService(Invitations, Accounts, Cache, Totp, Audit, Settings, Clock);
        Lifecycle = new SesionPanelCicloService(Sessions, Accounts, Cancelled, Issuer, Audit, Settings, Clock);
    }

    public PanelClock Clock { get; }

    public PanelEnvironment Environment { get; }

    public PhoneProtector Phones { get; }

    public TotpService Totp { get; }

    public PanelSettings Settings { get; }

    public FakeSesionesStore AppAccounts { get; } = new();

    public FakePanelCuentas Accounts { get; } = new();

    public FakePanelSesiones Sessions { get; } = new();

    public FakePanelActivaciones Invitations { get; }

    public FakePanelAuditoria AuditStore { get; } = new();

    public FakePanelDesafios Challenges { get; } = new();

    public FakePanelCache Cache { get; } = new();

    public FakePanelCancelled Cancelled { get; } = new();

    public AuditoriaPanel Audit { get; }

    public PanelAdminService Admin { get; }

    public FakeFirebaseVerifier Verifier { get; } = new();

    public PanelTokenIssuer Issuer { get; }

    public SesionPanelService SessionService { get; }

    public AccesoPanelService Access { get; }

    public ActivacionPanelService Activation { get; }

    public SesionPanelCicloService Lifecycle { get; }

    /// <summary>Registra un <c>idToken</c> ficticio que el verificador acepta para el <c>uid</c> y el celular dados.</summary>
    public string TokenFor(string uid = Uid, string phone = Phone)
    {
        var token = "idtoken-" + uid;
        Verifier.Tokens[token] = new FirebaseIdentity(uid, phone);
        return token;
    }

    /// <summary>Código TOTP vigente para el secreto, según el reloj de pruebas.</summary>
    public string CurrentCode(byte[] secret, int stepOffset = 0) => TotpService.CodeFor(secret, TotpService.StepOf(Clock.GetUtcNow()) + stepOffset);

    public PanelCli Cli(FakePanelConsole console) => new(Admin, Settings, Environment, console);

    /// <summary>Crea una cuenta del panel ya lista para entrar: TOTP configurado con el secreto dado.</summary>
    public CuentaPanel SeedAccount(string? sub = null, string[]? roles = null, byte[]? secret = null)
    {
        sub ??= AppSub;
        var protectedSecret = Totp.Protect(secret ?? TotpService.GenerateSecret(), sub);
        var account = new CuentaPanel(
            Guid.NewGuid().ToString("N")[..24], sub, "Ana de prueba", roles ?? [PanelRoles.Administrador], PanelCuentaEstado.Activa, 1,
            protectedSecret.Ciphertext, protectedSecret.KeyVersion, 0, PanelTotpEstado.Configurado, 0, null, null);
        Accounts.Seed(account);
        return account;
    }
}
