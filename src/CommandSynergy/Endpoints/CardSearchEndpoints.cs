using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace CommandSynergy.Endpoints;

/// <summary>
/// Maps card-search JSON endpoints.
/// </summary>
public static class CardSearchEndpoints
{
    private const int MaxSearchQueryLength = 120;

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
            var validationErrors = ValidateRequest(q, colors);
            if (validationErrors.Count > 0)
            {
                return Results.ValidationProblem(validationErrors);
            }

            var response = await cardSearchService.SearchAsync(new CardSearchQueryContract
            {
                Query = q.Trim(),
                CommanderCardId = string.IsNullOrWhiteSpace(commanderCardId) ? null : commanderCardId.Trim(),
                Colors = (colors ?? Array.Empty<string>())
                    .Where(static color => !string.IsNullOrWhiteSpace(color))
                    .Select(static color => color.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
            }, cancellationToken).ConfigureAwait(false);

            return Results.Ok(response);
        });

        return endpoints;
    }

    private static Dictionary<string, string[]> ValidateRequest(string query, string[]? colors)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(query))
        {
            errors[nameof(query)] = ["A search query is required."];
        }
        else if (query.Trim().Length > MaxSearchQueryLength)
        {
            errors[nameof(query)] = [$"The search query cannot exceed {MaxSearchQueryLength} characters."];
        }

        var invalidColors = (colors ?? Array.Empty<string>())
            .Where(static color => !string.IsNullOrWhiteSpace(color))
            .Select(static color => color.Trim())
            .Where(static color => color.Length != 1 || "WUBRG".IndexOf(char.ToUpperInvariant(color[0])) < 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (invalidColors.Length > 0)
        {
            errors[nameof(colors)] = ["Colors must be one-letter Commander color identity symbols from W, U, B, R, or G."];
        }

        return errors;
    }
}