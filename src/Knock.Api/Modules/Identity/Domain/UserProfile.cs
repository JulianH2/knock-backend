namespace Knock.Api.Modules.Identity.Domain;

/// <summary>Perfil de una cuenta tal como lo necesitan <c>GET /me</c> y <c>PUT /me</c> (spec HU-002 §5): nunca el celular ni su hash.</summary>
/// <param name="Sub">Identificador público de la cuenta (el <c>sub</c> del JWT).</param>
/// <param name="State"><see cref="UserState"/>.</param>
/// <param name="Name">Nombre normalizado, o <c>null</c> mientras no lo haya puesto.</param>
/// <param name="ProfileComplete">En esta versión, si la persona ya tiene nombre.</param>
internal sealed record UserProfile(string Sub, string State, string? Name, bool ProfileComplete);
