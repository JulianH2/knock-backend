namespace Knock.Market.Api.Modules.Media;

/// <summary>
/// Public module contract: the only way for other modules or services to use private photos.
/// Read-link SAS generation is OI-3, so in OI-2 this exposes ownership checks only.
/// </summary>
public interface IMediaApi
{
    /// <summary>Whether the file exists, is Ready and its owner is <paramref name="ownerId"/> (the JWT <c>sub</c>).</summary>
    Task<bool> IsReadyAndOwnedByAsync(Guid fileId, string ownerId, CancellationToken cancellationToken);
}
