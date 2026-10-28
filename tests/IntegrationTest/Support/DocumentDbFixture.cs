using System.Security.Cryptography;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Knock.IntegrationTest.Support;

/// <summary>
/// documentdb-local real con Testcontainers, fijada por el digest del spike (TT-BD-06, decisión (a)). Usuario y contraseña
/// son de un solo uso y solo viven en memoria. SecurityTest la comparte por el enlace a <c>IntegrationTest/Support</c>;
/// otras suites pueden enlazar este archivo igual.
/// </summary>
internal sealed class DocumentDbFixture : IAsyncDisposable
{
    /// <summary>Etiqueta <c>latest</c> del 2025-06-17, la que validó el spike en P5 y P9. Se cambia solo tras repetirlas (TT-BD-07).</summary>
    public const string Image =
        "ghcr.io/microsoft/documentdb/documentdb-local@sha256:0fcf634531c1917ad0855ff9f4354aca0a5c5d8e435f08250568deb37eeb0ad5";

    public const ushort Port = 10260;

    private const string _user = "knock";

    private static readonly Lazy<Task<DocumentDbFixture>> _shared = new(StartAsync);

    private readonly string _password = "Kn0ck" + Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
    private readonly IContainer _container;

    private DocumentDbFixture()
    {
        // La imagen recibe usuario y contraseña por argumentos y genera un certificado TLS autofirmado al arrancar.
        _container = new ContainerBuilder(Image)
            .WithPortBinding(Port, assignRandomHostPort: true)
            .WithCommand("--username", _user, "--password", _password)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(Port))
            .Build();
    }

    /// <summary>Cadena para <c>ConnectionStrings:KnockMongo</c> (TLS sin validar el certificado autofirmado: solo el contenedor local).</summary>
    public string ConnectionString => new MongoUrlBuilder
    {
        Server = new MongoServerAddress(_container.Hostname, _container.GetMappedPublicPort(Port)),
        Username = _user,
        Password = _password,
        UseTls = true,
        AllowInsecureTls = true,
        DirectConnection = true,
    }.ToString();

    /// <summary>Contenedor propio, para pruebas que lo detienen.</summary>
    public static async Task<DocumentDbFixture> StartAsync()
    {
        var fixture = new DocumentDbFixture();
        await fixture._container.StartAsync();
        await fixture.WaitUntilReadyAsync();
        return fixture;
    }

    /// <summary>Un contenedor por ejecución para las pruebas que solo lo leen; el reaper de Testcontainers lo borra al terminar.</summary>
    public static Task<DocumentDbFixture> SharedAsync() => _shared.Value;

    public Task StopAsync() => _container.StopAsync();

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    /// <summary>El puerto abre antes de que el servidor acepte comandos: se espera al primer ping (hasta 90 s).</summary>
    private async Task WaitUntilReadyAsync()
    {
        using var client = new MongoClient(ConnectionString);
        var deadline = DateTime.UtcNow.AddSeconds(90);
        while (true)
        {
            try
            {
                await client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
                return;
            }
            catch (Exception ex) when (ex is MongoException or TimeoutException && DateTime.UtcNow < deadline)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}
