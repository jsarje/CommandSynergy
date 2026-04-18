namespace CommandSynergy.Domain.Decks;

/// <summary>
/// Represents a user-defined organizational pile in the deck workspace.
/// </summary>
public sealed record Pile(string PileId, string Name, int SortOrder);