using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace Knock.IntegrationTest.Support;

/// <summary>
/// Cliente mínimo del emulador oficial de Firebase Auth (solo pruebas; datos ficticios).
/// Arranque: <c>npx firebase-tools@15.34.0 emulators:start --only auth --project demo-knock</c> y
/// <c>FIREBASE_AUTH_EMULATOR_HOST=127.0.0.1:9099</c>. Si no está disponible, las pruebas FALLAN con un mensaje claro.
/// </summary>
internal static class FirebaseEmulator
{
    public const string ProjectId = "demo-knock";
    public const string HostVariable = "FIREBASE_AUTH_EMULATOR_HOST";

    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

    public static string Host => Environment.GetEnvironmentVariable(HostVariable) ?? string.Empty;

    private static string Instructions =>
        $"El emulador de Firebase Auth no está disponible (host: '{(Host.Length == 0 ? "sin definir" : Host)}'). " +
        $"Arráncalo con `npx firebase-tools@15.34.0 emulators:start --only auth --project {ProjectId}` " +
        $"(escucha solo en 127.0.0.1) y define {HostVariable}=127.0.0.1:9099 antes de correr las pruebas.";

    private static Uri Url(string path) => new($"http://{Host}/{path}");

    /// <summary>Falla (no salta) la prueba si el emulador no responde.</summary>
    public static async Task EnsureAvailableAsync()
    {
        if (string.IsNullOrWhiteSpace(Host))
        {
            throw new InvalidOperationException(Instructions);
        }

        try
        {
            using var response = await _http.GetAsync(Url(string.Empty));
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new InvalidOperationException(Instructions, ex);
        }
    }

    /// <summary>Crea (o reutiliza) un usuario con teléfono de prueba ficticio y devuelve su ID token y su uid.</summary>
    public static async Task<(string IdToken, string Uid)> SignInWithPhoneAsync(string phoneNumber)
    {
        await EnsureAvailableAsync();
        using var send = await _http.PostAsJsonAsync(
            Url("identitytoolkit.googleapis.com/v1/accounts:sendVerificationCode?key=fake"),
            new { phoneNumber, recaptchaToken = "fake" });
        send.EnsureSuccessStatusCode();
        var sessionInfo = (await ReadAsync(send)).GetProperty("sessionInfo").GetString();

        using var codes = await _http.GetAsync(Url($"emulator/v1/projects/{ProjectId}/verificationCodes"));
        codes.EnsureSuccessStatusCode();
        var code = (await ReadAsync(codes)).GetProperty("verificationCodes").EnumerateArray()
            .Last(c => c.GetProperty("sessionInfo").GetString() == sessionInfo)
            .GetProperty("code").GetString();

        using var signIn = await _http.PostAsJsonAsync(
            Url("identitytoolkit.googleapis.com/v1/accounts:signInWithPhoneNumber?key=fake"),
            new { sessionInfo, code });
        signIn.EnsureSuccessStatusCode();
        var body = await ReadAsync(signIn);
        return (body.GetProperty("idToken").GetString()!, body.GetProperty("localId").GetString()!);
    }

    /// <summary>ID token de un usuario anónimo: Firebase lo valida, pero no trae <c>phone_number</c>.</summary>
    public static async Task<string> SignInAnonymouslyAsync()
    {
        await EnsureAvailableAsync();
        using var response = await _http.PostAsJsonAsync(
            Url("identitytoolkit.googleapis.com/v1/accounts:signUp?key=fake"),
            new { returnSecureToken = true });
        response.EnsureSuccessStatusCode();
        return (await ReadAsync(response)).GetProperty("idToken").GetString()!;
    }

    /// <summary>Token sin firma (como los del emulador) con los claims indicados, para casos que el emulador no genera.</summary>
    public static string ForgeUnsignedIdToken(string uid, string? phoneNumber, string projectId, DateTimeOffset issuedAt, DateTimeOffset expiresAt)
    {
        var claims = new Dictionary<string, object?>
        {
            ["user_id"] = uid,
            ["sub"] = uid,
            ["aud"] = projectId,
            ["iss"] = $"https://securetoken.google.com/{projectId}",
            ["auth_time"] = issuedAt.ToUnixTimeSeconds(),
            ["iat"] = issuedAt.ToUnixTimeSeconds(),
            ["exp"] = expiresAt.ToUnixTimeSeconds(),
            ["firebase"] = new Dictionary<string, object?> { ["sign_in_provider"] = "phone" },
        };
        if (phoneNumber is not null)
        {
            claims["phone_number"] = phoneNumber;
        }

        var header = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes("""{"alg":"none","typ":"JWT"}"""));
        var payload = Base64UrlEncoder.Encode(JsonSerializer.SerializeToUtf8Bytes(claims));
        return string.Create(CultureInfo.InvariantCulture, $"{header}.{payload}.");
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
}
