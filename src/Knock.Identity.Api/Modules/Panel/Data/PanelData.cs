using Knock.Identity.Api.Modules.Panel.Domain;

namespace Knock.Identity.Api.Modules.Panel.Data;

/// <summary>Cuenta que crea la herramienta interna (R-25): sin secreto TOTP (<c>pendiente</c>) o, solo en Development, con el secreto de prueba ya cifrado.</summary>
internal sealed record NuevaCuentaPanel(
    string Sub,
    string Nombre,
    IReadOnlyList<string> Roles,
    DateTime CreadaEn,
    byte[]? TotpSecretoCifrado = null,
    int TotpVersionLlave = 0)
{
    public override string ToString() => nameof(NuevaCuentaPanel);
}

/// <summary>Sesión que se abre tras el segundo paso (R-12, R-15). Solo huellas del token de renovación.</summary>
internal sealed record NuevaSesionPanel(string Id, string Sub, string HuellaRenovacion, int PermisosVersion, DateTime CreadaEn, DateTime RenovacionVenceEn, DateTime VenceEn)
{
    public override string ToString() => nameof(NuevaSesionPanel);
}

/// <summary>Documento de auditoría (R-23). Solo estos campos: nunca celular, códigos, tokens, <c>idToken</c>, cuerpos ni datos de la persona sobre la que se actúa.</summary>
internal sealed record PanelAuditEntry(
    DateTime Fecha,
    string Tipo,
    string Evento,
    string Resultado,
    string IpHmac,
    string CorrelationId,
    string? Sub = null,
    string? Sid = null,
    string? Permiso = null,
    string? Ruta = null,
    string? RecursoId = null);

/// <summary>Valores de <c>tipo</c>, <c>evento</c> y <c>resultado</c> de la auditoría (§6).</summary>
internal static class PanelAuditValues
{
    public const string TipoAcceso = "acceso";
    public const string TipoSesion = "sesion";
    public const string TipoAccion = "accion";
    public const string TipoAdministracion = "administracion";

    public const string ResultadoPermitido = "permitido";
    public const string ResultadoDenegado = "denegado";
    public const string ResultadoFallido = "fallido";

    public const string CelularVerificado = "celular_verificado";
    public const string CelularRechazado = "celular_rechazado";
    public const string TotpFallido = "totp_fallido";
    public const string AccesoConcedido = "acceso_concedido";
    public const string Bloqueo = "bloqueo";
    public const string TotpActivado = "totp_activado";
    public const string Renovacion = "renovacion";
    public const string RenovacionConcurrente = "renovacion_concurrente";
    public const string ReusoDetectado = "reuso_detectado";
    public const string PermisosCambiados = "permisos_cambiados";
    public const string Cierre = "cierre";
    public const string Accion = "accion";
    public const string Alta = "alta";
    public const string RolesCambiados = "roles_cambiados";
    public const string Deshabilitada = "deshabilitada";
    public const string Habilitada = "habilitada";
    public const string TotpReiniciado = "totp_reiniciado";
}

/// <summary>Resultado de consumir una invitación de activación (R-29).</summary>
internal enum ActivacionResultado
{
    Consumida,
    Invalida,
}

/// <summary>Cuenta y sesión de una invitación vigente, antes de consumirla.</summary>
internal sealed record InvitacionVigente(string CuentaId, string Sub)
{
    public override string ToString() => nameof(InvitacionVigente);
}

/// <summary><c>panel_cuentas</c> (spec 033, §6). Todas las escrituras son actualizaciones atómicas de un documento.</summary>
internal interface IPanelCuentasStore
{
    Task<CuentaPanel?> FindBySubAsync(string sub, CancellationToken cancellationToken);

    /// <summary>Crea la cuenta; <c>false</c> si ya existe una con ese <c>sub</c> (índice único).</summary>
    Task<bool> InsertAsync(NuevaCuentaPanel account, CancellationToken cancellationToken);

    /// <summary>Cambia los roles y aumenta <c>permisosVersion</c> en uno; devuelve la cuenta resultante o <c>null</c> si no existe.</summary>
    Task<CuentaPanel?> SetRolesAsync(string sub, IReadOnlyList<string> roles, DateTime now, CancellationToken cancellationToken);

    /// <summary>Cambia el estado y aumenta <c>permisosVersion</c> en uno.</summary>
    Task<CuentaPanel?> SetEstadoAsync(string sub, string estado, DateTime now, CancellationToken cancellationToken);

    /// <summary>Deja el TOTP en <c>pendiente</c> sin secreto, borra el contador de fallos y aumenta <c>permisosVersion</c>.</summary>
    Task<CuentaPanel?> ResetTotpAsync(string sub, DateTime now, CancellationToken cancellationToken);

    /// <summary>Acepta el paso con una actualización condicionada a que sea mayor que <c>totpUltimoPaso</c>: de dos peticiones con el mismo código solo una devuelve <c>true</c> (R-09).</summary>
    Task<bool> TryAcceptStepAsync(string sub, long step, CancellationToken cancellationToken);

    /// <summary>Cuenta un fallo de TOTP de forma atómica y persistente (R-11). Con el quinto fallo de la ventana fija de 15 minutos fija <c>bloqueadoHasta</c>. Devuelve la cuenta resultante.</summary>
    Task<CuentaPanel?> RegisterFailureAsync(string sub, DateTime now, CancellationToken cancellationToken);

