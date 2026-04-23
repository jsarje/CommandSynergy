using CommandSynergy.Domain.Analysis;
using CommandSynergy.Domain.Cards;

namespace CommandSynergy.Application.Analysis;

/// <summary>
/// Computes per-card theme signals from card metadata.
/// </summary>
public sealed class ThemeMatchingService : IThemeMatchingService
{
    /// <summary>
    /// Computes normalized theme signals for a card profile.
    /// </summary>
    public IReadOnlyDictionary<string, decimal> ComputeThemeSignals(CardProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var signals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var oracleText = BuildOracleCorpus(profile);
        var typeLine = BuildTypeCorpus(profile);

        foreach (var theme in ThemeTaxonomy.Default.Where(static theme => !string.Equals(theme.Name, "Goodstuff", StringComparison.OrdinalIgnoreCase)))
        {
            var score = 0m;

            if (profile.Keywords.Count > 0)
            {
                score += profile.Keywords.Count(keyword => theme.KeywordPatterns.Any(pattern => string.Equals(pattern, keyword, StringComparison.OrdinalIgnoreCase))) * theme.KeywordWeight;
            }

            score += theme.OracleTextPatterns.Count(pattern => pattern.IsMatch(oracleText)) * theme.OracleTextWeight;

            if (theme.TypePattern is not null && theme.TypePattern.IsMatch(typeLine))
            {
                score += theme.TypeWeight;
            }

            var normalizedScore = decimal.Round(Math.Clamp(score, 0m, 1m), 2, MidpointRounding.AwayFromZero);
            if (normalizedScore > 0m)
            {
                signals[theme.Name] = normalizedScore;
            }
        }

        return signals;
    }

    /// <summary>
    /// Builds a short explanation for why a card contributed to a theme.
    /// </summary>
    public string DescribeMatch(CardProfile profile, string themeName)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrWhiteSpace(themeName);

        var definition = ThemeTaxonomy.GetByName(themeName);
        if (definition is null)
        {
            return "Matched the deck's detected strategy.";
        }

        var oracleText = BuildOracleCorpus(profile);
        var typeLine = BuildTypeCorpus(profile);

        if (profile.Keywords.Any(keyword => definition.KeywordPatterns.Any(pattern => string.Equals(pattern, keyword, StringComparison.OrdinalIgnoreCase))))
        {
            return "Matched a theme keyword on the card.";
        }

        if (definition.OracleTextPatterns.Any(pattern => pattern.IsMatch(oracleText)))
        {
            return "Matched the card's oracle text.";
        }

        if (definition.TypePattern is not null && definition.TypePattern.IsMatch(typeLine))
        {
            return "Matched the card's type line.";
        }

        return "Reinforces the detected theme.";
    }

    private static string BuildOracleCorpus(CardProfile profile)
    {
        var faceOracleText = profile.FaceProfiles
            .Where(static face => !string.IsNullOrWhiteSpace(face.OracleText))
            .Select(static face => face.OracleText)
            .OfType<string>();

        return string.Join('\n', new[] { profile.OracleText }.Concat(faceOracleText).Where(static value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string BuildTypeCorpus(CardProfile profile)
    {
        var faceTypes = profile.FaceProfiles.Select(static face => face.TypeLine);
        return string.Join('\n', new[] { profile.TypeLine }.Concat(faceTypes).Where(static value => !string.IsNullOrWhiteSpace(value)));
    }
}
