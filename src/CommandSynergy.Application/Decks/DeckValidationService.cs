using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Domain.Decks;
using CommandSynergy.Domain.Rules;

namespace CommandSynergy.Application.Decks;

/// <summary>
/// Converts deck snapshot contracts into the domain aggregate and validates them with commander rules.
/// </summary>
public sealed class DeckValidationService : IDeckValidationCoreService
{
    private readonly ICardCatalogGateway cardCatalogGateway;
    private readonly ICommanderRules commanderRules;

    /// <summary>
    /// Creates a deck-validation application service.
    /// </summary>
    public DeckValidationService(ICardCatalogGateway cardCatalogGateway, ICommanderRules commanderRules)
    {
        this.cardCatalogGateway = cardCatalogGateway;
        this.commanderRules = commanderRules;
    }

    /// <inheritdoc />
    public async Task<DeckValidationResponseContract> ValidateAsync(DeckSnapshotContract deckSnapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(deckSnapshot);

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

        var cardIds = deck.Entries.Select(entry => entry.CardId).Distinct(StringComparer.OrdinalIgnoreCase);
        var profiles = await cardCatalogGateway.GetCardProfilesAsync(cardIds, cancellationToken).ConfigureAwait(false);
        var validation = commanderRules.Validate(deck, profiles);

        return new DeckValidationResponseContract
        {
            IsValid = validation.IsValid,
            DeckCardCount = validation.DeckCardCount,
            Findings = validation.Findings.Select(finding => new ValidationFindingContract
            {
                Severity = finding.Severity,
                Code = finding.Code,
                Message = finding.Message,
                AffectedCardIds = finding.AffectedCardIds,
            }).ToArray(),
        };
    }
}
