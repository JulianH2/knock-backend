using System.Security.Cryptography;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Knock.IntegrationTest.Support;

/// <summary>
/// Redis real con Testcontainers (spec 003, R-07 · CA-03): una réplica, contraseña de un solo uso y sin persistencia, como en
/// <c>deploy/docker-compose.local.yml</c>. La contraseña solo vive en memoria.
/// </summary>
internal sealed class RedisFixture : IAsyncDisposable
{
    /// <summary>redis:7.2-alpine del 2026-10-29, fijada por digest: 7.2 es la última versión con licencia BSD-3 (7.4 y posteriores no).</summary>
    public const string Image = "redis:7.2-alpine@sha256:2c933526722b4126a61d5c170f766dc159adf97682f0b1cbbc27b1666ba96d84";

    public const ushort Port = 6379;

    private static readonly Lazy<Task<RedisFixture>> _shared = new(StartAsync);

    private readonly string _password = "Kn0ck" + Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
    private readonly IContainer _container;

    private RedisFixture()
    {
        _container = new ContainerBuilder(Image)
            .WithPortBinding(Port, assignRandomHostPort: true)
            .WithCommand("redis-server", "--requirepass", _password, "--save", "", "--appendonly", "no")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted(
                "redis-cli", "--no-auth-warning", "-a", _password, "ping"))
            .Build();
    }

    /// <summary>Cadena para <c>ConnectionStrings:KnockRedis</c>; nunca sale del proceso de pruebas.</summary>
    public string ConnectionString =>
        $"{_container.Hostname}:{_container.GetMappedPublicPort(Port)},password={_password},defaultDatabase=0,abortConnect=false";

    /// <summary>Un contenedor por ejecución; el reaper de Testcontainers lo borra al terminar.</summary>
    public static Task<RedisFixture> SharedAsync() => _shared.Value;

    public static async Task<RedisFixture> StartAsync()
    {
        var fixture = new RedisFixture();
        await fixture._container.StartAsync();
        return fixture;
    }

    /// <summary>IP interna del contenedor en la red de Docker: para otro contenedor (por ejemplo, las herramientas de entrega de CA_010_02).</summary>
    public string ContainerInternalHost => _container.IpAddress;

    /// <summary>Contraseña de un solo uso de la prueba; solo viaja por variables de entorno en memoria.</summary>
    public string Password => _password;

    /// <summary><c>redis-cli</c> sin credenciales, para probar que el servidor rechaza comandos sin autenticar.</summary>
    public Task<(long ExitCode, string Output)> RunCliAsync(params string[] arguments) =>
        ExecAsync(["redis-cli", .. arguments]);

    /// <summary><c>redis-cli</c> con la contraseña que guarda la fixture.</summary>
    public Task<(long ExitCode, string Output)> RunAuthenticatedCliAsync(params string[] arguments) =>
        ExecAsync(["redis-cli", "--no-auth-warning", "-a", _password, .. arguments]);

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    private async Task<(long ExitCode, string Output)> ExecAsync(string[] command)
    {
        var result = await _container.ExecAsync(command);
        return (result.ExitCode ?? -1, result.Stdout + result.Stderr);
    }
}
