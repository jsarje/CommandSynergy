using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Domain.Analysis;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Decks;

namespace CommandSynergy.Application.Analysis;

/// <summary>
/// Orchestrates authoritative bracket and synergy analysis for a deck snapshot.
/// </summary>
public sealed class DeckAnalysisService
{
    private static readonly DeckStatsCalculationService deckStatsCalculationService = new();
    private readonly ICardCatalogGateway cardCatalogGateway;
    private readonly ICommanderSpellbookClient commanderSpellbookClient;
    private readonly IEdhrecClient edhrecClient;
    private readonly BracketCalculationService bracketCalculationService;
    private readonly PowerLevelCalculationService powerLevelCalculationService;
    private readonly SynergyScoringService synergyScoringService;
    private readonly ThemeAnalysisService themeAnalysisService;
    private readonly IReadOnlyList<IDeckAdviceService> deckAdviceServices;

    /// <summary>
    /// Creates a deck-analysis service.
    /// </summary>
    public DeckAnalysisService(
        ICardCatalogGateway cardCatalogGateway,
        ICommanderSpellbookClient commanderSpellbookClient,
        IEdhrecClient edhrecClient,
        BracketCalculationService bracketCalculationService,
        PowerLevelCalculationService powerLevelCalculationService,
        SynergyScoringService synergyScoringService,
        ThemeAnalysisService themeAnalysisService,
        IEnumerable<IDeckAdviceService> deckAdviceServices)
    {
        this.cardCatalogGateway = cardCatalogGateway;
        this.commanderSpellbookClient = commanderSpellbookClient;
        this.edhrecClient = edhrecClient;
        this.bracketCalculationService = bracketCalculationService;
        this.powerLevelCalculationService = powerLevelCalculationService;
        this.synergyScoringService = synergyScoringService;
        this.themeAnalysisService = themeAnalysisService;
        this.deckAdviceServices = deckAdviceServices.ToArray();
    }

