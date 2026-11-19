using Knock.Contracts.Moderation;

namespace Knock.Identity.Api.Modules.Safety.Services;

/// <summary>
/// Moderación de contenido mientras no exista la de la spec 031 en Identidad: responde siempre «no disponible», así que un reporte no se guarda sin revisión (falla cerrado, constitución §2.13).
/// Las pruebas la sustituyen por un doble.
/// </summary>
internal sealed class UnavailableContentModerator : IContentModerator
{
    private static readonly Task<ModerationResult> _notAvailable = Task.FromResult(new ModerationResult(ModerationOutcome.Unavailable));

    public Task<ModerationResult> ReviewTextAsync(string text, CancellationToken cancellationToken) => _notAvailable;

    public Task<ModerationResult> ReviewImageAsync(ReadOnlyMemory<byte> image, CancellationToken cancellationToken) => _notAvailable;
}
