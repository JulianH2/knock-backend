using Azure.Storage.Blobs;
using Testcontainers.Azurite;

namespace Knock.IntegrationTest.Support;

/// <summary>
/// Azurite real con Testcontainers (imagen fijada). Su cadena de conexión usa la cuenta de desarrollo pública de
/// Azurite y solo vive en memoria durante la prueba; nunca se escribe en el repositorio ni en logs.
/// </summary>
internal sealed class AzuriteFixture : IAsyncDisposable
{
    public const string Image = "mcr.microsoft.com/azure-storage/azurite:3.37.0";

    /// <summary>
    /// Azure.Storage.Blobs 12.30 envía <c>x-ms-version</c> 2026-10-06, que Azurite 3.37 aún no reconoce; la opción
    /// documentada de Azurite para eso es <c>--skipApiVersionCheck</c> (solo pruebas, no cambia el código de la API).
    /// </summary>
    private readonly AzuriteContainer _container = new AzuriteBuilder(Image)
        .WithInMemoryPersistence()
        .WithCommand("--skipApiVersionCheck")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

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
