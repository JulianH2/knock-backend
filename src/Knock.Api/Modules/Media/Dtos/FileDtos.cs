using System.Text.Json.Serialization;

namespace Knock.Api.Modules.Media.Dtos;

/// <summary>
/// Petición de subida: uso previsto, tipo y tamaño declarados. La forma la valida <c>CreateUploadRequestValidator</c>
/// (422 con campos); tipo o tamaño no permitidos los rechaza el servicio (422).
/// </summary>
internal sealed record CreateUploadRequest(
    [property: JsonPropertyName("uso")] string? Usage,
    [property: JsonPropertyName("contentType")] string? ContentType,
    [property: JsonPropertyName("tamanoBytes")] long? SizeBytes);

/// <summary>Query de la lectura: <c>version</c> es <c>original</c> (por defecto) o <c>reducida</c>.</summary>
internal sealed record ReadLinkQuery(string? Version)
{
    public const string Original = "original";
    public const string Reduced = "reducida";
}

internal sealed record UploadCreatedResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("urlSubida")] Uri UploadUrl,
    [property: JsonPropertyName("venceEn")] DateTimeOffset ExpiresAt);

internal sealed record ConfirmedFileResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("estado")] string State,
    [property: JsonPropertyName("ancho")] int Width,
    [property: JsonPropertyName("alto")] int Height);

internal sealed record ReadLinkResponse(
    [property: JsonPropertyName("url")] Uri Url,
    [property: JsonPropertyName("venceEn")] DateTimeOffset ExpiresAt);
