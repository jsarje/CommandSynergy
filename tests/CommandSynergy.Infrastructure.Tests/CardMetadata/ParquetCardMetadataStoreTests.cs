using CommandSynergy.Application.Configuration;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Infrastructure.CardMetadata;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Parquet.Serialization;

namespace CommandSynergy.Infrastructure.Tests.CardMetadata;

/// <summary>
/// Validates Parquet snapshot upsert behavior in <see cref="ParquetCardMetadataStore"/>.
/// </summary>
public sealed class ParquetCardMetadataStoreTests
{
    [Fact]
    public async Task UpsertCardAsync_persists_a_new_card_that_is_later_loaded_by_snapshot()
    {
        var metadataDirectory = Path.Combine(Path.GetTempPath(), $"cs-store-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(metadataDirectory);

        try
        {
            var sut = BuildStore(metadataDirectory);
            var card = CreateCard("sol-ring-id", "Sol Ring", "sol-ring-oracle", new[] { "C" }, typeLine: "Artifact");

            await sut.UpsertCardAsync(card);

            var snapshot = await sut.LoadSnapshotAsync();

            snapshot.Cards.Should().ContainSingle();
            snapshot.Cards[0].CardId.Should().Be("sol-ring-id");
            snapshot.Cards[0].Name.Should().Be("Sol Ring");
            snapshot.Cards[0].OracleId.Should().Be("sol-ring-oracle");
        }
        finally
        {
            Directory.Delete(metadataDirectory, true);
        }
    }

    [Fact]
    public async Task UpsertCardAsync_replaces_an_existing_card_with_the_same_card_id()
    {
        var metadataDirectory = Path.Combine(Path.GetTempPath(), $"cs-store-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(metadataDirectory);

        try
        {
            var sut = BuildStore(metadataDirectory);
            var originalCard = CreateCard("sol-ring-id", "Sol Ring", "sol-ring-oracle", new[] { "C" }, typeLine: "Artifact");
            var updatedCard = CreateCard("sol-ring-id", "Sol Ring Updated", "sol-ring-oracle", new[] { "C" }, typeLine: "Artifact");

            await sut.UpsertCardAsync(originalCard);
            await sut.UpsertCardAsync(updatedCard);

            var snapshot = await sut.LoadSnapshotAsync();

            snapshot.Cards.Should().ContainSingle();
            snapshot.Cards[0].CardId.Should().Be("sol-ring-id");
            snapshot.Cards[0].Name.Should().Be("Sol Ring Updated");
        }
        finally
        {
            Directory.Delete(metadataDirectory, true);
        }
    }

    [Fact]
    public async Task UpsertCardAsync_preserves_other_cards_when_replacing_one_record()
    {
        var metadataDirectory = Path.Combine(Path.GetTempPath(), $"cs-store-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(metadataDirectory);

        try
        {
            var sut = BuildStore(metadataDirectory);
            await WriteSnapshotAsync(metadataDirectory, "existing-card", "Existing Card", "existing-oracle", new[] { "W" }, "Creature");

            var newCard = CreateCard("sol-ring-id", "Sol Ring", "sol-ring-oracle", new[] { "C" }, typeLine: "Artifact");
            await sut.UpsertCardAsync(newCard);

            var snapshot = await sut.LoadSnapshotAsync();

            snapshot.Cards.Should().HaveCount(2);
            snapshot.Cards.Should().Contain(card => card.CardId == "existing-card");
            snapshot.Cards.Should().Contain(card => card.CardId == "sol-ring-id");
        }
        finally
        {
            Directory.Delete(metadataDirectory, true);
        }
    }

    [Fact]
    public async Task UpsertCardAsync_persists_commander_eligibility_basis_and_metadata_source()
    {
        var metadataDirectory = Path.Combine(Path.GetTempPath(), $"cs-store-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(metadataDirectory);

        try
        {
            var sut = BuildStore(metadataDirectory);
            var card = CreateCard(
                "atraxa-id",
                "Atraxa, Praetors' Voice",
                "atraxa-oracle",
                new[] { "B", "G", "U", "W" },
                typeLine: "Legendary Creature — Phyrexian Angel Horror",
                eligibilityBasis: CommanderEligibilityBasis.LegendaryCreature,
                metadataSource: CardMetadataSource.UserDrivenScryfallEnrichment);

            await sut.UpsertCardAsync(card);

            var snapshot = await sut.LoadSnapshotAsync();

            snapshot.Cards.Should().ContainSingle();
            snapshot.Cards[0].CommanderEligibilityBasis.Should().Be(CommanderEligibilityBasis.LegendaryCreature);
            snapshot.Cards[0].MetadataSource.Should().Be(CardMetadataSource.UserDrivenScryfallEnrichment);
        }
        finally
        {
            Directory.Delete(metadataDirectory, true);
        }
    }

    [Fact]
    public async Task ReplaceSnapshotAsync_replaces_existing_snapshot_contents()
    {
        var metadataDirectory = Path.Combine(Path.GetTempPath(), $"cs-store-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(metadataDirectory);

        try
        {
            var sut = BuildStore(metadataDirectory);
            await sut.UpsertCardAsync(CreateCard("existing-card", "Existing Card", "existing-oracle", [ "W" ], "Creature"));

            await sut.ReplaceSnapshotAsync(
            [
                CreateCard(
                    "bulk-card-id",
                    "Bulk Card",
                    "bulk-card-oracle",
                    [ "U" ],
                    "Legendary Creature — Wizard",
                    eligibilityBasis: CommanderEligibilityBasis.LegendaryCreature,
                    metadataSource: CardMetadataSource.BulkSnapshotImport),
            ]);

            var snapshot = await sut.LoadSnapshotAsync();

            snapshot.Cards.Should().ContainSingle();
            snapshot.Cards[0].CardId.Should().Be("bulk-card-id");
            snapshot.Cards[0].MetadataSource.Should().Be(CardMetadataSource.BulkSnapshotImport);
            snapshot.Cards[0].CommanderEligibilityBasis.Should().Be(CommanderEligibilityBasis.LegendaryCreature);
        }
        finally
        {
            Directory.Delete(metadataDirectory, true);
        }
    }

    [Fact]
    public async Task LoadSnapshotAsync_returns_empty_when_snapshot_file_does_not_exist()
    {
        var metadataDirectory = Path.Combine(Path.GetTempPath(), $"cs-store-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(metadataDirectory);

        try
        {
            var sut = BuildStore(metadataDirectory);

            var snapshot = await sut.LoadSnapshotAsync();

            snapshot.Should().NotBeNull();
            snapshot.Cards.Should().BeEmpty();
            snapshot.SnapshotId.Should().Be("missing");
        }
        finally
        {
            Directory.Delete(metadataDirectory, true);
        }
    }

    private static ParquetCardMetadataStore BuildStore(string metadataDirectory) =>
        new(
            Options.Create(new CardMetadataOptions
            {
                SnapshotDirectory = metadataDirectory,
                SnapshotFileName = "cards.parquet",
                SearchIndexVersion = "test-v1",
            }),
            NullLogger<ParquetCardMetadataStore>.Instance);

    private static CardProfile CreateCard(
        string cardId,
        string name,
        string oracleId,
        IReadOnlyList<string> colorIdentity,
        string typeLine,
        CommanderEligibilityBasis eligibilityBasis = CommanderEligibilityBasis.Unknown,
        CardMetadataSource metadataSource = CardMetadataSource.Unknown) => new()
    {
        CardId = cardId,
        OracleId = oracleId,
        Name = name,
        TypeLine = typeLine,
        ColorIdentity = colorIdentity,
        CommanderEligibilityBasis = eligibilityBasis,
        MetadataSource = metadataSource,
        LastSyncedUtc = DateTimeOffset.UtcNow,
        FaceProfiles = [new CardFaceProfile("0", name, null, typeLine, null, null, true)],
    };

    private sealed class TestParquetCardMetadataRow
    {
        public string CardId { get; init; } = string.Empty;

        public string? OracleId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string TypeLine { get; init; } = string.Empty;

        public string[] ColorIdentity { get; init; } = Array.Empty<string>();
    }

    private static async Task WriteSnapshotAsync(
        string metadataDirectory,
        string cardId,
        string name,
        string oracleId,
        string[] colorIdentity,
        string typeLine)
    {
        var rows = new[] { new TestParquetCardMetadataRow { CardId = cardId, OracleId = oracleId, Name = name, TypeLine = typeLine, ColorIdentity = colorIdentity } };
        var snapshotPath = Path.Combine(metadataDirectory, "cards.parquet");
        await using var stream = File.Create(snapshotPath);
        await ParquetSerializer.SerializeAsync(rows, stream);
    }
}
