namespace Knock.Api.Modules.Catalog.Domain;

/// <summary>Reglas del oficio del catálogo (dato sembrado, no enum): clave estable, nombre y modalidad.</summary>
internal static class Oficio
{
    public const int MaxClaveLength = 16;
    public const int MaxNombreLength = 100;

    /// <summary>Equivale al antiguo <c>CHECK</c> de PostgreSQL; hoy la única barrera es el cargador (spec §5).</summary>
    public static bool IsValidModalidad(string? modalidad) => modalidad is "domicilio" or "taller";
}
