using System.Threading.RateLimiting;

namespace Knock.Identity.Api.Modules.Account.Services;

/// <summary>
/// Límite de la recuperación de acceso por número (spec 039, R-21: 3 por hora por IP y por número). La IP la limita el limitador del framework
/// antes de llegar al controlador; el número solo se conoce al verificar el token de Firebase, así que se cuenta aquí. Vive en la memoria del proceso,
/// como los demás límites de Identidad: con varias réplicas el límite efectivo se multiplica. La clave es el HMAC del número, nunca el número.
/// </summary>
internal interface IRecoveryAttemptLimiter
{
    /// <summary>Cuenta un intento para <paramref name="key"/>; falso si ya pasó el límite de la ventana, con el tiempo que falta para el siguiente.</summary>
    bool TryAcquire(string key, out TimeSpan retryAfter);
}

/// <summary>Ventana fija de 3 intentos por hora por clave; las particiones inactivas las libera el propio limitador.</summary>
internal sealed class RecoveryAttemptLimiter : IRecoveryAttemptLimiter, IDisposable
{
    public const int PermitLimit = 3;

    public static readonly TimeSpan Window = TimeSpan.FromHours(1);

    private readonly PartitionedRateLimiter<string> _limiter = PartitionedRateLimiter.Create<string, string>(key =>
        RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = PermitLimit,
            Window = Window,
            QueueLimit = 0,
            AutoReplenishment = true,
        }));

    public bool TryAcquire(string key, out TimeSpan retryAfter)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        using var lease = _limiter.AttemptAcquire(key);
        if (lease.IsAcquired)
        {
            retryAfter = TimeSpan.Zero;
            return true;
        }

        retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out var hint) ? hint : Window;
        return false;
    }

    public void Dispose() => _limiter.Dispose();
}
