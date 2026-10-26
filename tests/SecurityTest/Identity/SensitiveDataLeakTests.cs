using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Knock.Api.Modules.Identity.Services;
using Knock.IntegrationTest.Support;
using Xunit;

namespace Knock.SecurityTest.Identity;

public class SensitiveDataLeakTests
{
    private const string _testPhone = "+15555550104";

    [Fact]
    public async Task ExchangeAndRejection_DoNotLeakSensitiveData_TC_TT_API_02_13()
    {
        var (idToken, uid) = await FirebaseEmulator.SignInWithPhoneAsync(_testPhone);
        var rejectedToken = idToken[..^25];
        string accessToken = string.Empty;
        string refreshToken = string.Empty;
        string successBody = string.Empty;
        string rejectionBody = string.Empty;
        string meBody = string.Empty;

        var logs = await ConsoleCapture.RunAsync(async () =>
        {
            using var factory = new KnockApiFactory();
            using var client = factory.CreateClient();

            using var ok = await AuthHttp.ExchangeAsync(client, idToken);
            ok.StatusCode.Should().Be(HttpStatusCode.OK);
            successBody = await ok.Content.ReadAsStringAsync();
            using var okAgain = await AuthHttp.ExchangeAsync(client, idToken);
            (accessToken, refreshToken) = await AuthHttp.ReadSessionAsync(okAgain);

            using var rejected = await AuthHttp.ExchangeAsync(client, rejectedToken);
            rejected.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            rejectionBody = await rejected.Content.ReadAsStringAsync();

            using var me = await AuthHttp.MeAsync(client, accessToken);
            meBody = await me.Content.ReadAsStringAsync();
            using var meRejected = await AuthHttp.MeAsync(client, accessToken + "x");
            meRejected.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        });

        logs.Should().Contain("/api/v1/auth/firebase", "la captura debe incluir los eventos de las peticiones (si no, la prueba sería vacía)");
        foreach (var secret in new[] { idToken, rejectedToken, _testPhone, accessToken, refreshToken, uid })
        {
            logs.Should().NotContain(secret, "los logs no deben contener tokens, teléfono ni uid");
        }

        // El cuerpo de la respuesta solo lleva los tokens de la sesión propia: ni teléfono, ni ID token, ni uid de Firebase.
        foreach (var secret in new[] { idToken, _testPhone, uid })
        {
            successBody.Should().NotContain(secret);
            rejectionBody.Should().NotContain(secret);
            meBody.Should().NotContain(secret);
        }

        rejectionBody.Should().NotContain(rejectedToken).And.NotContain("accessToken").And.NotContain("refreshToken");
    }

    [Fact]
    public async Task ExchangeDoesNotLogOrReturnUidOrSub_TC_TT_API_12_06()
    {
        var (idToken, uid) = await FirebaseEmulator.SignInWithPhoneAsync(_testPhone);
        var sub = UserIdDerivation.Derive(uid).ToString("D");
        var compactSub = sub.Replace("-", string.Empty, StringComparison.Ordinal);
        var bodies = new Dictionary<string, string>();

        var logs = await ConsoleCapture.RunAsync(async () =>
        {
            using var factory = new KnockApiFactory();
            using var client = factory.CreateClient();

            // Un canje correcto y uno rechazado (ID token cortado), y luego /me con la sesión emitida.
            foreach (var (name, token) in new[] { ("ok", idToken), ("rejected", idToken[..^25]) })
            {
                using var response = await AuthHttp.ExchangeAsync(client, token);
                bodies[name] = await response.Content.ReadAsStringAsync();
            }

            using var session = JsonDocument.Parse(bodies["ok"]);
            using var me = await AuthHttp.MeAsync(client, session.RootElement.GetProperty("accessToken").GetString()!);
            bodies["me"] = await me.Content.ReadAsStringAsync();
        });

        logs.Should().Contain("/api/v1/auth/firebase", "la captura debe incluir los eventos de las peticiones (si no, la prueba sería vacía)");
        logs.Should().NotContain(uid).And.NotContain(sub).And.NotContain(compactSub, "ni el uid ni el sub (con o sin guiones) aparecen en los logs");

        // El uid no sale en ninguna respuesta; el sub solo viaja dentro del JWT de acceso y en /me, que lo devuelve a su dueño.
        bodies["ok"].Should().NotContain(uid).And.NotContain(sub, "el sub va dentro del JWT, no en claro en el cuerpo");
        bodies["rejected"].Should().NotContain(uid).And.NotContain(sub);
        bodies["me"].Should().NotContain(uid).And.Contain(sub);

        // El sub no contiene el uid ni una forma trivial de él (texto, Base64, hexadecimal) ni es su hash simple.
        var uidBytes = Encoding.UTF8.GetBytes(uid);
        sub.Should().NotContain(uid);
        compactSub.Should().NotContain(Convert.ToHexStringLower(uidBytes)).And.NotContain(Convert.ToBase64String(uidBytes));
        compactSub.Should().NotBe(Convert.ToHexStringLower(SHA256.HashData(uidBytes))[..32]);
    }
}
