using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Endpoints;

/// <summary>
/// Maps card-search JSON endpoints.
/// </summary>
public static class CardSearchEndpoints
{
    /// <summary>
    /// Registers the card-search endpoint group.
    /// </summary>
    public static IEndpointRouteBuilder MapCardSearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/cards/search", async (
            string q,
            string? commanderCardId,
            string[]? colors,
            ICardSearchService cardSearchService,
            CancellationToken cancellationToken) =>
        {
            var response = await cardSearchService.SearchAsync(new CardSearchQueryContract
            {
                Query = q,
                CommanderCardId = commanderCardId,
                Colors = colors ?? Array.Empty<string>(),
            }, cancellationToken).ConfigureAwait(false);

            return Results.Ok(response);
        });

        return endpoints;
    }
}