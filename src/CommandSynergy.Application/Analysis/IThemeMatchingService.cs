using CommandSynergy.Domain.Cards;

namespace CommandSynergy.Application.Analysis;

/// <summary>
/// Computes per-card theme signals from card metadata.
/// </summary>
public interface IThemeMatchingService
{
    /// <summary>
    /// Computes normalized theme signals for a card profile.
    /// </summary>
    IReadOnlyDictionary<string, decimal> ComputeThemeSignals(CardProfile profile);

    /// <summary>
    /// Builds a short explanation for why a card contributed to a theme.
    /// </summary>
    string DescribeMatch(CardProfile profile, string themeName);
}
