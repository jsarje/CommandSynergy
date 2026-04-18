using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using Microsoft.Extensions.Logging;

namespace CommandSynergy.Infrastructure.Observability;

/// <summary>
/// Adds structured logging around card search requests.
/// </summary>
public sealed class CardSearchLoggingDecorator : ICardSearchService
{
    private readonly ICardSearchService inner;
    private readonly ILogger<CardSearchLoggingDecorator> logger;

    /// <summary>
    /// Creates a logging decorator for the card search service.
    /// </summary>
    public CardSearchLoggingDecorator(ICardSearchService inner, ILogger<CardSearchLoggingDecorator> logger)
    {
        this.inner = inner;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<CardSearchResponseContract> SearchAsync(CardSearchQueryContract request, CancellationToken cancellationToken = default)
    {
        var startedUtc = DateTimeOffset.UtcNow;
        logger.LogInformation("Searching cards for query {Query}", request.Query);

        var response = await inner.SearchAsync(request, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Card search for query {Query} returned {ResultCount} results in {ElapsedMilliseconds}ms",
            request.Query,
            response.Results.Count,
            (DateTimeOffset.UtcNow - startedUtc).TotalMilliseconds);

        return response;
    }
}