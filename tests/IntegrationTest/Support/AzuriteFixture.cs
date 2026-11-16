using Azure.Storage.Blobs;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Knock.IntegrationTest.Support;

/// <summary>
/// Azurite real con Testcontainers, fijado por el digest de la imagen (spec 013, OI-2). Un contenedor por ejecución y por prueba
/// para que los contenedores de blob no se mezclen; el reaper de Testcontainers lo borra al terminar.
/// La cadena de conexión solo vive en memoria durante la prueba; usa la cuenta pública de desarrollo de Azurite, nunca una real.
/// </summary>
internal sealed class AzuriteFixture : IAsyncDisposable
{
    /// <summary>Etiqueta 3.37.0 fijada por el digest de la lista de manifiestos (amd64 y arm64).</summary>
    public const string Image =
        "mcr.microsoft.com/azure-storage/azurite:3.37.0@sha256:830430c1da1a2d537e08f3e6764dd1f5ae00cf0346bcaf625b968ec3f0971fd5";

    public const ushort BlobPort = 10000;

    /// <summary>Cuenta y llave públicas y documentadas del emulador de Azurite; no son un secreto real.</summary>
    private const string _account = "devstoreaccount1";

    private const string _accountKey =
        "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";

    /// <summary>
    /// Azure.Storage.Blobs 12.30 envía <c>x-ms-version</c> 2026-10-06, que Azurite 3.37 aún no reconoce; la opción
    /// documentada de Azurite para eso es <c>--skipApiVersionCheck</c> (solo pruebas, no cambia el código de la API).
    /// </summary>
    private readonly IContainer _container = new ContainerBuilder(Image)
        .WithPortBinding(BlobPort, assignRandomHostPort: true)
        .WithCommand("azurite", "-l", "/data", "--blobHost", "0.0.0.0", "--queueHost", "0.0.0.0", "--tableHost", "0.0.0.0", "--skipApiVersionCheck")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(BlobPort))
        .Build();

    private AzuriteFixture()
    {
    }

    /// <summary>Cadena para <c>ConnectionStrings:KnockBlob</c> y para crear clientes de Blob en la prueba.</summary>
    public string ConnectionString =>
        $"DefaultEndpointsProtocol=http;AccountName={_account};AccountKey={_accountKey};BlobEndpoint=http://{_container.Hostname}:{_container.GetMappedPublicPort(BlobPort)}/{_account};";

    public static async Task<AzuriteFixture> StartAsync()
    {
        var fixture = new AzuriteFixture();
        await fixture._container.StartAsync();
        return fixture;
    }

    public BlobContainerClient Container(string name) => new BlobServiceClient(ConnectionString).GetBlobContainerClient(name);

    /// <summary>Nombres de los blobs de un contenedor (vacío si el contenedor aún no existe).</summary>
    public async Task<IReadOnlyList<string>> ListBlobNamesAsync(string container)
    {
        var client = Container(container);
        if (!await client.ExistsAsync())
        {
            return [];
        }

        var names = new List<string>();
        await foreach (var blob in client.GetBlobsAsync())
        {
            names.Add(blob.Name);
        }

        return names;
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
