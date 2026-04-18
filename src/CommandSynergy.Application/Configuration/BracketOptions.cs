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

    [Range(0, 10)]
    public decimal LowCostAccelerationWeight { get; set; } = 1.5m;

    [Range(0, 10)]
    public decimal HighSaltWeight { get; set; } = 0.5m;

    [Range(0, 10)]
    public decimal HighSaltThreshold { get; set; } = 1.5m;

    public IReadOnlyList<BracketGameChangerOption> GameChangers { get; set; } = Array.Empty<BracketGameChangerOption>();
}

/// <summary>
/// Defines a configured high-impact bracket factor for a known card.
/// </summary>
public sealed class BracketGameChangerOption
{
    /// <summary>
    /// Gets or sets the card or oracle identifier to match.
    /// </summary>
    public required string CardId { get; set; }

    /// <summary>
    /// Gets or sets the category reported in bracket results.
    /// </summary>
    public required string Category { get; set; }

    /// <summary>
    /// Gets or sets the configured weight for the matching card.
    /// </summary>
    [Range(0, 10)]
    public decimal Weight { get; set; }

    /// <summary>
    /// Gets or sets the user-readable explanation for the match.
    /// </summary>
    public required string Explanation { get; set; }
}