    /// <summary>
    /// Calculates the current bracket and synergy results for the supplied deck snapshot.
    /// </summary>
    public async Task<DeckAnalysisResponseContract> AnalyzeAsync(DeckSnapshotContract deckSnapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(deckSnapshot);

        var deck = BuildDeck(deckSnapshot);
        var cardIds = deck.Entries.Select(entry => entry.CardId).Distinct(StringComparer.OrdinalIgnoreCase);
        var profiles = await cardCatalogGateway.GetCardProfilesAsync(cardIds, cancellationToken).ConfigureAwait(false);

        var bracketAssessment = bracketCalculationService.Calculate(deck, profiles);
        var baseSynergyAssessment = synergyScoringService.Calculate(deck, profiles);
        var commanderEntry = deck.Entries.SingleOrDefault(static entry => entry.IsCommander);
        var commanderProfile = commanderEntry is not null && profiles.TryGetValue(commanderEntry.CardId, out var profile)
            ? profile
            : null;
        var commanderNames = deck.Entries
            .Where(static entry => entry.IsCommander)
            .Select(entry => profiles.TryGetValue(entry.CardId, out var profile) ? profile.Name : null)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToArray();
        var mainDeckNames = deck.Entries
            .Where(static entry => !entry.IsCommander)
            .Select(entry => profiles.TryGetValue(entry.CardId, out var profile) ? profile : null)
            .Where(static profile => profile is not null && !profile.IsLand)
            .Select(static profile => profile!.Name)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToArray();

        var comboAnalysisTask = commanderSpellbookClient.FindCombosAsync(commanderNames, mainDeckNames, cancellationToken);
        var edhrecInsightsTask = commanderProfile is null
            ? Task.FromResult(CommanderThemeInsights.Empty())
            : edhrecClient.GetCommanderThemeInsightsAsync(commanderProfile, cancellationToken);
        await Task.WhenAll(comboAnalysisTask, edhrecInsightsTask).ConfigureAwait(false);

        var comboAnalysis = await comboAnalysisTask.ConfigureAwait(false);
        var edhrecInsights = await edhrecInsightsTask.ConfigureAwait(false);
        var (themeAnalysis, themeSynergy) = await themeAnalysisService.AnalyseAsync(deck, profiles, edhrecInsights, cancellationToken).ConfigureAwait(false);
        var synergyAssessment = MergeSynergyAssessments(baseSynergyAssessment, themeSynergy);
        var powerLevelAssessment = powerLevelCalculationService.Calculate(deck, profiles, comboAnalysis);
        var deckStats = deckStatsCalculationService.Calculate(deck, profiles);

        var response = new DeckAnalysisResponseContract
        {
            Bracket = MapBracket(bracketAssessment),
            PowerLevel = powerLevelAssessment,
            Synergy = MapSynergy(synergyAssessment),
            ThemeAnalysis = MapThemeAnalysis(themeAnalysis),
            ComboAnalysis = MapComboAnalysis(comboAnalysis),
            DeckStats = deckStats,
        };

        foreach (var deckAdviceService in deckAdviceServices)
        {
            response = await deckAdviceService.EnrichAsync(deckSnapshot, response, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }

    private static Deck BuildDeck(DeckSnapshotContract deckSnapshot)
    {
        var deck = new Deck(deckSnapshot.DeckId, deckSnapshot.Name);

        foreach (var pile in deckSnapshot.Piles)
        {
            deck.AddPile(pile.PileId, pile.Name, pile.SortOrder);
        }

        foreach (var entry in deckSnapshot.Entries)
        {
            deck.UpsertEntry(entry.CardId, entry.Quantity, entry.AssignedPileId, entry.IsCommander, entry.IsCompanion);
        }

        if (!string.IsNullOrWhiteSpace(deckSnapshot.CommanderCardId)
            && deckSnapshot.Entries.All(entry => !entry.IsCommander))
        {
            deck.SetCommander(deckSnapshot.CommanderCardId);
        }

        if (!string.IsNullOrWhiteSpace(deckSnapshot.CompanionCardId) && deckSnapshot.Entries.All(entry => !entry.IsCompanion))
        {
            deck.SetCompanion(deckSnapshot.CompanionCardId);
        }

        return deck;
    }

    private static BracketAssessmentContract MapBracket(BracketAssessment assessment) => new()
    {
        Level = assessment.BracketLevel,
        TotalWeight = assessment.TotalWeight,
        Summary = assessment.Summary,
        Factors = assessment.ContributingFactors.Select(factor => new BracketFactorContract
        {
            SourceCardId = factor.SourceCardId,
            Category = factor.Category,
            Weight = factor.Weight,
            Explanation = factor.Explanation,
        }).ToArray(),
    };

    private static SynergyAssessmentContract MapSynergy(SynergyAssessment assessment) => new()
    {
        Score = assessment.SynergyScore,
        ThemeScore = assessment.ThemeScore,
        FinalScore = assessment.FinalScore == 0m ? assessment.SynergyScore : assessment.FinalScore,
        QualitativeLabel = assessment.QualitativeLabel,
        EdhrecEnhanced = assessment.EdhrecEnhanced,
        Summary = assessment.Summary,
        CommanderSpecificHits = assessment.CommanderSpecificHits,
        StapleOverloadIndicators = assessment.StapleOverloadIndicators,
    };

    private static SynergyAssessment MergeSynergyAssessments(SynergyAssessment baseAssessment, SynergyAssessment themeAssessment) => new(
        themeAssessment.FinalScore == 0m ? baseAssessment.SynergyScore : themeAssessment.FinalScore,
        baseAssessment.CommanderSpecificHits,
        baseAssessment.StapleOverloadIndicators,
        themeAssessment.Summary,
        themeAssessment.CalculatedUtc,
        themeAssessment.ThemeScore,
        themeAssessment.FinalScore == 0m ? baseAssessment.SynergyScore : themeAssessment.FinalScore,
        themeAssessment.QualitativeLabel,
        themeAssessment.EdhrecEnhanced);

    private static ComboAnalysisContract MapComboAnalysis(ComboAnalysis analysis) => new()
    {
        IncludedCombos = analysis.IncludedCombos.Select(MapComboResult).ToArray(),
        AlmostIncludedCombos = analysis.AlmostIncludedCombos.Select(MapComboResult).ToArray(),
        MissingOneCount = analysis.MissingOneCount,
        AnalysedAtUtc = analysis.AnalysedAtUtc,
    };

    private static ComboResultContract MapComboResult(ComboResult combo) => new()
    {
        CardNames = combo.CardNames,
        Produces = combo.Produces,
        Steps = combo.Steps,
        Prerequisites = combo.Prerequisites,
    };

    private static ThemeAnalysisContract MapThemeAnalysis(ThemeAnalysis analysis) => new()
    {
        RankedThemes = analysis.RankedThemes.Select(MapDeckTheme).ToArray(),
        PrimaryThemes = analysis.PrimaryThemes.Select(MapDeckTheme).ToArray(),
        OffThemeCards = analysis.OffThemeCards.Select(card => new OffThemeCardContract
        {
            CardId = card.CardId,
            CardName = card.CardName,
            Reason = card.Reason,
            MetadataUnavailable = card.MetadataUnavailable,
        }).ToArray(),
        CommanderAlignment = new CommanderAlignmentContract
        {
            Level = analysis.CommanderAlignment.Level.ToString(),
            CommanderTopTheme = analysis.CommanderAlignment.CommanderTopTheme,
            DeckStrengthForCommanderTheme = analysis.CommanderAlignment.DeckStrengthForCommanderTheme,
            EvidenceCardIds = analysis.CommanderAlignment.EvidenceCardIds,
            Summary = analysis.CommanderAlignment.Summary,
        },
        AnalysedCardCount = analysis.AnalysedCardCount,
        IsInsufficient = analysis.IsInsufficient,
        AnalysedAtUtc = analysis.AnalysedAtUtc,
        UsedEdhrecFallback = analysis.UsedEdhrecFallback,
        RefreshSummary = analysis.RefreshSummary,
    };

    private static DeckThemeContract MapDeckTheme(DeckTheme theme) => new()
    {
        Name = theme.Name,
        Description = theme.Description,
        Strength = theme.Strength,
        StrengthLabel = theme.StrengthLabel,
        ContributingCardIds = theme.ContributingCardIds,
        ContributingCardCount = theme.ContributingCardCount,
        Contributors = theme.Contributors.Select(contributor => new ThemeContributorContract
        {
            CardId = contributor.CardId,
            CardName = contributor.CardName,
            Signal = contributor.Signal,
            Reason = contributor.Reason,
        }).ToArray(),
        SignalConfidence = theme.SignalConfidence,
    };
}
