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

    public required IReadOnlyList<decimal> LevelThresholds { get; set; }

    [Range(0, 10)]
    public decimal LowCostAccelerationWeight { get; set; } = 1.5m;

    [Range(0, 10)]
    public decimal HighSaltWeight { get; set; } = 0.5m;

    [Range(0, 10)]
    public decimal HighSaltThreshold { get; set; } = 1.5m;

    [Range(0, 10)]
    public decimal GameChangerWeight { get; set; } = 5.0m;

    [Range(0, 10)]
    public decimal MassLandDenialWeight { get; set; } = 3.0m;
}