using System.ComponentModel.DataAnnotations;

namespace CommandSynergy.Application.Configuration;

/// <summary>
/// Configures the authoritative server-side metadata snapshot source and derived search artifact.
/// </summary>
public sealed class CardMetadataOptions
{
    public const string SectionName = "CardMetadata";

    [Required]
    public string SnapshotDirectory { get; set; } = "Data/CardMetadata";

    [Required]
    public string SnapshotFileName { get; set; } = "cards.parquet";

    [Required]
    public string SearchIndexVersion { get; set; } = "v1";
}