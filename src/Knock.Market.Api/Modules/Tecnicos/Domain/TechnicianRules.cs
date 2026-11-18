namespace Knock.Market.Api.Modules.Tecnicos.Domain;

/// <summary>Valores cerrados del perfil del técnico (spec 046, §6 y R-39).</summary>
internal static class TechnicianRules
{
    public const string Taller = "taller";
    public const string Domicilio = "domicilio";
    public const string Ambos = "ambos";

    /// <summary>Estado con el que nace el perfil: el que lee la elegibilidad (spec 028, D-04).</summary>
    public const string ActiveState = "activo";

    /// <summary>Sin verificación de identidad todavía (spec 025): la insignia es opcional. Mismo valor que usa la comparación de cotizaciones.</summary>
    public const string NoVerification = "ninguno";

    /// <summary>409 de TEC-01 (spec 046, §5).</summary>
    public const string AlreadyTechnicianCode = "already_a_technician";

    /// <summary>
    /// Tope técnico de claves en el cuerpo para acotar la entrada; la regla de negocio no tiene máximo (D-07) y el catálogo es más corto que esto.
    /// </summary>
    public const int MaxTradesInBody = 50;

    public static readonly IReadOnlySet<int> Radii = new HashSet<int> { 3, 5, 8, 12 };

    public static bool IsKnownAttention(string? attention) => attention is Taller or Domicilio or Ambos;

    public static bool UsesWorkshop(string? attention) => attention is Taller or Ambos;

    public static bool VisitsHomes(string? attention) => attention is Domicilio or Ambos;
}
