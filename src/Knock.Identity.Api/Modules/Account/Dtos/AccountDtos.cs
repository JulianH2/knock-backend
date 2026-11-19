namespace Knock.Identity.Api.Modules.Account.Dtos;

/// <summary>Cuerpo de <c>POST /me/cambio-numero</c> (USR-03). El campo es anulable a propósito: si falta, el validador responde 422 en su campo.</summary>
internal sealed record PhoneAvailabilityRequest(string? CelularNuevo = null);

/// <summary>Cuerpo de <c>POST /me/cambio-numero/confirmar</c> (USR-04): solo el token de Firebase del número nuevo; el celular sale del token verificado.</summary>
internal sealed record ConfirmPhoneChangeRequest(string? FirebaseIdToken = null);

/// <summary>Cuerpo de <c>POST /recuperacion-acceso</c> (USR-05).</summary>
internal sealed record AccessRecoveryRequest(string? FirebaseIdToken = null, string? CelularAnterior = null, string? Tipo = null);

/// <summary>Cuerpo de <c>PUT /me/consentimientos</c> (USR-08).</summary>
internal sealed record UpdateConsentRequest(string? Tipo = null, bool? Vigente = null);

/// <summary>Un consentimiento (USR-07 y USR-08). <c>Version</c> y <c>Fecha</c> son nulos mientras la persona no ha activado un opcional.</summary>
internal sealed record ConsentResponse(string Tipo, bool Vigente, bool Obligatorio, string? Version, DateTime? Fecha);

/// <summary>Citas o cotizaciones abiertas: el total y hasta 50 identificadores propios.</summary>
internal sealed record PendingGroupResponse(int Total, IReadOnlyList<string> Ids);

/// <summary>Pendientes antes de eliminar la cuenta (USR-09).</summary>
internal sealed record PendingResponse(PendingGroupResponse Citas, PendingGroupResponse Cotizaciones);

/// <summary>Cuerpo de <c>POST /me/eliminacion</c> (USR-10).</summary>
internal sealed record DeleteAccountRequest(string? Confirmacion = null, string? FirebaseIdToken = null, string? Motivo = null);

/// <summary>Eliminación programada (USR-10): cuándo vence la gracia de 7 días.</summary>
internal sealed record DeletionScheduledResponse(DateTime EliminarEn);
