using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Application.Decks.Portability;

public sealed class WorkingCopyProjectionService : IWorkingCopyProjectionService
{
    public DeckSnapshotContract CreateWorkingCopy(PortableDeckSnapshot snapshot, string? deckId = null, string? deckName = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var normalizedSnapshot = PortableDeckSectionMapper.NormalizeImportedSnapshot(snapshot);

        var piles = normalizedSnapshot.Sections
            .OrderBy(static section => section.SortOrder)
            .Select(static section => new PileDefinitionContract
            {
                PileId = section.SectionId,
                Name = section.DisplayName,
                SortOrder = section.SortOrder,
            })
            .ToArray();      

        var commanderCardId = normalizedSnapshot.CommanderCardIds.FirstOrDefault()
            ?? normalizedSnapshot.Entries.FirstOrDefault(static entry => entry.IsCommander && !string.IsNullOrWhiteSpace(entry.CardId))?.CardId            
            ?? throw new InvalidOperationException("A working copy requires a resolved commander card.");

        return new DeckSnapshotContract
        {
            DeckId = deckId,
            Name = deckName ?? normalizedSnapshot.DeckName,
            CommanderCardId = commanderCardId,
            CompanionCardId = normalizedSnapshot.CompanionCardId,
            Piles = piles,
            Entries = normalizedSnapshot.Entries
                .Where(static entry => !string.IsNullOrWhiteSpace(entry.CardId))
                .Select(static entry => new DeckEntryContract
                {
                    CardId = entry.CardId!,
                    Quantity = entry.Quantity,
                    AssignedPileId = entry.SectionId,
                    IsCommander = entry.IsCommander,
                    IsCompanion = entry.IsCompanion,
                })
                .ToArray(),
        };
    }
}