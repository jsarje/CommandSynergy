using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Decks.Portability.Formats;

namespace CommandSynergy.Application.Decks.Portability;

public sealed class DeckFormatDetectionService
{
    private readonly IDeckFormatRegistry deckFormatRegistry;

    public DeckFormatDetectionService(IDeckFormatRegistry deckFormatRegistry)
    {
        this.deckFormatRegistry = deckFormatRegistry;
    }

    public DeckFormatSelectionResult Detect(string documentText, string? hintedFormatId = null)
    {
        var candidates = deckFormatRegistry.GetSupportedProfiles()
            .Select(profile => new DeckFormatCandidate(profile, profile.Detect(documentText)))
            .Where(static candidate => candidate.Score > 0)
            .OrderByDescending(static candidate => candidate.Score)
            .ThenBy(static candidate => candidate.Profile.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (!string.IsNullOrWhiteSpace(hintedFormatId))
        {
            var hintedProfile = deckFormatRegistry.GetById(hintedFormatId);
            if (hintedProfile is not null)
            {
                return new DeckFormatSelectionResult(hintedProfile, candidates, false);
            }
        }

        if (candidates.Length == 0)
        {
            var genericProfile = deckFormatRegistry.GetById("generic-plaintext");
            return new DeckFormatSelectionResult(genericProfile, Array.Empty<DeckFormatCandidate>(), false);
        }

        if (candidates.Length == 1)
        {
            return new DeckFormatSelectionResult(candidates[0].Profile, candidates, false);
        }

        var highestScore = candidates[0].Score;
        var equallyStrong = candidates.Where(candidate => candidate.Score == highestScore).ToArray();

        return new DeckFormatSelectionResult(candidates[0].Profile, candidates, equallyStrong.Length > 1);
    }
}

public sealed record DeckFormatSelectionResult(
    DeckFormatProfileBase? SelectedProfile,
    IReadOnlyList<DeckFormatCandidate> Candidates,
    bool RequiresConfirmation);

public sealed record DeckFormatCandidate(DeckFormatProfileBase Profile, int Score);