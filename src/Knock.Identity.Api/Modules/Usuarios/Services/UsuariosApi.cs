using Knock.Identity.Api.Modules.Usuarios.Data;

namespace Knock.Identity.Api.Modules.Usuarios.Services;

/// <summary>Implementación de <see cref="IUsuariosApi"/> sobre el almacén de usuarios: solo expone el identificador y el nombre público.</summary>
internal sealed class UsuariosApi(IUsuariosStore store) : IUsuariosApi
{
    public async Task<PublicUser?> FindPublicUserAsync(string publicId, CancellationToken cancellationToken) =>
        await store.FindProfileAsync(publicId, cancellationToken) is { } profile ? new PublicUser(profile.Id, profile.Name) : null;
}
