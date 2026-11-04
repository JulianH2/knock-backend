namespace Knock.Api.Modules.Identity.Dtos;

/// <summary>Petición del canje. Los campos son anulables a propósito y no se declaran requeridos en el esquema: si falta alguno, el validador responde 422 con <c>errors</c> en su campo, en lugar del 400 automático de MVC. El celular y el <c>uid</c> no se aceptan: salen del token verificado.</summary>
internal sealed record FirebaseExchangeRequest(string? FirebaseIdToken = null, string? DeviceId = null, bool? AceptaAviso = null);

/// <summary>Sesión emitida. <c>EsNuevo</c> distingue el 201 (cuenta creada) del 200 (cuenta existente).</summary>
internal sealed record SessionResponse(string AccessToken, string RefreshToken, bool EsNuevo, bool PerfilCompleto);

/// <summary>
/// Perfil de la persona con sesión (HU-002): <c>Nombre</c> es <c>null</c> mientras no lo haya puesto; <c>PerfilCompleto</c> significa «tiene nombre». Los dos
/// campos nuevos llevan valor por defecto para que el contrato los declare opcionales (cambio compatible: <c>id</c> sigue siendo el único requerido).
/// </summary>
internal sealed record MeResponse(string Id, string? Nombre = null, bool PerfilCompleto = false);
