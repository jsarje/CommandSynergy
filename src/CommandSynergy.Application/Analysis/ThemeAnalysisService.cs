using CommandSynergy.Application.Abstractions;
using CommandSynergy.Domain.Analysis;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Decks;

namespace CommandSynergy.Application.Analysis;

/// <summary>
/// Aggregates deck-level theme analysis and thematic scoring.
/// </summary>
public sealed class ThemeAnalysisService : IThemeAnalysisService
{
    private const int MinimumAnalysisCardCount = 20;
    private const decimal OffThemeSignalThreshold = 0.10m;

    private readonly IThemeMatchingService themeMatchingService;
    private readonly IAnalysisExplanationBuilder explanationBuilder;

    /// <summary>
    /// Creates a theme-analysis service.
    /// </summary>
    public ThemeAnalysisService(IThemeMatchingService themeMatchingService, IAnalysisExplanationBuilder explanationBuilder)
    {
        this.themeMatchingService = themeMatchingService;
        this.explanationBuilder = explanationBuilder;
    }

    /// <summary>
    /// Analyses a deck and computes theme results plus enhanced synergy scoring.
    /// </summary>
    public Task<(ThemeAnalysis Analysis, SynergyAssessment Synergy)> AnalyseAsync(
        Deck deck,
        IReadOnlyDictionary<string, CardProfile> cardProfiles,
        CommanderThemeInsights? edhrecInsights,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(deck);
        ArgumentNullException.ThrowIfNull(cardProfiles);
        cancellationToken.ThrowIfCancellationRequested();

        var analysisTime = DateTimeOffset.UtcNow;
        var totalCardCount = deck.Entries.Sum(static entry => entry.Quantity);
        if (totalCardCount < MinimumAnalysisCardCount)
        {
            var insufficientAnalysis = new ThemeAnalysis(
                Array.Empty<DeckTheme>(),
                Array.Empty<DeckTheme>(),
                Array.Empty<OffThemeCard>(),
                new CommanderAlignment(AlignmentLevel.None, null, 0m, Array.Empty<string>(), "Add more cards to reveal a deck theme."),
                totalCardCount,
                true,
                analysisTime,
                false,
                "Theme analysis is waiting for a larger sample size.");

            var insufficientSynergy = new SynergyAssessment(
                0m,
                Array.Empty<string>(),
                Array.Empty<string>(),
                "Add more cards to evaluate thematic coherence.",
                analysisTime,
                0m,
                0m,
                "Pile",
                false);

            return Task.FromResult((insufficientAnalysis, insufficientSynergy));
        }

        var weightedTotal = 0m;
        var aggregateThemeStrengths = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in deck.Entries)
        {
            var entryWeight = entry.IsCommander ? 3m : entry.Quantity;
            weightedTotal += entryWeight;

            if (!cardProfiles.TryGetValue(entry.CardId, out var profile))
            {
                continue;
            }

            var signals = profile.ThemeSignals.Count > 0
                ? profile.ThemeSignals
                : themeMatchingService.ComputeThemeSignals(profile);

            foreach (var pair in signals)
            {
                aggregateThemeStrengths[pair.Key] = aggregateThemeStrengths.GetValueOrDefault(pair.Key) + (pair.Value * entryWeight);
            }
        }

        var rankedThemes = BuildDeckThemes(deck, cardProfiles, aggregateThemeStrengths, weightedTotal);
        var primaryThemes = rankedThemes.Where(static theme => theme.Strength >= 0.15m).ToArray();
        var commanderAlignment = DetermineCommanderAlignment(deck, cardProfiles, rankedThemes);
        var offThemeCards = CollectOffThemeCards(deck, cardProfiles);
        var themeScore = ComputeThemeScore(rankedThemes, offThemeCards.Count, totalCardCount, commanderAlignment);
        var finalScore = ApplyEdhrecBlend(themeScore, deck, edhrecInsights);
        var qualitativeLabel = explanationBuilder.DetermineQualitativeLabel(finalScore);
        var summary = explanationBuilder.BuildThemeSummary(themeScore, finalScore, qualitativeLabel, commanderAlignment, offThemeCards.Count, totalCardCount, edhrecInsights?.IsAvailable == true);

