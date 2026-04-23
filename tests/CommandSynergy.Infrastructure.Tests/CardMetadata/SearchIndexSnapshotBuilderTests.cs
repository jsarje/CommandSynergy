using CommandSynergy.Application.Configuration;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Infrastructure.CardMetadata;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace CommandSynergy.Infrastructure.Tests.CardMetadata;

public sealed class SearchIndexSnapshotBuilderTests
{
    [Fact]
    public void Build_projects_searchable_card_fields_and_configured_version()
    {
        var sut = new SearchIndexSnapshotBuilder(Options.Create(new CardMetadataOptions
        {
            SearchIndexVersion = "search-v2",
        }));

        var snapshot = new CardMetadataSnapshot(
            "snapshot-123",
            "cards.parquet",
            DateTime.UtcNow,
            [
                new CardMetadataRecord(
                    "sol-ring",
                    "sol-ring-oracle",
                    "Sol Ring",
                    "Artifact",
                    Array.Empty<string>(),
                    "{1}",
                    1m,
                    1.1m,
                    "https://cards.example/sol-ring.jpg",
                    false,
                    AllowsMultipleCopies: true,
                    CommanderEligibilityBasis: CommanderEligibilityBasis.Unknown),
            ]);

        var result = sut.Build(snapshot);

        result.Version.Should().Be("search-v2");
        result.SourceSnapshotId.Should().Be("snapshot-123");
        result.GeneratedUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        result.CardSummaries.Should().ContainSingle();
        result.CardSummaries[0].CardId.Should().Be("sol-ring");
        result.CardSummaries[0].Name.Should().Be("Sol Ring");
        result.CardSummaries[0].ManaCost.Should().Be("{1}");
        result.CardSummaries[0].ManaValue.Should().Be(1m);
        result.CardSummaries[0].TypeLine.Should().Be("Artifact");
        result.CardSummaries[0].SaltScore.Should().Be(1.1m);
        result.CardSummaries[0].ImageUri.Should().Be("https://cards.example/sol-ring.jpg");
        result.CardSummaries[0].AllowsMultipleCopies.Should().BeTrue();
    }
}
