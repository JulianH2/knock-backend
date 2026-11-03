namespace Knock.Api.Modules.Identity.Dtos;

/// <summary>
/// Petición del canje. Los campos son anulables a propósito: si falta alguno, el validador (422 con <c>errors</c>) lo reporta en su campo
/// en lugar del 400 automático de MVC. El celular y el <c>uid</c> no se aceptan: salen del token verificado.
/// </summary>
internal sealed record FirebaseExchangeRequest(string? FirebaseIdToken, string? DeviceId, bool? AceptaAviso);

/// <summary>Sesión emitida. <c>EsNuevo</c> distingue el 201 (cuenta creada) del 200 (cuenta existente).</summary>
internal sealed record SessionResponse(string AccessToken, string RefreshToken, bool EsNuevo, bool PerfilCompleto);

internal sealed record MeResponse(string Id);
