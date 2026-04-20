using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Decks.Portability.Formats;

namespace CommandSynergy.Application.Decks.Portability;

public sealed class DeckFormatRegistry : IDeckFormatRegistry
{
    private readonly IReadOnlyList<DeckFormatProfileBase> profiles;

    public DeckFormatRegistry(IEnumerable<DeckFormatProfileBase> profiles)
    {
        this.profiles = profiles.OrderBy(static profile => profile.DisplayName, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public IReadOnlyList<DeckFormatProfileBase> GetSupportedProfiles() => profiles;

    public DeckFormatProfileBase? GetById(string formatId) =>
        profiles.FirstOrDefault(profile => string.Equals(profile.FormatId, formatId, StringComparison.OrdinalIgnoreCase));
}