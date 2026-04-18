using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Endpoints;

/// <summary>
/// Maps commander deck-analysis JSON endpoints.
/// </summary>
public static class DeckAnalysisEndpoints
{
    private const int MaxEntryCount = 250;
    private const int MaxPileCount = 64;
    private const int MaxIdentifierLength = 128;

    /// <summary>
    /// Registers the deck-analysis endpoint group.
    /// </summary>
    public static IEndpointRouteBuilder MapDeckAnalysisEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/decks/analyze", async (
            DeckSnapshotContract request,
            IDeckAnalysisService deckAnalysisService,
            CancellationToken cancellationToken) =>
        {
            var validationErrors = ValidateRequest(request);
            if (validationErrors.Count > 0)
            {
                return Results.ValidationProblem(validationErrors);
            }

            var response = await deckAnalysisService.AnalyzeAsync(request, cancellationToken).ConfigureAwait(false);
            return Results.Ok(response);
        });

        return endpoints;
    }

    private static Dictionary<string, string[]> ValidateRequest(DeckSnapshotContract request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.CommanderCardId) || request.CommanderCardId.Trim().Length > MaxIdentifierLength)
        {
            errors[nameof(request.CommanderCardId)] = ["A commander card identifier is required and must be 128 characters or fewer."];
        }

        if (request.Entries.Count == 0)
        {
            errors[nameof(request.Entries)] = ["At least one deck entry is required."];
        }
        else if (request.Entries.Count > MaxEntryCount)
        {
            errors[nameof(request.Entries)] = [$"Deck submissions cannot exceed {MaxEntryCount} entries."];
        }

        if (request.Piles.Count > MaxPileCount)
        {
            errors[nameof(request.Piles)] = [$"Deck submissions cannot exceed {MaxPileCount} piles."];
        }

        if (request.Entries.Any(static entry => string.IsNullOrWhiteSpace(entry.CardId) || entry.CardId.Trim().Length > MaxIdentifierLength))
        {
            errors["entries.cardId"] = ["Each deck entry must include a card identifier that is 128 characters or fewer."];
        }

        if (request.Entries.Any(static entry => entry.Quantity <= 0 || entry.Quantity > 100))
        {
            errors["entries.quantity"] = ["Each deck entry quantity must be between 1 and 100."];
        }

        return errors;
    }
}