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

/// <summary>Todos los dobles del panel y los servicios armados sobre ellos, con datos ficticios.</summary>
internal sealed class PanelWorld
{
    public const string Origin = "https://panel.example";
    public const string Phone = "+525555550211";
    public const string OtherPhone = "+525555550212";
    public const string AppSub = "11111111-1111-8111-8111-111111111111";
    public const string OtherAppSub = "22222222-2222-8222-8222-222222222222";

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

    public PanelCli Cli(FakePanelConsole console) => new(Admin, Settings, Environment, console);

    /// <summary>Crea una cuenta del panel ya lista para entrar: TOTP configurado con el secreto dado.</summary>
    public CuentaPanel SeedAccount(string sub = AppSub, string[]? roles = null, byte[]? secret = null)
    {
        var protectedSecret = Totp.Protect(secret ?? TotpService.GenerateSecret(), sub);
        var account = new CuentaPanel(
            Guid.NewGuid().ToString("N")[..24], sub, "Ana de prueba", roles ?? [PanelRoles.Administrador], PanelCuentaEstado.Activa, 1,
            protectedSecret.Ciphertext, protectedSecret.KeyVersion, 0, PanelTotpEstado.Configurado, 0, null, null);
        Accounts.Seed(account);
        return account;
    }
}
