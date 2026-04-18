using System.Diagnostics;
using CommandSynergy.Application.Configuration;
using CommandSynergy.Infrastructure.CardMetadata;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace CommandSynergy.Infrastructure.Tests.Performance;

public sealed class CardSearchPerformanceTests
{
    [Fact]
    public void Build_completes_within_search_budget_for_large_snapshot()
    {
        var builder = new SearchIndexSnapshotBuilder(Options.Create(new CardMetadataOptions
        {
            SearchIndexVersion = "perf-v1",
        }));

        var snapshot = new CardMetadataSnapshot(
            "snapshot-perf",
            "cards.parquet",
            DateTime.UtcNow,
            Enumerable.Range(0, 5000)
                .Select(index => new CardMetadataRecord(
                    $"card-{index}",
                    $"oracle-{index}",
                    $"Card {index}",
                    "Artifact",
                    ["C"],
                    "{1}",
                    1,
                    null,
                    null,
                    false))
                .ToArray());

        var started = Stopwatch.GetTimestamp();
        var result = builder.Build(snapshot);
        var elapsed = Stopwatch.GetElapsedTime(started);

        result.CardSummaries.Should().HaveCount(5000);
        elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
    }
}