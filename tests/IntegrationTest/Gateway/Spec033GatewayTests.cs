using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.Gateway.Hosting;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Knock.IntegrationTest.Gateway;

/// <summary>
/// Spec 033, T-09: el gateway separa los tokens de la app y del panel por ruta, limita los endpoints de sesión del panel por IP y concede CORS con
/// credenciales solo al origen del panel. Los servicios son aplicaciones mínimas en memoria y los contadores y la lista de sesiones canceladas son dobles
/// (no necesita Docker). Solo datos ficticios.
/// </summary>
public sealed class Spec033GatewayTests
{
    private const string _panelOrigin = "https://panel.example";
    private const string _appOrigin = "https://app.example";
    private static readonly byte[] _panelKey = RandomNumberGenerator.GetBytes(32);

    private sealed class CountingCounter : IRequestCounter
    {
        private readonly ConcurrentDictionary<string, long> _counts = new();

        public Task<(long Count, TimeSpan Remaining)> HitAsync(string key, TimeSpan window, CancellationToken cancellationToken) =>
            Task.FromResult((_counts.AddOrUpdate(key, 1, (_, current) => current + 1), window));
    }

    private sealed class CancelledSids(params string[] sids) : IRevokedSessions
    {
        public Task<bool> IsRevokedAsync(string sessionId, CancellationToken cancellationToken) => Task.FromResult(sids.Contains(sessionId));
    }

