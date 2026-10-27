using System.ComponentModel.DataAnnotations;

namespace Knock.Api.Modules.Identity.Dtos;

/// <summary>Petición del canje: solo el ID token de Firebase (máx. 4096 caracteres).</summary>
internal sealed record FirebaseExchangeRequest(
    [Required(AllowEmptyStrings = false), StringLength(4096)] string FirebaseIdToken);

internal sealed record SessionResponse(string AccessToken, string RefreshToken);

internal sealed record MeResponse(string Id);
