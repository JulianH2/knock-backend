using System.Collections.Frozen;

namespace Knock.Identity.Api.Modules.Panel.Domain;

/// <summary>Lista cerrada de permisos del panel (spec 033, R-02). Cambiarla exige una spec.</summary>
internal static class PanelPermisos
{
    public const string VerificationReview = "verification:review";
    public const string ReportsResolve = "reports:resolve";
    public const string ReviewsModerate = "reviews:moderate";
    public const string CatalogManage = "catalog:manage";
    public const string MetricsRead = "metrics:read";

    /// <summary>Los cinco permisos en el orden de la lista: es el orden en que se devuelven (§5, «Formas»).</summary>
    public static IReadOnlyList<string> All { get; } = [VerificationReview, ReportsResolve, ReviewsModerate, CatalogManage, MetricsRead];

    public static bool IsKnown(string? permission) => permission is not null && All.Contains(permission, StringComparer.Ordinal);
}

/// <summary>Lista cerrada de roles del panel (R-02).</summary>
internal static class PanelRoles
{
    public const string Administrador = "administrador";
    public const string Verificador = "verificador";
    public const string Moderador = "moderador";
    public const string Catalogo = "catalogo";
    public const string Analista = "analista";

    public const int MaxRolesPerAccount = 5;

    public static IReadOnlyList<string> All { get; } = [Administrador, Verificador, Moderador, Catalogo, Analista];

    public static bool IsKnown(string? role) => role is not null && All.Contains(role, StringComparer.Ordinal);
}

/// <summary>
/// Mapa de rol a permisos (R-02). Vive en código a propósito: cambiarlo exige una spec y no hay endpoint ni documento que lo altere.
/// Los permisos efectivos de una cuenta son la unión de los de sus roles, sin repetidos y en el orden de <see cref="PanelPermisos.All"/>.
/// </summary>
internal static class MapaDeRoles
{
    private static readonly FrozenDictionary<string, string[]> _map = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        [PanelRoles.Administrador] = [.. PanelPermisos.All],
        [PanelRoles.Verificador] = [PanelPermisos.VerificationReview],
        [PanelRoles.Moderador] = [PanelPermisos.ReportsResolve, PanelPermisos.ReviewsModerate],
        [PanelRoles.Catalogo] = [PanelPermisos.CatalogManage],
        [PanelRoles.Analista] = [PanelPermisos.MetricsRead],
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>Permisos de un rol; vacío si el rol no está en la lista cerrada.</summary>
    public static IReadOnlyList<string> PermisosDe(string role) => _map.TryGetValue(role, out var permissions) ? permissions : [];

    /// <summary>Permisos efectivos de los roles dados (unión, sin repetidos, en el orden de la lista cerrada). Un rol desconocido no aporta nada.</summary>
    public static IReadOnlyList<string> PermisosDe(IEnumerable<string> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);
        var granted = roles.SelectMany(PermisosDe).ToHashSet(StringComparer.Ordinal);
        return [.. PanelPermisos.All.Where(granted.Contains)];
    }

    /// <summary>Una cuenta tiene de uno a cinco roles, todos de la lista cerrada y sin repetir.</summary>
    public static bool AreValid(IReadOnlyCollection<string>? roles) =>
        roles is { Count: >= 1 and <= PanelRoles.MaxRolesPerAccount }
        && roles.All(PanelRoles.IsKnown)
        && roles.Distinct(StringComparer.Ordinal).Count() == roles.Count;
}

/// <summary>Valores de <c>estado</c> de una cuenta del panel (R-03).</summary>
internal static class PanelCuentaEstado
{
    public const string Activa = "activa";
    public const string Deshabilitada = "deshabilitada";
}

/// <summary>Valores de <c>totpEstado</c> (R-25, R-29): sin secreto o con el secreto ya activado.</summary>
internal static class PanelTotpEstado
{
    public const string Pendiente = "pendiente";
    public const string Configurado = "configurado";
}

/// <summary>
/// Cuenta del panel (R-01, R-03). El secreto TOTP viaja cifrado y solo lo abre <c>TotpService</c>; <see cref="ToString"/> no imprime nada.
/// </summary>
internal sealed record CuentaPanel(
    string Id,
    string Sub,
    string Nombre,
    IReadOnlyList<string> Roles,
    string Estado,
    int PermisosVersion,
    byte[]? TotpSecretoCifrado,
    int TotpVersionLlave,
    long TotpUltimoPaso,
    string TotpEstado,
    int IntentosFallidos,
    DateTime? VentanaFallosInicia,
    DateTime? BloqueadoHasta)
{
    public bool EstaActiva => Estado == PanelCuentaEstado.Activa;

    public bool TieneTotp => TotpEstado == PanelTotpEstado.Configurado && TotpSecretoCifrado is { Length: > 0 };

    public IReadOnlyList<string> Permisos => MapaDeRoles.PermisosDe(Roles);

    public bool EstaBloqueada(DateTime now) => BloqueadoHasta is { } until && until > now;

    public override string ToString() => nameof(CuentaPanel);
}

