using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Knock.Api.Modules.Identity.Data;
using Knock.Api.Modules.Identity.Domain;
using Knock.IntegrationTest.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Knock.SecurityTest.Identity;

/// <summary>HU-001 caso 20: un ID token rechazado no revela si el celular tiene cuenta. Solo celulares y datos ficticios.</summary>
public class AccountEnumerationTests
{
    private const string _phoneWithAccount = "+525555550131";
    private const string _phoneWithoutAccount = "+525555550132";

    /// <summary>Doble que cuenta cualquier llamada a la base de usuarios: un rechazo no debe llegar a ella.</summary>
    private sealed class CountingStore : IIdentityStore
    {
        private int _calls;

        public int Calls => _calls;

        public Task<UserAccount?> FindBySubAsync(string sub, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            return Task.FromResult<UserAccount?>(null);
        }

        public Task<UserAccount?> FindByPhoneHashAsync(byte[] phoneHash, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            return Task.FromResult<UserAccount?>(null);
        }

        public Task<AccountCreation> CreateAccountAsync(NewAccount account, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            return Task.FromResult(AccountCreation.Created);
        }

        public Task AddRefreshTokenAsync(RefreshTokenRecord refresh, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            return Task.CompletedTask;
        }
    }

    private sealed record Rejection(HttpStatusCode Status, string Headers, string Shape, string Body);

    private static string Forge(string uid, string? phone, string project = FirebaseEmulator.ProjectId, bool expired = false)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = expired ? now.AddHours(-1) : now.AddMinutes(59);
        return FirebaseEmulator.ForgeUnsignedIdToken(uid, phone, project, expiresAt.AddHours(-1), expiresAt);
    }

    /// <summary>Respuesta sin lo que cambia en cada petición (<c>traceId</c>, <c>correlationId</c> y sus encabezados, fecha).</summary>
    private static async Task<Rejection> ExchangeAsync(HttpClient client, string idToken)
    {
        using var response = await AuthHttp.ExchangeAsync(client, idToken);
        var raw = await response.Content.ReadAsStringAsync();
        var body = JsonNode.Parse(raw)!.AsObject();
        body.Remove("traceId");
        body.Remove("correlationId");
        var headers = response.Headers.Concat(response.Content.Headers)
            .Where(h => !h.Key.Contains("correlation", StringComparison.OrdinalIgnoreCase)
                && !h.Key.Contains("trace", StringComparison.OrdinalIgnoreCase)
                && !h.Key.Equals("Date", StringComparison.OrdinalIgnoreCase))
            .OrderBy(h => h.Key, StringComparer.OrdinalIgnoreCase)
            .Select(h => $"{h.Key}={string.Join('|', h.Value)}");
        var shape = string.Join(',', body.Select(p => p.Key).Order(StringComparer.Ordinal));
        return new Rejection(response.StatusCode, string.Join(';', headers), shape, body.ToJsonString(new JsonSerializerOptions { WriteIndented = false }));
    }

    [Fact]
    public async Task RejectionsAreIndistinguishableAndNeverQueryUsers_TC_HU_001_20()
    {
        await FirebaseEmulator.EnsureAvailableAsync();
        var (realToken, _) = await FirebaseEmulator.SignInWithPhoneAsync(_phoneWithAccount);
        var store = new CountingStore();
        using var factory = new KnockApiFactory(configureServices: services =>
        {
            services.AddSingleton<IIdentityStore>(store);
        });
        using var client = factory.CreateClient();

        // El mismo tipo de rechazo, una vez para el celular que tiene cuenta y otra para el que no.
        var cases = new (string Name, string WithAccount, string WithoutAccount)[]
        {
            ("alterado", realToken[..^25], Forge("uid-prueba-0132", _phoneWithoutAccount)[..^5] + "AAAAA"),
            ("vencido", Forge("uid-prueba-0131", _phoneWithAccount, expired: true), Forge("uid-prueba-0132", _phoneWithoutAccount, expired: true)),
            ("otro proyecto", Forge("uid-prueba-0131", _phoneWithAccount, "otro-proyecto"), Forge("uid-prueba-0132", _phoneWithoutAccount, "otro-proyecto")),
            ("sin teléfono", Forge("uid-prueba-0131", null), Forge("uid-prueba-0132", null)),
            ("fuera de +52", Forge("uid-prueba-0131", "+14155550131"), Forge("uid-prueba-0132", "+14155550132")),
        };

        var all = new List<Rejection>();
        foreach (var (name, withAccount, withoutAccount) in cases)
        {
            var a = await ExchangeAsync(client, withAccount);
            var b = await ExchangeAsync(client, withoutAccount);

            a.Status.Should().Be(HttpStatusCode.Unauthorized, name);
            b.Should().BeEquivalentTo(a, $"con «{name}» el celular con cuenta y el que no la tiene no se distinguen");
            all.Add(a);
        }

        all.Select(r => r.Body).Distinct().Should().ContainSingle("todos los motivos de rechazo dan el mismo cuerpo");
        all.Select(r => r.Headers).Distinct().Should().ContainSingle("y los mismos encabezados");
        store.Calls.Should().Be(0, "un token rechazado no consulta ni escribe en la base de usuarios");

        // La diferencia entre cuenta nueva y existente solo aparece con un token válido (quien demuestra tener el celular).
        using var valid = await AuthHttp.ExchangeAsync(client, realToken);
        valid.StatusCode.Should().Be(HttpStatusCode.Created);
        store.Calls.Should().BeGreaterThan(0, "control: con un token válido sí se consulta la base, así que el contador mide algo");
    }
}
