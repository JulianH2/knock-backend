using System.Security.Cryptography;
using System.Text;
using OtpNet;

namespace Knock.Identity.Api.Modules.Panel.Services;

/// <summary>
/// Llaves del cifrado del secreto TOTP (<c>PanelTotp:*</c>; en Azure, el secreto versionado <c>panel-totp-key</c> de Key Vault). Distintas de las del celular.
/// Solo llegan por variable de entorno o user-secrets: nunca van en <c>appsettings*.json</c>.
/// </summary>
internal sealed class PanelTotpOptions
{
    public const string SectionName = "PanelTotp";

    /// <summary>Llaves AES-256 por versión: Base64 de exactamente 32 bytes.</summary>
    public Dictionary<int, string> Keys { get; } = [];

    /// <summary>Versión vigente: la que usan las activaciones nuevas. Las cuentas guardan la versión con que se cifraron.</summary>
    public int KeyVersion { get; set; }
}

/// <summary>Llaves del secreto TOTP ya decodificadas y validadas.</summary>
internal sealed class PanelTotpKeys
{
    public const int KeyBytes = 32;

    private PanelTotpKeys(Dictionary<int, byte[]> keys, int currentVersion)
    {
        Keys = keys;
        CurrentVersion = currentVersion;
    }

    public IReadOnlyDictionary<int, byte[]> Keys { get; }

    public int CurrentVersion { get; }

    /// <summary>
    /// Como <see cref="TryCreate"/>, pero lanza si la configuración no es válida. Se llama al crear el servicio y no al arrancar: una API desplegada sin la llave
    /// nueva sigue arrancando y solo falla el panel. El mensaje nombra la clave de configuración y jamás su valor.
    /// </summary>
    public static PanelTotpKeys CreateOrThrow(PanelTotpOptions options) =>
        TryCreate(options, out var errors) ?? throw new Microsoft.Extensions.Options.OptionsValidationException(Microsoft.Extensions.Options.Options.DefaultName, typeof(PanelTotpOptions), errors);

    /// <summary>Valida la configuración. Los mensajes nombran la clave de configuración y nunca su valor.</summary>
    public static PanelTotpKeys? TryCreate(PanelTotpOptions options, out IReadOnlyList<string> errors)
    {
        ArgumentNullException.ThrowIfNull(options);
        var problems = new List<string>();
        var decoded = new Dictionary<int, byte[]>();
        if (options.Keys.Count == 0)
        {
            problems.Add($"{PanelTotpOptions.SectionName}:Keys is missing: set at least one key by environment variable or secret store.");
        }

        foreach (var (version, value) in options.Keys)
        {
            var buffer = new byte[(value?.Length ?? 0) + 4];
            if (!string.IsNullOrWhiteSpace(value) && Convert.TryFromBase64String(value, buffer, out var written) && written == KeyBytes)
            {
                decoded[version] = buffer[..written];
            }
            else
            {
                problems.Add($"{PanelTotpOptions.SectionName}:Keys:{version} must be Base64 of exactly {KeyBytes} bytes.");
            }
        }

        if (options.KeyVersion <= 0 || !decoded.ContainsKey(options.KeyVersion))
        {
            problems.Add($"{PanelTotpOptions.SectionName}:KeyVersion must be a positive integer that matches one valid key.");
        }

        errors = problems;
        return problems.Count == 0 ? new PanelTotpKeys(decoded, options.KeyVersion) : null;
    }
}

/// <summary>Secreto TOTP cifrado: <c>nonce (12) ‖ texto cifrado ‖ etiqueta (16)</c> y la versión de la llave.</summary>
internal sealed record ProtectedTotpSecret(byte[] Ciphertext, int KeyVersion)
{
    public override string ToString() => nameof(ProtectedTotpSecret);
}

/// <summary>
/// TOTP de RFC 6238 (HMAC-SHA1, 6 dígitos, pasos de 30 s, ventana de ±1 paso) y cifrado del secreto con AES-256-GCM (spec 033, R-09, R-26).
/// No conoce HTTP ni MongoDB: la prueba de arquitectura lo exige. El código se compara en tiempo constante y un paso ya aceptado no se vuelve a
/// aceptar: <see cref="Verify"/> devuelve el paso coincidente y quien guarda lo hace con una actualización condicionada a que sea mayor.
/// </summary>
internal sealed class TotpService(PanelTotpKeys keys)
{
    public const int SecretBytes = 20;
    public const int StepSeconds = 30;
    public const int Digits = 6;
    public const int WindowSteps = 1;

    private const int _nonceBytes = 12;
    private const int _tagBytes = 16;
    private const string _issuer = "Knock Panel";

