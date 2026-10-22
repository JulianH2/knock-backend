using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Knock.Api.Modules.Media.Dtos;

/// <summary>Petición de subida: uso previsto, tipo y tamaño declarados. Tipo o tamaño no permitidos → 422.</summary>
internal sealed record CreateUploadRequest(
    [property: JsonPropertyName("uso")]
    [Required(AllowEmptyStrings = false), RegularExpression("^(perfil|solicitud|trabajo)$")] string Usage,
    [property: JsonPropertyName("contentType")]
    [Required(AllowEmptyStrings = false), StringLength(100)] string ContentType,
    [property: JsonPropertyName("tamanoBytes")]
    [Range(1, long.MaxValue)] long SizeBytes);

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
