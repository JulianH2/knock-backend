using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Knock.SecurityTest.Hosting;

public class ServerFingerprintTests
{
    [Fact]
    public async Task ServerHeaderIsOffAndNoVersionHeadersLeak_TC_TT_API_06_04()
    {
        using var factory = new KnockApiFactory();
        using var client = factory.CreateClient();

        // El TestServer no emite `Server`: la aserción con dientes es la de las opciones de Kestrel.
        factory.Services.GetRequiredService<IOptions<KestrelServerOptions>>().Value.AddServerHeader.Should().BeFalse();
        using var ok = await TestHttp.GetAsync(client, "/api/v1/test/ping");
        using var error = await TestHttp.GetAsync(client, "/api/v1/test/boom");

        foreach (var response in new[] { ok, error })
        {
            foreach (var header in new[] { "Server", "X-Powered-By", "X-AspNet-Version" })
            {
                SecurityHttp.HeaderValues(response, header).Should().BeEmpty($"{header} no debe salir");
            }
        }
    }
}