    /// <summary>Secreto de 160 bits (RFC 4226, §4).</summary>
    public static byte[] GenerateSecret() => RandomNumberGenerator.GetBytes(SecretBytes);

    /// <summary>Número de paso de 30 segundos del instante dado.</summary>
    public static long StepOf(DateTimeOffset now) => now.ToUnixTimeSeconds() / StepSeconds;

    /// <summary>Código de 6 dígitos del paso dado (para armar el QR y para las pruebas).</summary>
    public static string CodeFor(byte[] secret, long step)
    {
        ArgumentNullException.ThrowIfNull(secret);
        var totp = new Totp(secret, StepSeconds, OtpHashMode.Sha1, Digits);
        return totp.ComputeTotp(DateTimeOffset.FromUnixTimeSeconds(step * StepSeconds).UtcDateTime);
    }

    /// <summary>
    /// Comprueba <paramref name="code"/> contra los pasos de la ventana (anterior, actual y siguiente). Devuelve el paso coincidente más alto que sea
    /// mayor que <paramref name="lastAcceptedStep"/>, o <c>null</c> si no hay (incorrecto, fuera de la ventana o ya aceptado). Recorre los tres pasos
    /// siempre y compara en tiempo constante: el tiempo no dice cuál coincidió.
    /// </summary>
    public static long? Verify(byte[] secret, string? code, DateTimeOffset now, long lastAcceptedStep)
    {
        ArgumentNullException.ThrowIfNull(secret);
        if (code is not { Length: Digits } || !code.All(c => c is >= '0' and <= '9'))
        {
            return null;
        }

        var submitted = Encoding.ASCII.GetBytes(code);
        var current = StepOf(now);
        long? best = null;
        for (var step = current - WindowSteps; step <= current + WindowSteps; step++)
        {
            var expected = Encoding.ASCII.GetBytes(CodeFor(secret, step));
            var matches = CryptographicOperations.FixedTimeEquals(submitted, expected);
            if (matches && step > lastAcceptedStep && (best is null || step > best))
            {
                best = step;
            }
        }

        return best;
    }

    /// <summary>Dirección <c>otpauth://</c> del secreto, con la etiqueta dada. Solo se entrega una vez, al activar (R-29).</summary>
    public static string BuildOtpauthUri(byte[] secret, string accountLabel)
    {
        ArgumentNullException.ThrowIfNull(secret);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountLabel);
        return new OtpUri(OtpType.Totp, secret, accountLabel, _issuer, OtpHashMode.Sha1, Digits, StepSeconds).ToString();
    }

    /// <summary>Cifra el secreto con la llave vigente. El dato asociado de GCM liga el texto cifrado a la cuenta: copiarlo a otra cuenta falla.</summary>
    public ProtectedTotpSecret Protect(byte[] secret, string sub)
    {
        ArgumentNullException.ThrowIfNull(secret);
        ArgumentException.ThrowIfNullOrWhiteSpace(sub);
        var nonce = RandomNumberGenerator.GetBytes(_nonceBytes);
        var output = new byte[_nonceBytes + secret.Length + _tagBytes];
        nonce.CopyTo(output, 0);
        using var aes = new AesGcm(keys.Keys[keys.CurrentVersion], _tagBytes);
        aes.Encrypt(nonce, secret, output.AsSpan(_nonceBytes, secret.Length), output.AsSpan(_nonceBytes + secret.Length, _tagBytes), AssociatedData(sub));
        return new ProtectedTotpSecret(output, keys.CurrentVersion);
    }

    /// <summary>Abre el secreto con la llave de la versión con que se cifró. Falla (<see cref="CryptographicException"/>) si el dato fue alterado o es de otra cuenta.</summary>
    public byte[] Unprotect(byte[] ciphertext, int keyVersion, string sub)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);
        ArgumentException.ThrowIfNullOrWhiteSpace(sub);
        if (!keys.Keys.TryGetValue(keyVersion, out var key) || ciphertext.Length < _nonceBytes + _tagBytes)
        {
            throw new CryptographicException("The TOTP secret cannot be opened.");
        }

        var length = ciphertext.Length - _nonceBytes - _tagBytes;
        var plaintext = new byte[length];
        using var aes = new AesGcm(key, _tagBytes);
        aes.Decrypt(
            ciphertext.AsSpan(0, _nonceBytes),
            ciphertext.AsSpan(_nonceBytes, length),
            ciphertext.AsSpan(_nonceBytes + length, _tagBytes),
            plaintext,
            AssociatedData(sub));
        return plaintext;
    }

    private static byte[] AssociatedData(string sub) => Encoding.UTF8.GetBytes($"panel_cuentas|totp|{sub}");
}
