using Knock.Identity.Api.Modules.Panel.Services;

namespace Knock.Identity.Api.Modules.Panel.Cli;

/// <summary>Consola de la herramienta interna: separada del <see cref="Console"/> para probarla y para decidir si se puede imprimir un enlace de activación.</summary>
internal interface IPanelCliConsole
{
    /// <summary>La salida estándar está redirigida a un archivo o a otro programa: un enlace de activación quedaría fuera del control de quien ejecuta.</summary>
    bool IsOutputRedirected { get; }

    /// <summary>Hay una terminal interactiva (una persona frente a ella).</summary>
    bool IsInteractive { get; }

    TextWriter Out { get; }

    TextWriter Error { get; }
}

/// <summary>Consola real del proceso.</summary>
internal sealed class SystemPanelCliConsole : IPanelCliConsole
{
    public bool IsOutputRedirected => Console.IsOutputRedirected;

    public bool IsInteractive => Environment.UserInteractive && !Console.IsInputRedirected;

    public TextWriter Out => Console.Out;

    public TextWriter Error => Console.Error;
}

/// <summary>
/// Comandos de la herramienta interna del panel (spec 033, R-25 y R-26): <c>panel alta</c>, <c>roles</c>, <c>deshabilitar</c>, <c>habilitar</c> y
/// <c>reiniciar-totp</c>. Corre sin levantar el servidor web. Imprime solo el enlace de activación (nunca el secreto ni la dirección <c>otpauth://</c>),
/// una sola vez y en la terminal de quien ejecuta; no escribe nada en los registros. Los mensajes de error no llevan datos de la persona.
/// Códigos de salida: 0 correcto, 1 error de datos o de dependencias, 2 uso incorrecto.
/// </summary>
internal sealed class PanelCli(PanelAdminService admin, PanelSettings settings, IHostEnvironment environment, IPanelCliConsole console)
{
    public const int ExitOk = 0;
    public const int ExitFailure = 1;
    public const int ExitUsage = 2;

    public const string ActivationPath = "/activacion";

    private static readonly Dictionary<PanelAdminError, string> _messages = new()
    {
        [PanelAdminError.InvalidPhone] = "El celular no tiene el formato +52 de 10 dígitos.",
        [PanelAdminError.AppAccountNotFound] = "No existe una cuenta de la app con ese celular.",
        [PanelAdminError.InvalidName] = "El nombre debe tener de 1 a 60 caracteres.",
        [PanelAdminError.InvalidRoles] = "Los roles deben ser de 1 a 5 de: administrador, verificador, moderador, catalogo, analista (sin repetir).",
        [PanelAdminError.AccountAlreadyExists] = "Esa persona ya tiene cuenta del panel.",
        [PanelAdminError.AccountNotFound] = "Esa persona no tiene cuenta del panel.",
        [PanelAdminError.TestSecretNotAllowed] = "--secreto-de-prueba solo se acepta en Development.",
        [PanelAdminError.InvalidTestSecret] = "El secreto de prueba no es un Base32 válido de al menos 80 bits.",
    };

