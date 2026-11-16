using Knock.Contracts.Moderation;
using Knock.Market.Api.Modules.Requests.Dtos;

namespace Knock.Market.Api.Modules.Requests.Services;

/// <summary>
/// Fotos privadas de una solicitud (spec 020, R-04). Cuando el módulo de archivos exponga <c>IMediaApi</c> (spec 013, OI-2), una sola clase de este módulo lo adapta a esta
/// interfaz. Mientras tanto, <see cref="UnavailableRequestPhotos"/> falla cerrado y las pruebas usan dobles.
/// </summary>
internal interface IRequestPhotos
{
    /// <summary>Verdadero solo si todas las fotos existen, son de <paramref name="ownerId"/>, tienen <c>uso = solicitud</c> y están <c>listo</c>. No dice cuál condición falló.</summary>
    Task<bool> AreReadyForAsync(string ownerId, IReadOnlyList<string> photoIds, CancellationToken cancellationToken);

    /// <summary>Enlaces de lectura de 5 minutos (versión reducida) de fotos ya validadas.</summary>
    Task<IReadOnlyList<RequestPhoto>> ReadLinksAsync(IReadOnlyList<string> photoIds, CancellationToken cancellationToken);
}

/// <summary>Sin módulo de archivos todavía: ninguna foto es válida, así que no se crea ninguna solicitud. Cierra el paso en lugar de aceptar fotos que nadie confirmó.</summary>
internal sealed class UnavailableRequestPhotos : IRequestPhotos
{
    public Task<bool> AreReadyForAsync(string ownerId, IReadOnlyList<string> photoIds, CancellationToken cancellationToken) => Task.FromResult(false);

    public Task<IReadOnlyList<RequestPhoto>> ReadLinksAsync(IReadOnlyList<string> photoIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RequestPhoto>>([]);
}

/// <summary>Sin proveedor de moderación configurado: falla cerrado (<c>Unavailable</c>), así que nada se publica sin revisión (spec 013, R-05 y R-06).</summary>
internal sealed class UnavailableContentModerator : IContentModerator
{
    public Task<ModerationResult> ReviewTextAsync(string text, CancellationToken cancellationToken) =>
        Task.FromResult(new ModerationResult(ModerationOutcome.Unavailable));

    public Task<ModerationResult> ReviewImageAsync(ReadOnlyMemory<byte> image, CancellationToken cancellationToken) =>
        Task.FromResult(new ModerationResult(ModerationOutcome.Unavailable));
}
