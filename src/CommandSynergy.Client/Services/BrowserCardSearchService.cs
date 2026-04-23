using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Client.Services;

public sealed class BrowserCardSearchService(ICardSearchIndexClient cardSearchIndexClient) : ICardSearchService
{
    public Task<CardSearchResponseContract> SearchAsync(CardSearchQueryContract request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return cardSearchIndexClient.SearchAsync(
            request.Query,
            request.CommanderCardId,
            request.Colors,
            cancellationToken);
    }
}