    private static string PanelToken(string? sid = null, string? sub = null, string audience = "knock-panel", byte[]? key = null)
    {
        var now = DateTime.UtcNow;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = GatewayTestSupport.Issuer,
            Audience = audience,
            Claims = new Dictionary<string, object>
            {
                ["sub"] = sub ?? Guid.NewGuid().ToString("D"),
                ["sid"] = sid ?? Guid.NewGuid().ToString("N"),
                ["pv"] = 1,
            },
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(15),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key ?? _panelKey), SecurityAlgorithms.HmacSha256),
        });
    }

    private static Task<GatewayHost> StartAsync(IRevokedSessions? revoked = null, IRequestCounter? counter = null) =>
        GatewayHost.StartAsync(
            new Dictionary<string, string?>
            {
                ["PanelJwt:SigningKey"] = Convert.ToBase64String(_panelKey),
                ["Cors:Panel:Origen"] = _panelOrigin,
                ["Security:Cors:AllowedOrigins:0"] = _appOrigin,
            },
            services =>
            {
                services.AddSingleton(revoked ?? new CancelledSids());
                services.AddSingleton(counter ?? new CountingCounter());
            });

    private static async Task<HttpResponseMessage> Send(GatewayHost host, string path, string? token = null, string? origin = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(path, UriKind.Relative));
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (origin is not null)
        {
            request.Headers.Add("Origin", origin);
        }

        return await host.Client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> SendPreflight(GatewayHost host, string path, string origin, string requestedHeaders)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, new Uri(path, UriKind.Relative));
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", requestedHeaders);
        return await host.Client.SendAsync(request);
    }

    [Fact]
    public async Task Tokens_No_Se_Cruzan_Entre_La_App_Y_El_Panel_CA_033_10()
    {
        await using var host = await StartAsync();
        var appToken = GatewayTestSupport.IssueToken();
        var panelToken = PanelToken();

        using var appOnPanel = await Send(host, "/api/v1/panel/prueba", appToken);
        using var appOnPanelSession = await Send(host, "/api/v1/panel/sesion/celular", appToken);
        using var panelOnApp = await Send(host, "/api/v1/solicitudes", panelToken);
        using var panelOnAnonymousApp = await Send(host, "/api/v1/auth/firebase", panelToken);
        using var panelOnPanel = await Send(host, "/api/v1/panel/prueba", panelToken);
        using var appOnApp = await Send(host, "/api/v1/solicitudes", appToken);

        appOnPanel.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        appOnPanelSession.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "el token de la app no vale en ninguna ruta del panel");
        panelOnApp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        panelOnAnonymousApp.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "el token del panel no vale en ninguna ruta de la app, ni anónima");
        panelOnPanel.StatusCode.Should().Be(HttpStatusCode.OK);
        panelOnPanel.Headers.GetValues("X-Servicio").Should().Equal("identidad");
        appOnApp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Un_Token_Del_Panel_Firmado_Con_Otra_Llave_O_Con_El_Destinatario_De_La_App_No_Entra_CA_033_10()
    {
        await using var host = await StartAsync();

        using var otherKey = await Send(host, "/api/v1/panel/prueba", PanelToken(key: RandomNumberGenerator.GetBytes(32)));
        using var appKey = await Send(host, "/api/v1/panel/prueba", PanelToken(key: GatewayTestSupport.SigningKey));
        using var appAudience = await Send(host, "/api/v1/panel/prueba", PanelToken(audience: "knock-app"));
        using var noToken = await Send(host, "/api/v1/panel/prueba");
        using var closeWithoutToken = await Send(host, "/api/v1/panel/sesion/cerrar");

        new[] { otherKey, appKey, appAudience, noToken, closeWithoutToken }.Select(response => response.StatusCode).Should().OnlyContain(status => status == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Las_Rutas_De_Sesion_Del_Panel_Son_Publicas_Y_Cerrar_Exige_El_Token_Del_Panel_R_27()
    {
        await using var host = await StartAsync();

        using var phone = await Send(host, "/api/v1/panel/sesion/celular");
        using var totp = await Send(host, "/api/v1/panel/sesion/totp");
        using var renew = await Send(host, "/api/v1/panel/sesion/renovar");
        using var activation = await Send(host, "/api/v1/panel/activacion");
        using var close = await Send(host, "/api/v1/panel/sesion/cerrar", PanelToken());

        new[] { phone, totp, renew, activation, close }.Select(response => response.StatusCode).Should().OnlyContain(status => status == HttpStatusCode.OK);
        new[] { phone, totp, renew, activation, close }.Select(response => response.Headers.GetValues("X-Servicio").Single()).Should().OnlyContain(name => name == "identidad");
    }

    [Fact]
    public async Task Una_Sesion_Del_Panel_Cancelada_Por_Sid_Se_Rechaza_En_El_Gateway_R_14()
    {
        var cancelled = Guid.NewGuid().ToString("N");
        await using var host = await StartAsync(new CancelledSids(cancelled));

        using var response = await Send(host, "/api/v1/panel/prueba", PanelToken(cancelled));
        using var live = await Send(host, "/api/v1/panel/prueba", PanelToken());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        live.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Mas_De_Diez_Peticiones_Por_Minuto_Desde_Una_Ip_A_Los_Endpoints_De_Sesion_Dan_429_Con_Retry_After_CA_033_22()
    {
        await using var host = await StartAsync();

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < 10; attempt++)
        {
            using var ok = await Send(host, "/api/v1/panel/sesion/celular");
            statuses.Add(ok.StatusCode);
        }

        using var eleventh = await Send(host, "/api/v1/panel/sesion/totp");

        statuses.Should().OnlyContain(status => status == HttpStatusCode.OK);
        eleventh.StatusCode.Should().Be(HttpStatusCode.TooManyRequests, "los endpoints de sesión comparten el límite estricto");
        eleventh.Headers.RetryAfter.Should().NotBeNull();
    }

    [Fact]
    public async Task El_Limite_De_Sesion_Del_Panel_No_Se_Mezcla_Con_El_De_Auth_De_La_App_R_27()
    {
        await using var host = await StartAsync();
        for (var attempt = 0; attempt < 10; attempt++)
        {
            using var panel = await Send(host, "/api/v1/panel/sesion/celular");
        }

        using var app = await Send(host, "/api/v1/auth/firebase");

        app.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Preflight_Desde_El_Origen_Del_Panel_Permite_Credenciales_Y_El_Encabezado_X_Knock_Panel_CA_033_23()
    {
        await using var host = await StartAsync();

        using var response = await SendPreflight(host, "/api/v1/panel/sesion/renovar", _panelOrigin, "x-knock-panel");

        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Equal(_panelOrigin);
        response.Headers.GetValues("Access-Control-Allow-Credentials").Should().Equal("true");
        response.Headers.GetValues("Access-Control-Allow-Headers").Single().Should().ContainEquivalentOf("X-Knock-Panel");
    }

    [Theory]
    [InlineData("https://otro.example", "/api/v1/panel/sesion/renovar")]
    [InlineData(_appOrigin, "/api/v1/panel/sesion/renovar")]
    [InlineData(_panelOrigin, "/api/v1/solicitudes")]
    [InlineData(_panelOrigin, "/api/v1/auth/firebase")]
    public async Task Desde_Otro_Origen_O_Hacia_Una_Ruta_Que_No_Es_Del_Panel_No_Se_Permiten_Credenciales_CA_033_23(string origin, string path)
    {
        await using var host = await StartAsync();

        using var response = await SendPreflight(host, path, origin, "x-knock-panel");
        using var actual = await Send(host, path, null, origin);

        response.Headers.Contains("Access-Control-Allow-Credentials").Should().BeFalse();
        actual.Headers.Contains("Access-Control-Allow-Credentials").Should().BeFalse();
        if (origin != _appOrigin)
        {
            response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
        }
    }

    [Fact]
    public async Task Las_Rutas_De_La_App_Siguen_Con_Su_Cors_Sin_Credenciales_Spec009()
    {
        await using var host = await StartAsync();

        using var response = await SendPreflight(host, "/api/v1/auth/firebase", _appOrigin, "authorization");

        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Equal(_appOrigin);
        response.Headers.Contains("Access-Control-Allow-Credentials").Should().BeFalse();
    }

    [Fact]
    public async Task Sin_Origen_Del_Panel_Configurado_Ningun_Origen_Recibe_Credenciales_R_27()
    {
        await using var host = await GatewayHost.StartAsync(
            new Dictionary<string, string?> { ["PanelJwt:SigningKey"] = Convert.ToBase64String(_panelKey), ["Cors:Panel:Origen"] = "http://panel.example/ruta" },
            services =>
            {
                services.AddSingleton<IRevokedSessions>(new NoRevokedSessions());
                services.AddSingleton<IRequestCounter>(new NoLimitCounter());
            });

        using var response = await SendPreflight(host, "/api/v1/panel/sesion/renovar", _panelOrigin, "x-knock-panel");

        response.Headers.Contains("Access-Control-Allow-Credentials").Should().BeFalse();
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }
}
