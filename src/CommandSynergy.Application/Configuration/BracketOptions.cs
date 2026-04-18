using System.ComponentModel.DataAnnotations;

namespace CommandSynergy.Application.Configuration;

/// <summary>
/// Configures baseline bracket thresholds and weighting defaults for analysis services.
/// </summary>
public sealed class BracketOptions
{
    public const string SectionName = "Bracket";

    [Range(1, 5)]
    public int MinimumBracketLevel { get; set; } = 1;

    [Range(1, 5)]
    public int MaximumBracketLevel { get; set; } = 5;

    public IReadOnlyList<decimal> LevelThresholds { get; set; } = new[] { 0m, 5m, 10m, 15m, 20m };
}