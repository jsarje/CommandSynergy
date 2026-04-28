using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Endpoints;

/// <summary>
/// Maps deck-suggestion JSON endpoints.
/// </summary>
public static class DeckSuggestionEndpoints
{
    private const int DefaultSuggestionLimit = 3;
    private const int MaximumSuggestionLimit = 12;

    /// <summary>
    /// Registers the deck-suggestion endpoint group.
    /// </summary>
    public static IEndpointRouteBuilder MapDeckSuggestionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/decks/suggestions", async (
            DeckSuggestionsRequestContract request,
            IDeckSuggestionService deckSuggestionService,
            CancellationToken cancellationToken) =>
        {
            if (request.Deck.Entries is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.Deck)] = ["A deck snapshot with entries is required."],
                });
            }

            var sanitizedRequest = request with
            {
                ExcludedCardIds = request.ExcludedCardIds
                    .Where(static cardId => !string.IsNullOrWhiteSpace(cardId))
                    .Select(static cardId => cardId.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                Filters = request.Filters is null
                    ? new DeckSuggestionFiltersContract()
                    : request.Filters with
                    {
                        CardType = string.IsNullOrWhiteSpace(request.Filters.CardType) ? null : request.Filters.CardType.Trim(),
                        ColorIdentity = request.Filters.ColorIdentity
                            .Where(static color => !string.IsNullOrWhiteSpace(color))
                            .Select(static color => color.Trim())
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToArray(),
                    },
                Limit = Math.Clamp(request.Limit == 0 ? DefaultSuggestionLimit : request.Limit, 1, MaximumSuggestionLimit),
            };

            var response = await deckSuggestionService.GetSuggestionsAsync(sanitizedRequest, cancellationToken).ConfigureAwait(false);
            return Results.Ok(response);
        });

        return endpoints;
    }
}
