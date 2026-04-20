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
    private readonly ICardCatalogGateway cardCatalogGateway;
    private readonly BracketCalculationService bracketCalculationService;
    private readonly SynergyScoringService synergyScoringService;
    private readonly IReadOnlyList<IDeckAdviceService> deckAdviceServices;

    /// <summary>
    /// Creates a deck-analysis service.
    /// </summary>
    public DeckAnalysisService(
        ICardCatalogGateway cardCatalogGateway,
        BracketCalculationService bracketCalculationService,
        SynergyScoringService synergyScoringService,
        IEnumerable<IDeckAdviceService> deckAdviceServices)
    {
        this.cardCatalogGateway = cardCatalogGateway;
        this.bracketCalculationService = bracketCalculationService;
        this.synergyScoringService = synergyScoringService;
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
        var synergyAssessment = synergyScoringService.Calculate(deck, profiles);

        var response = new DeckAnalysisResponseContract
        {
            Bracket = MapBracket(bracketAssessment),
            Synergy = MapSynergy(synergyAssessment),
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
        Summary = assessment.Summary,
        CommanderSpecificHits = assessment.CommanderSpecificHits,
        StapleOverloadIndicators = assessment.StapleOverloadIndicators,
    };
}