using System.Net;
using AwesomeAssertions;
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
}
