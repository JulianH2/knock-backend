namespace Knock.Identity.Api.Modules.Usuarios;

/// <summary>Una persona tal como la ven los demás módulos: su identificador público (el <c>sub</c>) y su nombre público, nada más (spec 043, R-19).</summary>
public sealed record PublicUser(string Id, string Name);

/// <summary>
/// Lo que el módulo de usuarios ofrece a los demás módulos de Identidad sin que lean su colección ni sus servicios (spec 002): saber si una persona existe y cómo se llama en público.
/// </summary>
public interface IUsuariosApi
{
    /// <summary>La persona de ese identificador público (su <c>sub</c>), o <see langword="null"/> si no existe. El nombre puede estar vacío mientras no haya completado su perfil.</summary>
    Task<PublicUser?> FindPublicUserAsync(string publicId, CancellationToken cancellationToken);
}
