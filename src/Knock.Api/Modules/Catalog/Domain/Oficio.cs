namespace Knock.Api.Modules.Catalog.Domain;

/// <summary>Oficio del catálogo como dato sembrado (no enum): clave estable, nombre, modalidad y estado.</summary>
internal sealed class Oficio
{
    public Guid Id { get; init; }

    public required string Clave { get; init; }

    public required string Nombre { get; init; }

    /// <summary><c>domicilio</c> o <c>taller</c>.</summary>
    public required string Modalidad { get; init; }

    public bool Activo { get; init; }
}
