using System.ComponentModel.DataAnnotations;

namespace Knock.Market.Api.Modules.Media.Data;

/// <summary>Module configuration (<c>Media</c> section of appsettings, no secrets).</summary>
internal sealed class MediaOptions
{
    public const string SectionName = "Media";
    public const string ConnectionStringName = "KnockBlob";

    [Required] public string QuarantineContainer { get; set; } = "cuarentena";
    [Required] public string FilesContainer { get; set; } = "archivos";
    [Required] public string RecordsContainer { get; set; } = "registros-temporales";
    [Range(1, int.MaxValue)] public int ReducedMaxSide { get; set; } = 1600;
    [Range(1, int.MaxValue)] public int ProfileReducedMaxSide { get; set; } = 512;
    [Range(1, int.MaxValue)] public int MaxConcurrentProcessing { get; set; } = 1;
}
