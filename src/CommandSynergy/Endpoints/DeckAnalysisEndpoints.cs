using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Endpoints;

/// <summary>
/// Maps commander deck-analysis JSON endpoints.
/// </summary>
public static class DeckAnalysisEndpoints
{
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
            var response = await deckAnalysisService.AnalyzeAsync(request, cancellationToken).ConfigureAwait(false);
            return Results.Ok(response);
        });

        return endpoints;
    }
}