    public async Task<int> RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Count == 0 || !TryParseOptions(args.Skip(1), out var options))
        {
            return Usage();
        }

        return args[0] switch
        {
            "alta" => await AltaAsync(options, cancellationToken),
            "roles" => await RolesAsync(options, cancellationToken),
            "deshabilitar" => await SimpleAsync(options, admin.DisableAsync, cancellationToken),
            "habilitar" => await SimpleAsync(options, admin.EnableAsync, cancellationToken),
            "reiniciar-totp" => await ResetTotpAsync(options, cancellationToken),
            _ => Usage(),
        };
    }

    private async Task<int> AltaAsync(Dictionary<string, string> options, CancellationToken cancellationToken)
    {
        if (!options.TryGetValue("celular", out var phone) || !options.TryGetValue("nombre", out var name) || !options.TryGetValue("roles", out var roles))
        {
            return Usage();
        }

        options.TryGetValue("secreto-de-prueba", out var testSecret);
        // El enlace se imprime una sola vez y solo ante una persona: sin terminal interactiva o con la salida redirigida no se crea nada.
        if (testSecret is null && CannotPrintLink() is { } refusal)
        {
            return refusal;
        }

        var outcome = await admin.AltaAsync(
            new PanelAltaRequest(phone, name, SplitRoles(roles), testSecret, environment.IsDevelopment()),
            cancellationToken);
        return outcome.Succeeded ? PrintResult(outcome, "Cuenta de prueba creada.") : Fail(outcome);
    }

    private async Task<int> RolesAsync(Dictionary<string, string> options, CancellationToken cancellationToken)
    {
        if (!options.TryGetValue("celular", out var phone) || !options.TryGetValue("roles", out var roles))
        {
            return Usage();
        }

        var outcome = await admin.SetRolesAsync(phone, SplitRoles(roles), cancellationToken);
        return outcome.Succeeded ? PrintResult(outcome, "Roles cambiados. Las sesiones abiertas de la cuenta quedaron cerradas.") : Fail(outcome);
    }

    private async Task<int> SimpleAsync(
        Dictionary<string, string> options,
        Func<string, CancellationToken, Task<PanelAdminOutcome>> command,
        CancellationToken cancellationToken)
    {
        if (!options.TryGetValue("celular", out var phone))
        {
            return Usage();
        }

        var outcome = await command(phone, cancellationToken);
        return outcome.Succeeded ? PrintResult(outcome, "Listo. Las sesiones abiertas de la cuenta quedaron cerradas.") : Fail(outcome);
    }

    private async Task<int> ResetTotpAsync(Dictionary<string, string> options, CancellationToken cancellationToken)
    {
        if (!options.TryGetValue("celular", out var phone))
        {
            return Usage();
        }

        if (CannotPrintLink() is { } refusal)
        {
            return refusal;
        }

        var outcome = await admin.ResetTotpAsync(phone, cancellationToken);
        return outcome.Succeeded ? PrintResult(outcome, "TOTP reiniciado.") : Fail(outcome);
    }

    /// <summary>Rechaza el comando antes de tocar nada si no se puede imprimir el enlace con seguridad (R-25) o si falta el origen del panel.</summary>
    private int? CannotPrintLink()
    {
        if (console.IsOutputRedirected || !console.IsInteractive)
        {
            console.Error.WriteLine("Error: este comando imprime un enlace de un solo uso y necesita una terminal interactiva con la salida sin redirigir. No se creó nada.");
            return ExitFailure;
        }

        if (settings.Origin is null)
        {
            console.Error.WriteLine($"Error: falta {PanelSettings.OriginKey} (origen https del panel) para armar el enlace. No se creó nada.");
            return ExitFailure;
        }

        return null;
    }

    private int PrintResult(PanelAdminOutcome outcome, string success)
    {
        if (outcome.ActivationToken is null)
        {
            console.Out.WriteLine(success);
            return ExitOk;
        }

        // Solo el enlace: el token viaja en el fragmento (#), que el navegador no envía al servidor ni en Referer.
        console.Out.WriteLine($"{success} Enlace de activación (de un solo uso, vence en {(int)PanelSesionReglasMinutes()} minutos):");
        console.Out.WriteLine($"{settings.Origin}{ActivationPath}#token={outcome.ActivationToken}");
        return ExitOk;
    }

    private int Fail(PanelAdminOutcome outcome)
    {
        console.Error.WriteLine("Error: " + _messages[outcome.Error!.Value] + " No se cambió nada.");
        return ExitFailure;
    }

    private int Usage()
    {
        console.Error.WriteLine("Uso:");
        console.Error.WriteLine("  panel alta --celular +52… --nombre \"Nombre\" --roles rol[,rol] [--secreto-de-prueba BASE32]  (el secreto de prueba solo en Development)");
        console.Error.WriteLine("  panel roles --celular +52… --roles rol[,rol]");
        console.Error.WriteLine("  panel deshabilitar --celular +52…");
        console.Error.WriteLine("  panel habilitar --celular +52…");
        console.Error.WriteLine("  panel reiniciar-totp --celular +52…");
        return ExitUsage;
    }

    private static double PanelSesionReglasMinutes() => Domain.PanelSesionReglas.ActivationLifetime.TotalMinutes;

    private static string[] SplitRoles(string roles) => roles.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static bool TryParseOptions(IEnumerable<string> args, out Dictionary<string, string> options)
    {
        options = new Dictionary<string, string>(StringComparer.Ordinal);
        var list = args.ToList();
        for (var index = 0; index < list.Count; index += 2)
        {
            if (!list[index].StartsWith("--", StringComparison.Ordinal) || index + 1 >= list.Count || !options.TryAdd(list[index][2..], list[index + 1]))
            {
                return false;
            }
        }

        return true;
    }
}
