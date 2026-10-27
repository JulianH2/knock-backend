using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using Knock.IntegrationTest.Support;

namespace Knock.IntegrationTest.Media;

/// <summary>
/// Provoca la carrera entre la consulta y la descarga al confirmar: en cuanto la API lee las propiedades (HEAD) del
/// blob de cuarentena, sube otro contenido al mismo blob con un cliente aparte. Registra el ETag que vio la API y los
/// encabezados con los que después pidió la descarga (GET).
/// </summary>
internal sealed class ReplaceUploadedBlobPolicy(string connectionString, byte[] replacement) : HttpPipelineSynchronousPolicy
{
    private const string _quarantinePath = "/" + MediaHttp.QuarantineContainer + "/";
    private int _replaced;

    public bool Replaced => Volatile.Read(ref _replaced) == 1;

    public string? CheckedETag { get; private set; }

    public string? DownloadIfMatch { get; private set; }

    public string? DownloadRange { get; private set; }

    public override void OnSendingRequest(HttpMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (IsQuarantineBlob(message.Request, RequestMethod.Get))
        {
            DownloadIfMatch = Header(message.Request, "If-Match");
            DownloadRange = Header(message.Request, "x-ms-range") ?? Header(message.Request, "Range");
        }
    }

    public override void OnReceivedResponse(HttpMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!IsQuarantineBlob(message.Request, RequestMethod.Head) || message.Response.Status != 200
            || Interlocked.Exchange(ref _replaced, 1) == 1)
        {
            return;
        }

        CheckedETag = message.Response.Headers.ETag?.ToString("H");
        var blobName = message.Request.Uri.ToUri().Segments[^1];
        new BlobServiceClient(connectionString)
            .GetBlobContainerClient(MediaHttp.QuarantineContainer)
            .GetBlobClient(blobName)
            .Upload(BinaryData.FromBytes(replacement), overwrite: true, message.CancellationToken);
    }

    private static bool IsQuarantineBlob(Request request, RequestMethod method) =>
        request.Method == method && request.Uri.Path.Contains(_quarantinePath, StringComparison.Ordinal);

    private static string? Header(Request request, string name) => request.Headers.TryGetValue(name, out var value) ? value : null;
}
