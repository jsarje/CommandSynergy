using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks.Portability;

namespace CommandSynergy.Application.Abstractions;

public interface IWorkingCopyProjectionService
{
    DeckSnapshotContract CreateWorkingCopy(PortableDeckSnapshot snapshot, string? deckId = null, string? deckName = null);
}