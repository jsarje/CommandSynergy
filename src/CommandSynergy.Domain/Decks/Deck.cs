namespace CommandSynergy.Domain.Decks;

/// <summary>
/// Represents the aggregate root for a commander's active deck draft.
/// </summary>
public sealed class Deck
{
    private readonly List<DeckEntry> entries = new();
    private readonly List<Pile> piles = new();

    public Deck(string? deckId = null, string? name = null)
    {
        DeckId = deckId ?? Guid.NewGuid().ToString("n");
        Name = name;
    }

    public string DeckId { get; }

    public string? Name { get; }

    public IReadOnlyList<DeckEntry> Entries => entries;

    public IReadOnlyList<Pile> Piles => piles;

    public int TotalCardCount => entries.Sum(entry => entry.Quantity);

    public void AddPile(string pileId, string name, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (piles.Any(existing => string.Equals(existing.PileId, pileId, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        piles.Add(new Pile(pileId, name, sortOrder));
    }

    public void UpsertEntry(string cardId, int quantity, string? assignedPileId = null, bool isCommander = false, bool isCompanion = false)
    {
        EnsurePileExists(assignedPileId);

        var entry = entries.SingleOrDefault(existing => string.Equals(existing.CardId, cardId, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            entry = new DeckEntry(cardId, quantity, assignedPileId, isCommander, isCompanion);
            entries.Add(entry);
        }
        else
        {
            entry.UpdateQuantity(quantity);
            entry.AssignPile(assignedPileId);
            entry.SetCommander(isCommander);
            entry.SetCompanion(isCompanion);
        }

        if (isCommander)
        {
            SetCommander(cardId);
        }

        if (isCompanion)
        {
            SetCompanion(cardId);
        }
    }

    public void SetCommander(string cardId)
    {
        var entry = GetOrCreateEntry(cardId);
        foreach (var existing in entries)
        {
            existing.SetCommander(string.Equals(existing.CardId, cardId, StringComparison.OrdinalIgnoreCase));
        }

        entry.UpdateQuantity(Math.Max(entry.Quantity, 1));
    }

    public void SetCompanion(string? cardId)
    {
        foreach (var existing in entries)
        {
            existing.SetCompanion(!string.IsNullOrWhiteSpace(cardId) && string.Equals(existing.CardId, cardId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(cardId))
        {
            GetOrCreateEntry(cardId);
        }
    }

    public void AssignPile(string cardId, string? pileId)
    {
        EnsurePileExists(pileId);

        var entry = entries.Single(existing => string.Equals(existing.CardId, cardId, StringComparison.OrdinalIgnoreCase));
        entry.AssignPile(pileId);
    }

    private DeckEntry GetOrCreateEntry(string cardId)
    {
        var entry = entries.SingleOrDefault(existing => string.Equals(existing.CardId, cardId, StringComparison.OrdinalIgnoreCase));
        if (entry is not null)
        {
            return entry;
        }

        entry = new DeckEntry(cardId, 1);
        entries.Add(entry);
        return entry;
    }

    private void EnsurePileExists(string? pileId)
    {
        if (string.IsNullOrWhiteSpace(pileId))
        {
            return;
        }

        if (piles.All(existing => !string.Equals(existing.PileId, pileId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Pile '{pileId}' does not exist in the current deck.");
        }
    }
}