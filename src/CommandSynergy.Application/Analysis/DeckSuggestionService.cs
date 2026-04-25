using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Domain.Cards;

namespace CommandSynergy.Application.Analysis;

/// <summary>
/// Builds commander-aware card suggestions by blending EDHREC and local theme signals.
/// </summary>
public sealed class DeckSuggestionService(
    ICardCatalogGateway cardCatalogGateway,
    IEdhrecClient edhrecClient,
    IThemeMatchingService themeMatchingService) : IDeckSuggestionService
{
    private const int DefaultSuggestionLimit = 3;
    private const int MaximumSuggestionLimit = 12;

    /// <inheritdoc />
    public async Task<DeckSuggestionsResponseContract> GetSuggestionsAsync(DeckSuggestionsRequestContract request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Deck);

        var commanderCardId = request.Deck.Entries.SingleOrDefault(static entry => entry.IsCommander)?.CardId
            ?? request.Deck.CommanderCardId;
        if (string.IsNullOrWhiteSpace(commanderCardId))
        {
            return Empty(null);
        }

        var deckCardIds = request.Deck.Entries
            .Select(static entry => entry.CardId)
            .Where(static cardId => !string.IsNullOrWhiteSpace(cardId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var deckProfiles = await cardCatalogGateway.GetCardProfilesAsync(deckCardIds, cancellationToken).ConfigureAwait(false);
        if (!deckProfiles.TryGetValue(commanderCardId, out var commanderProfile))
        {
            return Empty(commanderCardId);
        }

        var candidatePoolTask = cardCatalogGateway.GetCommanderLegalCardProfilesAsync(cancellationToken);
        var edhrecInsightsTask = edhrecClient.GetCommanderThemeInsightsAsync(commanderProfile, cancellationToken);
        await Task.WhenAll(candidatePoolTask, edhrecInsightsTask).ConfigureAwait(false);

        var candidatePool = await candidatePoolTask.ConfigureAwait(false);
        var edhrecInsights = await edhrecInsightsTask.ConfigureAwait(false);
        var excludedCardIds = request.ExcludedCardIds
            .Concat(deckCardIds)
            .Where(static cardId => !string.IsNullOrWhiteSpace(cardId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var themeWeights = BuildDeckThemeWeights(request.Deck, deckProfiles);
        var commanderSignals = ResolveSignals(commanderProfile);
        var normalizedCommanderColors = commanderProfile.ColorIdentity
            .Where(static color => !string.IsNullOrWhiteSpace(color))
            .Select(static color => color.Trim().ToUpperInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var filters = request.Filters ?? new DeckSuggestionFiltersContract();
        var limit = Math.Clamp(request.Limit == 0 ? DefaultSuggestionLimit : request.Limit, 1, MaximumSuggestionLimit);

        var suggestions = candidatePool
            .Where(profile => !excludedCardIds.Contains(profile.CardId))
            .Where(profile => profile.IsLegalInCommander)
            .Where(profile => normalizedCommanderColors.Count == 0
                ? profile.ColorIdentity.Count == 0
                : profile.ColorIdentity.All(color => normalizedCommanderColors.Contains(color.Trim().ToUpperInvariant())))
            .Where(profile => MatchesFilters(profile, filters))
            .Select(profile => CreateSuggestion(profile, commanderSignals, themeWeights, edhrecInsights))
            .OrderByDescending(static suggestion => suggestion.CombinedScore)
            .ThenByDescending(static suggestion => suggestion.ThemeScore)
            .ThenBy(static suggestion => suggestion.Card.Name, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToArray();

        return new DeckSuggestionsResponseContract
        {
            CommanderCardId = commanderCardId,
            Suggestions = suggestions,
        };
    }

    private DeckSuggestionCardContract CreateSuggestion(
        CardProfile profile,
        IReadOnlyDictionary<string, decimal> commanderSignals,
        IReadOnlyDictionary<string, decimal> themeWeights,
        CommanderThemeInsights edhrecInsights)
    {
        var candidateSignals = ResolveSignals(profile);
        var themeScore = CalculateThemeScore(candidateSignals, commanderSignals, themeWeights);
        // EDHREC synergy values land on a -1..1 scale, so shift them into the
        // same 0..100 range used by local theme scoring before blending.
        decimal? edhrecScore = edhrecInsights.IsAvailable && edhrecInsights.SynergyByCardId.TryGetValue(profile.CardId, out var synergy)
            ? decimal.Round(Math.Clamp((synergy + 1m) * 50m, 0m, 100m), 1, MidpointRounding.AwayFromZero)
            : null;
        var combinedScore = decimal.Round(
            edhrecScore is null
                ? themeScore
                // Keep the local deck-shape analysis slightly dominant so live
                // deck composition changes stay ahead of static popularity data.
                : Math.Clamp((themeScore * 0.6m) + (edhrecScore.Value * 0.4m), 0m, 100m),
            1,
            MidpointRounding.AwayFromZero);

        return new DeckSuggestionCardContract
        {
            Card = MapSuggestionCard(profile),
            CombinedScore = combinedScore,
            ThemeScore = themeScore,
            EdhrecScore = edhrecScore,
        };
    }

    private IReadOnlyDictionary<string, decimal> BuildDeckThemeWeights(
        DeckSnapshotContract deck,
        IReadOnlyDictionary<string, CardProfile> deckProfiles)
    {
        var totals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var weightedTotal = 0m;

        foreach (var entry in deck.Entries)
        {
            if (!deckProfiles.TryGetValue(entry.CardId, out var profile))
            {
                continue;
            }

            // Commanders define the deck's intended lane, so weight that signal
            // above individual support cards when building suggestion themes.
            var entryWeight = entry.IsCommander ? 3m : Math.Max(entry.Quantity, 1);
            var signals = ResolveSignals(profile);
            if (signals.Count == 0)
            {
                continue;
            }

            weightedTotal += entryWeight;

            foreach (var pair in signals)
            {
                totals[pair.Key] = totals.GetValueOrDefault(pair.Key) + (pair.Value * entryWeight);
            }
        }

        if (weightedTotal <= 0m)
        {
            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        }

        return totals.ToDictionary(
            static pair => pair.Key,
            pair => pair.Value / weightedTotal,
            StringComparer.OrdinalIgnoreCase);
    }

    private IReadOnlyDictionary<string, decimal> ResolveSignals(CardProfile profile) =>
        profile.ThemeSignals.Count > 0
            ? profile.ThemeSignals
            : themeMatchingService.ComputeThemeSignals(profile);

    private static decimal CalculateThemeScore(
        IReadOnlyDictionary<string, decimal> candidateSignals,
        IReadOnlyDictionary<string, decimal> commanderSignals,
        IReadOnlyDictionary<string, decimal> themeWeights)
    {
        if (candidateSignals.Count == 0)
        {
            return 0m;
        }

        var deckOverlap = themeWeights.Sum(pair =>
            candidateSignals.TryGetValue(pair.Key, out var signal)
                ? pair.Value * signal
                : 0m);
        var commanderOverlap = commanderSignals.Sum(pair =>
            candidateSignals.TryGetValue(pair.Key, out var signal)
                ? pair.Value * signal
                : 0m);

        // Favor the evolving deck-wide theme map while still reserving room for
        // direct commander-plan alignment in the final internal score.
        return decimal.Round(
            Math.Clamp((deckOverlap * 70m) + (commanderOverlap * 30m), 0m, 100m),
            1,
            MidpointRounding.AwayFromZero);
    }

    private static bool MatchesFilters(CardProfile profile, DeckSuggestionFiltersContract filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.CardType)
            && !profile.TypeLine.Contains(filters.CardType.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (filters.ManaValue is not null && profile.ManaValue != filters.ManaValue.Value)
        {
            return false;
        }

        if (filters.MaxEurPrice is not null && (profile.EurPrice is null || profile.EurPrice > filters.MaxEurPrice.Value))
        {
            return false;
        }

        var colorFilters = filters.ColorIdentity
            .Where(static color => !string.IsNullOrWhiteSpace(color))
            .Select(static color => color.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (colorFilters.Length > 0)
        {
            var cardColors = profile.ColorIdentity
                .Where(static color => !string.IsNullOrWhiteSpace(color))
                .Select(static color => color.Trim().ToUpperInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (colorFilters.Any(filter => !cardColors.Contains(filter)))
            {
                return false;
            }
        }

        return true;
    }

    private static CardSearchResultContract MapSuggestionCard(CardProfile profile) => new()
    {
        CardId = profile.CardId,
        Name = profile.Name,
        ManaCost = profile.ManaCost,
        ManaValue = profile.ManaValue,
        TypeLine = profile.TypeLine,
        ColorIdentity = profile.ColorIdentity,
        SaltScore = profile.SaltScore,
        ImageUri = profile.ImageUri,
        EurPrice = profile.EurPrice,
        HasMultipleFaces = profile.HasMultipleFaces,
        AllowsMultipleCopies = profile.AllowsMultipleCopies,
        CommanderEligibilityBasis = profile.CommanderEligibilityBasis,
    };

    private static DeckSuggestionsResponseContract Empty(string? commanderCardId) => new()
    {
        CommanderCardId = commanderCardId,
        Suggestions = Array.Empty<DeckSuggestionCardContract>(),
    };
}
