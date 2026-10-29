using System.Net;
using System.Security.Cryptography;
using AwesomeAssertions;
using Knock.IntegrationTest.Support;
using Knock.SecurityTest.Secrets;
using MongoDB.Driver;
using Xunit;

namespace Knock.SecurityTest.Hosting;

public sealed class KnockMongoSecretTests
{
    private const string _connectionStringKey = "ConnectionStrings:KnockMongo";

    [Fact]
    public async Task ConnectionStringNeverAppearsInConfigLogsOrErrors_TC_TT_BD_06_14()
    {
        // Sin cadena (anula también una variable de entorno de la máquina): no arranca y el mensaje nombra la clave.
        using (var missing = new KnockApiFactory(settings: new Dictionary<string, string?> { [_connectionStringKey] = null }))
        {
            var startup = () => missing.CreateClient();
            AllMessages(startup.Should().Throw<Exception>().Which).Should().Contain(_connectionStringKey);
        }

        // Usuario y contraseña ficticios hacia un host que no responde: ni el log ni /health/ready los repiten.
        var user = "knock-fake-" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(4));
        var password = "Fake" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(12));
        var connectionString = new MongoUrlBuilder
        {
            Server = new MongoServerAddress("127.0.0.1", 9),
            Username = user,
            Password = password,
            DirectConnection = true,
        }.ToString();
        var status = HttpStatusCode.OK;
        var body = string.Empty;

        var logs = await ConsoleCapture.RunAsync(async () =>
        {
            using var factory = new KnockApiFactory(settings: new Dictionary<string, string?> { [_connectionStringKey] = connectionString });
            using var client = factory.CreateClient();
            using var ready = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));
            status = ready.StatusCode;
            body = await ready.Content.ReadAsStringAsync();
        });

        status.Should().Be(HttpStatusCode.ServiceUnavailable);
        logs.Should().NotBeEmpty("la captura debe ver los logs del host para que la ausencia signifique algo");
        foreach (var leaked in new[] { user, password, connectionString })
        {
            logs.Should().NotContain(leaked);
            body.Should().NotContain(leaked);
        }

        // Ningún appsettings*.json del repositorio trae una cadena de MongoDB.
        var files = Directory.GetFiles(AppSettingsSecretsTests.ApiProjectDirectory(), "appsettings*.json", SearchOption.TopDirectoryOnly);
        files.Should().NotBeEmpty();
        foreach (var file in files)
        {
            var json = await File.ReadAllTextAsync(file);
            json.Should().NotContainAny(["mongodb://", "mongodb+srv://"], $"{Path.GetFileName(file)} no debe traer la cadena de MongoDB");
        }
    }

    [Fact]
    public void InvalidConnectionString_StartupNamesTheKeyWithoutTheValue_TC_TT_BD_06_14()
    {
        var password = "Fake" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(12));
        var invalid = $"not-a-scheme://knock-fake:{password}@127.0.0.1:9";
        using var factory = new KnockApiFactory(settings: new Dictionary<string, string?> { [_connectionStringKey] = invalid });

        var startup = () => factory.CreateClient();

        var messages = AllMessages(startup.Should().Throw<Exception>().Which);
        messages.Should().Contain(_connectionStringKey);
        messages.Should().NotContain(password).And.NotContain(invalid);
    }

    private static string AllMessages(Exception exception)
    {
        var messages = new List<string>();
        for (var current = exception; current is not null; current = current.InnerException)
        {
            messages.Add(current.Message);
            if (current is AggregateException aggregate)
            {
                messages.AddRange(aggregate.InnerExceptions.Select(inner => AllMessages(inner)));
            }
        }

        return string.Join(Environment.NewLine, messages);
    }
}
