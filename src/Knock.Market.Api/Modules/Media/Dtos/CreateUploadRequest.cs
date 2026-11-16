using System.Text.Json.Serialization;

namespace Knock.Market.Api.Modules.Media.Dtos;

/// <summary>Petición de subida: uso, tipo y tamaño declarados.</summary>
internal sealed record CreateUploadRequest(
    [property: JsonPropertyName("uso")] string? Usage,
    [property: JsonPropertyName("contentType")] string? ContentType,
    [property: JsonPropertyName("tamanoBytes")] long? SizeBytes);
