namespace CommandSynergy.Domain.Decks;

/// <summary>
/// Represents a card entry within a commander deck.
/// </summary>
public sealed class DeckEntry
{
    public DeckEntry(string cardId, int quantity, string? assignedPileId = null, bool isCommander = false, bool isCompanion = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cardId);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);

        if (isCommander && isCompanion)
        {
            throw new InvalidOperationException("A deck entry cannot be both commander and companion.");
        }

        CardId = cardId;
        Quantity = quantity;
        AssignedPileId = assignedPileId;
        IsCommander = isCommander;
        IsCompanion = isCompanion;
    }

    public string CardId { get; }

    public int Quantity { get; private set; }

    public string? AssignedPileId { get; private set; }

    public bool IsCommander { get; private set; }

    public bool IsCompanion { get; private set; }

    public void UpdateQuantity(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);
        Quantity = quantity;
    }

    public void AssignPile(string? pileId) => AssignedPileId = pileId;

    public void SetCommander(bool isCommander)
    {
        if (isCommander && IsCompanion)
        {
            throw new InvalidOperationException("A deck entry cannot be both commander and companion.");
        }

        IsCommander = isCommander;
    }

    public void SetCompanion(bool isCompanion)
    {
        if (isCompanion && IsCommander)
        {
            throw new InvalidOperationException("A deck entry cannot be both commander and companion.");
        }

        IsCompanion = isCompanion;
    }
}