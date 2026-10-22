namespace Knock.Api.Modules.Media.Services;

internal sealed class MediaApi(MediaService media) : IMediaApi
{
    public string ModuleName => "Media";

    public Task<bool> IsReadyAndOwnedByAsync(Guid fileId, string ownerId, CancellationToken cancellationToken) =>
        media.IsReadyAndOwnedByAsync(fileId, ownerId, cancellationToken);

    public async Task<(Uri Url, DateTimeOffset ExpiresAt)?> CreateReadLinkAsync(Guid fileId, bool reduced, CancellationToken cancellationToken) =>
        await media.CreateAuthorizedReadLinkAsync(fileId, reduced, cancellationToken) is { } link ? (link.Url, link.ExpiresAt) : null;
}
