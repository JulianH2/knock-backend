using Knock.Api.Modules.Identity.Domain;

namespace Knock.Api.Modules.Identity.Data;

/// <summary>
/// Lo que el perfil (HU-002) necesita de la base de identidad. Es una interfaz aparte de <see cref="IIdentityStore"/> para no tocar el contrato del
/// canje ni los dobles que ya lo implementan.
/// </summary>
internal interface IProfileStore
{
    /// <returns>El perfil de la cuenta, o <c>null</c> si no existe (sin importar su estado).</returns>
    Task<UserProfile?> FindProfileAsync(string sub, CancellationToken cancellationToken);

    /// <summary>
    /// Guarda el nombre ya normalizado solo si la cuenta está activa: pone <c>perfilCompleto</c> en <c>true</c> y sube <c>version</c> únicamente si el
    /// contenido cambió.
    /// </summary>
    /// <returns>
    /// El perfil resultante; si la cuenta no está activa devuelve su perfil <b>sin modificarlo</b> (el servicio responde 403); <c>null</c> si no existe.
    /// </returns>
    Task<UserProfile?> SaveNameAsync(string sub, string name, CancellationToken cancellationToken);
}
