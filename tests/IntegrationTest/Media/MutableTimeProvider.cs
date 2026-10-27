namespace Knock.IntegrationTest.Media;

/// <summary><see cref="TimeProvider"/> falso: la hora real más un desfase que la prueba mueve (sin esperas reales).</summary>
internal sealed class MutableTimeProvider : TimeProvider
{
    public TimeSpan Offset { get; set; }

    public override DateTimeOffset GetUtcNow() => System.GetUtcNow() + Offset;
}