        var analysis = new ThemeAnalysis(
            rankedThemes,
            primaryThemes,
            offThemeCards,
            commanderAlignment,
            totalCardCount,
            false,
            analysisTime,
            edhrecInsights?.IsAvailable != true,
            explanationBuilder.BuildThemeRefreshSummary(primaryThemes, offThemeCards.Count, totalCardCount));

        var synergy = new SynergyAssessment(
            decimal.Round(finalScore, 1, MidpointRounding.AwayFromZero),
            Array.Empty<string>(),
            Array.Empty<string>(),
            summary,
            analysisTime,
            decimal.Round(themeScore, 1, MidpointRounding.AwayFromZero),
            decimal.Round(finalScore, 1, MidpointRounding.AwayFromZero),
            qualitativeLabel,
            edhrecInsights?.IsAvailable == true);

        return Task.FromResult((analysis, synergy));
    }

    private IReadOnlyList<DeckTheme> BuildDeckThemes(
        Deck deck,
        IReadOnlyDictionary<string, CardProfile> cardProfiles,
        IReadOnlyDictionary<string, decimal> aggregateThemeStrengths,
        decimal weightedTotal)
    {
        if (weightedTotal <= 0m)
        {
            return Array.Empty<DeckTheme>();
        }

        return aggregateThemeStrengths
            .Where(static pair => pair.Value > 0m)
            .Select(pair =>
            {
                var definition = ThemeTaxonomy.GetByName(pair.Key)!;
                var contributors = deck.Entries
                    .Where(entry => cardProfiles.TryGetValue(entry.CardId, out var profile) && ResolveSignals(profile).TryGetValue(pair.Key, out var signal) && signal > 0m)
                    .Select(entry =>
                    {
                        var profile = cardProfiles[entry.CardId];
                        var signal = ResolveSignals(profile)[pair.Key];
                        return new ThemeContributor(
                            entry.CardId,
                            profile.Name,
                            signal,
                            themeMatchingService.DescribeMatch(profile, pair.Key));
                    })
                    .OrderByDescending(contributor => contributor.Signal)
                    .ThenBy(contributor => contributor.CardName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var strength = decimal.Round(pair.Value / weightedTotal, 2, MidpointRounding.AwayFromZero);

                return new DeckTheme(
                    pair.Key,
                    definition.Description,
                    strength,
                    strength >= 0.50m ? "Strong" : strength >= 0.25m ? "Moderate" : "Supporting",
                    contributors.Select(static contributor => contributor.CardId).ToArray(),
                    contributors.Length,
                    contributors,
                    definition.SignalConfidence);
            })
            .OrderByDescending(theme => theme.Strength)
            .ThenBy(theme => theme.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        IReadOnlyDictionary<string, decimal> ResolveSignals(CardProfile profile) =>
            profile.ThemeSignals.Count > 0 ? profile.ThemeSignals : themeMatchingService.ComputeThemeSignals(profile);
    }

    private CommanderAlignment DetermineCommanderAlignment(
        Deck deck,
        IReadOnlyDictionary<string, CardProfile> cardProfiles,
        IReadOnlyList<DeckTheme> rankedThemes)
    {
        var commanderEntry = deck.Entries.SingleOrDefault(static entry => entry.IsCommander);
        if (commanderEntry is null || !cardProfiles.TryGetValue(commanderEntry.CardId, out var commanderProfile))
        {
            return new CommanderAlignment(AlignmentLevel.None, null, 0m, Array.Empty<string>(), "Choose a commander to anchor theme alignment.");
        }

        var commanderSignals = commanderProfile.ThemeSignals.Count > 0
            ? commanderProfile.ThemeSignals
            : themeMatchingService.ComputeThemeSignals(commanderProfile);
        var topCommanderTheme = commanderSignals.OrderByDescending(static pair => pair.Value).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(topCommanderTheme.Key) || topCommanderTheme.Value <= 0m)
        {
            return new CommanderAlignment(AlignmentLevel.None, null, 0m, Array.Empty<string>(), "The commander does not expose a strong local theme signal.");
        }

        var deckTheme = rankedThemes.FirstOrDefault(theme => string.Equals(theme.Name, topCommanderTheme.Key, StringComparison.OrdinalIgnoreCase));
        var deckStrength = deckTheme?.Strength ?? 0m;
        var level = deckStrength >= 0.50m
            ? AlignmentLevel.Strong
            : deckStrength >= 0.25m
                ? AlignmentLevel.Moderate
                : AlignmentLevel.Low;

        var evidence = deckTheme?.Contributors.Select(static contributor => contributor.CardId).Take(5).ToArray() ?? Array.Empty<string>();
        var summary = level switch
        {
            AlignmentLevel.Strong => "The 99 strongly reinforce the commander's plan.",
            AlignmentLevel.Moderate => "The deck broadly supports the commander's main plan.",
            _ => "The deck's support package only weakly reflects the commander's plan.",
        };

        return new CommanderAlignment(level, topCommanderTheme.Key, deckStrength, evidence, summary);
    }

    private IReadOnlyList<OffThemeCard> CollectOffThemeCards(Deck deck, IReadOnlyDictionary<string, CardProfile> cardProfiles) =>
        deck.Entries
            .Where(static entry => !entry.IsCommander)
            .Select(entry =>
            {
                if (!cardProfiles.TryGetValue(entry.CardId, out var profile))
                {
                    return new OffThemeCard(entry.CardId, entry.CardId, "Metadata was unavailable during analysis.", true);
                }

                var signals = profile.ThemeSignals.Count > 0 ? profile.ThemeSignals : themeMatchingService.ComputeThemeSignals(profile);
                if (signals.Count == 0 || signals.Max(static pair => pair.Value) <= OffThemeSignalThreshold)
                {
                    return new OffThemeCard(entry.CardId, profile.Name, "No strong theme signal was detected for this card.", false);
                }

                return null;
            })
            .Where(static card => card is not null)
            .Cast<OffThemeCard>()
            .OrderBy(card => card.CardName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static decimal ComputeThemeScore(
        IReadOnlyList<DeckTheme> rankedThemes,
        int offThemeCardCount,
        int totalCardCount,
        CommanderAlignment alignment)
    {
        if (rankedThemes.Count == 0 || totalCardCount <= 0)
        {
            return 0m;
        }

        var top = rankedThemes[0].Strength;
        var second = rankedThemes.Count > 1 ? rankedThemes[1].Strength : 0m;
        var onThemeShare = 1m - ((decimal)offThemeCardCount / totalCardCount);
        var alignmentBonus = alignment.Level switch
        {
            AlignmentLevel.Strong => 10m,
            AlignmentLevel.Moderate => 6m,
            AlignmentLevel.Low => 2m,
            _ => 0m,
        };

        var score = (top * 55m) + (second * 20m) + (onThemeShare * 20m) + alignmentBonus - (((decimal)offThemeCardCount / totalCardCount) * 20m);
        return Math.Clamp(score, 0m, 100m);
    }

    private static decimal ApplyEdhrecBlend(decimal themeScore, Deck deck, CommanderThemeInsights? edhrecInsights)
    {
        if (edhrecInsights is null || !edhrecInsights.IsAvailable || edhrecInsights.SynergyByCardId.Count == 0)
        {
            return themeScore;
        }

        var mappedScores = deck.Entries
            .Where(static entry => !entry.IsCommander)
            .Select(entry => edhrecInsights.SynergyByCardId.TryGetValue(entry.CardId, out var score) ? score : decimal.MinValue)
            .Where(static score => score != decimal.MinValue)
            .ToArray();

        if (mappedScores.Length == 0)
        {
            return themeScore;
        }

        var edhrecAverage = mappedScores.Average(score => ((double)score + 1d) * 50d);
        return Math.Clamp((themeScore * 0.7m) + ((decimal)edhrecAverage * 0.3m), 0m, 100m);
    }
}
