namespace Knock.Api.Modules.Identity.Dtos;

/// <summary>Petición del canje. Los campos son anulables a propósito y no se declaran requeridos en el esquema: si falta alguno, el validador responde 422 con <c>errors</c> en su campo, en lugar del 400 automático de MVC. El celular y el <c>uid</c> no se aceptan: salen del token verificado.</summary>
internal sealed record FirebaseExchangeRequest(string? FirebaseIdToken = null, string? DeviceId = null, bool? AceptaAviso = null);

/// <summary>Sesión emitida. <c>EsNuevo</c> distingue el 201 (cuenta creada) del 200 (cuenta existente).</summary>
internal sealed record SessionResponse(string AccessToken, string RefreshToken, bool EsNuevo, bool PerfilCompleto);

/// <summary>Petición de renovación (HU-003). Campos anulables a propósito: si falta alguno, el validador responde 422 con <c>errors</c> en su campo.</summary>
internal sealed record RefreshSessionRequest(string? RefreshToken = null, string? DeviceId = null);

/// <summary>Petición de cierre de sesión (HU-004).</summary>
internal sealed record LogoutRequest(string? RefreshToken = null);

internal sealed record MeResponse(string Id);
