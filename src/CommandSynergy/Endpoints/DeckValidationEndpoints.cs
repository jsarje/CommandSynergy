using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Endpoints;

/// <summary>
/// Maps commander deck-validation JSON endpoints.
/// </summary>
public static class DeckValidationEndpoints
{
    /// <summary>
    /// Registers the deck-validation endpoint group.
    /// </summary>
    public static IEndpointRouteBuilder MapDeckValidationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/decks/validate", async (
            DeckSnapshotContract request,
            IDeckValidationService deckValidationService,
            CancellationToken cancellationToken) =>
        {
            var response = await deckValidationService.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
            return Results.Ok(response);
        });

        return endpoints;
    }
}