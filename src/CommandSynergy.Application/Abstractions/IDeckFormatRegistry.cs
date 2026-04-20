using CommandSynergy.Application.Decks.Portability.Formats;

namespace CommandSynergy.Application.Abstractions;

public interface IDeckFormatRegistry
{
    IReadOnlyList<DeckFormatProfileBase> GetSupportedProfiles();

    DeckFormatProfileBase? GetById(string formatId);
}