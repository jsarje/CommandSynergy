using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Cards;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Domain.Cards;
using FluentAssertions;

namespace CommandSynergy.Application.Tests.Cards;

public sealed class CardSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_returns_gateway_results_and_snapshot_version()
    {
        var gateway = new StubCardCatalogGateway(
            [
                new CardSearchResultContract
                {
                    CardId = "sol-ring",
                    Name = "Sol Ring",
                    TypeLine = "Artifact",
                    ColorIdentity = Array.Empty<string>(),
                },
            ]);

        var sut = new CardSearchService(gateway);

        var response = await sut.SearchAsync(new CardSearchQueryContract { Query = "sol" });

        response.SnapshotVersion.Should().Be("snapshot-42");
        response.Results.Should().ContainSingle(result => result.CardId == "sol-ring");
    }

    private sealed class StubCardCatalogGateway : ICardCatalogGateway
    {
        private readonly IReadOnlyList<CardSearchResultContract> results;

        public StubCardCatalogGateway(IReadOnlyList<CardSearchResultContract> results)
        {
            this.results = results;
        }

        public Task<IReadOnlyDictionary<string, CardProfile>> GetCardProfilesAsync(IEnumerable<string> cardIds, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyDictionary<string, CardProfile>)new Dictionary<string, CardProfile>());

        public Task<IReadOnlyList<CardSearchResultContract>> SearchAsync(CardSearchQueryContract request, CancellationToken cancellationToken = default) =>
            Task.FromResult(results);

        public Task<string?> GetSnapshotVersionAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>("snapshot-42");
    }
}