using Knock.Market.Api.Modules.Media.Services;

namespace Knock.Market.Api.Modules.Media.Services;

/// <summary>Public module facade over the internal media service.</summary>
internal sealed class MediaApi(MediaService media) : IMediaApi
{
    public Task<bool> IsReadyAndOwnedByAsync(Guid fileId, string ownerId, CancellationToken cancellationToken) =>
        media.IsReadyAndOwnedByAsync(fileId, ownerId, cancellationToken);
}
