namespace Knock.Api.Modules.Media.Data;

/// <summary>
/// Configuración del módulo (sección <c>Media</c> de appsettings, sin secretos). La conexión a Blob llega por
/// <c>ConnectionStrings:KnockBlob</c> desde variables de entorno o user-secrets; en Azure, identidad administrada (TT-AZ-02).
/// </summary>
internal sealed class MediaOptions
{
    public const string SectionName = "Media";
    public const string ConnectionStringName = "KnockBlob";

    /// <summary>Contenedor privado de subidas sin confirmar.</summary>
    public string QuarantineContainer { get; set; } = "cuarentena";

    /// <summary>Contenedor privado definitivo (original recodificado y versión reducida).</summary>
    public string FilesContainer { get; set; } = "archivos";

    /// <summary>Contenedor privado de los registros temporales (<see cref="BlobMediaFileStore"/>) hasta TT-BD-06.</summary>
    public string RecordsContainer { get; set; } = "registros-temporales";

    /// <summary>Lado mayor de la versión reducida, en píxeles.</summary>
    public int ReducedMaxSide { get; set; } = 1024;

    /// <summary>
    /// Fotos que se decodifican a la vez en la réplica (<see cref="Services.ImageProcessor"/>); las demás esperan turno.
    /// Acota el pico de memoria (cálculo en <see cref="Domain.MediaRules.MaxPixels"/>).
    /// </summary>
    public int MaxConcurrentProcessing { get; set; } = 2;
}