/// <summary>Sesión del panel tal como la ve la renovación (R-15). Solo huellas: el token de renovación no se guarda.</summary>
internal sealed record SesionPanel(
    string Id,
    string Sub,
    string HuellaRenovacion,
    IReadOnlyList<string> HuellasUsadas,
    DateTime RotadaEn,
    int PermisosVersion,
    DateTime CreadaEn,
    DateTime UltimaActividad,
    DateTime RenovacionVenceEn,
    DateTime VenceEn,
    bool Revocada)
{
    public override string ToString() => nameof(SesionPanel);
}

/// <summary>Vidas y topes de la sesión del panel (R-13, R-15, R-16, R-17) y de los fallos de TOTP (R-11). Valores de la spec, no de configuración.</summary>
internal static class PanelSesionReglas
{
    public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan RenewalCookieLifetime = TimeSpan.FromMinutes(20);
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);
    public static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan RotationGrace = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan ActivationLifetime = TimeSpan.FromMinutes(15);

    public const int MaxOpenSessions = 3;
    public const int MaxUsedFingerprints = 5;
    public const int MaxAccountFailures = 5;
    public const int MaxIpFailures = 10;

    /// <summary>Vencimiento de la sesión: 8 horas desde el acceso con TOTP; después hay que repetir los dos pasos (R-15).</summary>
    public static DateTime SessionExpiry(DateTime createdAt) => createdAt + SessionLifetime;

    /// <summary>Vencimiento de la cookie de renovación emitida ahora; nunca pasa de la vida de la sesión.</summary>
    public static DateTime RenewalExpiry(DateTime now, DateTime sessionExpiry) => Min(now + RenewalCookieLifetime, sessionExpiry);

    /// <summary>Vencimiento del token de acceso emitido ahora; nunca pasa de la vida de la sesión.</summary>
    public static DateTime AccessExpiry(DateTime now, DateTime sessionExpiry) => Min(now + AccessTokenLifetime, sessionExpiry);

    /// <summary>La sesión ya no puede renovarse: revocada, pasadas las 8 horas o con la cookie vencida (R-17).</summary>
    public static bool CannotRenew(SesionPanel session, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session.Revocada || session.VenceEn <= now || session.RenovacionVenceEn <= now;
    }

    /// <summary>La ventana fija de fallos ya terminó (o nunca empezó): el siguiente fallo abre una ventana nueva (R-11).</summary>
    public static bool FailureWindowEnded(DateTime? windowStart, DateTime now) => windowStart is not { } start || start + FailureWindow <= now;

    /// <summary>Hasta cuándo queda bloqueada la cuenta al llegar al quinto fallo: el final de la ventana (R-11).</summary>
    public static DateTime LockUntil(DateTime windowStart) => windowStart + FailureWindow;

    /// <summary>Cuántas sesiones hay que revocar (las más antiguas) para que, con la nueva, queden como máximo tres (R-15).</summary>
    public static int SessionsToRevoke(int openBeforeAdding) => Math.Max(0, openBeforeAdding + 1 - MaxOpenSessions);

    private static DateTime Min(DateTime left, DateTime right) => left <= right ? left : right;
}

/// <summary>Qué hacer con un token de renovación que no es el vigente (R-17).</summary>
internal enum PanelReuse
{
    /// <summary>No está entre las huellas usadas: desconocido.</summary>
    Unknown,

    /// <summary>Es la huella inmediatamente anterior, rotada hace 10 segundos o menos: renovación concurrente tolerada (401 sin revocar).</summary>
    Concurrent,

    /// <summary>Cualquier otra huella usada, o la anterior pasada la gracia: token robado, se revocan todas las sesiones.</summary>
    Stolen,
}

/// <summary>Clasifica el reuso de un token de renovación contra el historial de la sesión.</summary>
internal static class PanelReuseClassifier
{
    public static PanelReuse Classify(SesionPanel session, string fingerprint, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(fingerprint);
        if (!session.HuellasUsadas.Contains(fingerprint, StringComparer.Ordinal))
        {
            return PanelReuse.Unknown;
        }

        var isPrevious = session.HuellasUsadas.Count > 0 && string.Equals(session.HuellasUsadas[^1], fingerprint, StringComparison.Ordinal);
        return isPrevious && now - session.RotadaEn <= PanelSesionReglas.RotationGrace ? PanelReuse.Concurrent : PanelReuse.Stolen;
    }
}

/// <summary>Códigos de error estables del panel (spec 033, §5). Nunca llevan datos de la persona.</summary>
internal static class PanelErrorCodes
{
    public const string InvalidCredentialsCode = "invalid_credentials";
    public const string NoPanelAccessCode = "no_panel_access";
    public const string InvalidCodeCode = "invalid_code";
    public const string ChallengeExpiredCode = "challenge_expired";
    public const string LockedCode = "locked";
    public const string LimitExceededCode = "limit_exceeded";
    public const string InvalidSessionCode = "invalid_session";
    public const string PermissionsChangedCode = "permissions_changed";
    public const string InsufficientPermissionCode = "insufficient_permission";
    public const string OriginNotAllowedCode = "origin_not_allowed";
    public const string ActivationInvalidCode = "activation_invalid";
    public const string UnavailableCode = "unavailable";
    public const string AuditUnavailableCode = "audit_unavailable";
}
