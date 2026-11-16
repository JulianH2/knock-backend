using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Knock.SecurityTest.Support;
using Xunit;

namespace Knock.SecurityTest.Identity;

/// <summary>Spec 010, R-09 y CA-04: las respuestas de autenticación (con tokens) nunca se comprimen, aunque el cliente acepte gzip/br.</summary>
public class IdentityCompressionTests
{
    [Fact]
    public async Task LoginResponseIsNeverCompressed_CA_010_04()
    {
        var hosts = await SecurityHosts.SharedAsync();
        var factory = hosts.Identity;
        var phone = SecurityIdentityFactory.NewPhone();
        var uid = "uid-" + Guid.NewGuid().ToString("N")[..16];

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/firebase")
        {
            Content = JsonContent.Create(new
            {
                firebaseIdToken = factory.Firebase.Issue(uid, phone),
                deviceId = Guid.NewGuid().ToString("D"),
                aceptaAviso = true,
                dispositivo = "Teléfono de prueba",
            }),
        };
        request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, br");

        using var response = await hosts.IdentityClient.SendAsync(request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        response.Headers.Contains("Content-Encoding").Should().BeFalse();
        var body = await SecureHttp.ReadJsonAsync(response);
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
    }
}