    /// <summary>Un acceso exitoso reinicia el contador de la cuenta (no el de la IP).</summary>
    Task ResetFailuresAsync(string sub, CancellationToken cancellationToken);
}

/// <summary><c>panel_sesiones</c> (spec 033, §6).</summary>
internal interface IPanelSesionesStore
{
    /// <summary>Abre la sesión y, si la cuenta pasa de tres abiertas, revoca las más antiguas en la misma transacción. Devuelve los <c>sid</c> revocados.</summary>
    Task<IReadOnlyList<string>> CreateAsync(NuevaSesionPanel session, CancellationToken cancellationToken);

    Task<SesionPanel?> FindByFingerprintAsync(string fingerprint, CancellationToken cancellationToken);

    Task<SesionPanel?> FindByUsedFingerprintAsync(string fingerprint, CancellationToken cancellationToken);

    Task<SesionPanel?> FindByIdAsync(string sid, CancellationToken cancellationToken);

    /// <summary>Rota la huella con una sola actualización condicionada a la huella vigente, a que no esté revocada ni vencida y a que la cookie siga vigente (R-17).</summary>
    Task<bool> RotateAsync(string sid, string currentFingerprint, string newFingerprint, DateTime now, DateTime renewalExpiresAt, CancellationToken cancellationToken);

    /// <summary>Revoca todas las sesiones abiertas de la cuenta y devuelve sus <c>sid</c> para cancelarlos en Redis.</summary>
    Task<IReadOnlyList<string>> RevokeAllAsync(string sub, CancellationToken cancellationToken);

    /// <summary>Revoca una sesión; <c>false</c> si no existe o ya estaba revocada.</summary>
    Task<bool> RevokeAsync(string sid, CancellationToken cancellationToken);
}

/// <summary><c>panel_activaciones</c> (spec 033, §6).</summary>
internal interface IPanelActivacionesStore
{
    /// <summary>Crea la invitación de la cuenta; reemplaza a la anterior (una por cuenta).</summary>
    Task CreateAsync(string accountId, byte[] fingerprint, DateTime now, DateTime expiresAt, CancellationToken cancellationToken);

    /// <summary>Invitación sin consumir y vigente de esa huella cuya cuenta sigue en <c>pendiente</c>; <c>null</c> en cualquier otro caso.</summary>
    Task<InvitacionVigente?> FindLiveAsync(byte[] fingerprint, DateTime now, CancellationToken cancellationToken);

    /// <summary>En una sola transacción marca la invitación como consumida y pasa la cuenta de <c>pendiente</c> a <c>configurado</c> con el secreto cifrado (R-29).</summary>
    Task<ActivacionResultado> ConsumeAsync(byte[] fingerprint, string accountId, ProtectedSecretForStore secret, DateTime now, CancellationToken cancellationToken);
}

/// <summary>Secreto ya cifrado que la activación guarda en la cuenta.</summary>
internal sealed record ProtectedSecretForStore(byte[] Ciphertext, int KeyVersion)
{
    public override string ToString() => nameof(ProtectedSecretForStore);
}

/// <summary><c>panel_auditoria</c>: de solo inserción para el servicio (R-23). No hay lectura, actualización ni borrado en el código.</summary>
internal interface IPanelAuditoriaStore
{
    Task InsertAsync(PanelAuditEntry entry, CancellationToken cancellationToken);
}

/// <summary>Desafío de un solo uso y fallos por IP en Redis (R-07, R-11).</summary>
internal interface IPanelDesafios
{
    Task CreateAsync(string challengeId, string sub, TimeSpan lifetime, CancellationToken cancellationToken);

    /// <summary>Lee el <c>sub</c> del desafío sin consumirlo (un código incorrecto no lo gasta: los fallos se cuentan por cuenta).</summary>
    Task<string?> PeekAsync(string challengeId, CancellationToken cancellationToken);

    /// <summary>Lee y borra el desafío en una sola operación atómica: de dos consumos solo uno recibe el <c>sub</c>.</summary>
    Task<string?> ConsumeAsync(string challengeId, CancellationToken cancellationToken);

    Task<long> IpFailuresAsync(string ip, CancellationToken cancellationToken);

    /// <summary>Suma un fallo a la IP con el vencimiento puesto en la misma operación atómica.</summary>
    Task RegisterIpFailureAsync(string ip, TimeSpan window, CancellationToken cancellationToken);
}

/// <summary>Datos de la cuenta que se cachean 60 segundos (R-24).</summary>
internal sealed record CuentaPanelEnCache(IReadOnlyList<string> Roles, string Estado, int PermisosVersion, string Nombre)
{
    public override string ToString() => nameof(CuentaPanelEnCache);
}

/// <summary>Caché de la cuenta del panel en Redis: <c>panel:cuenta:{sub}</c>, 60 segundos (R-24).</summary>
internal interface IPanelCuentaCache
{
    Task<CuentaPanelEnCache?> GetAsync(string sub, CancellationToken cancellationToken);

    Task SetAsync(string sub, CuentaPanelEnCache value, CancellationToken cancellationToken);

    Task DeleteAsync(string sub, CancellationToken cancellationToken);
}
