using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks.Portability;
using CommandSynergy.Application.Decks.Portability.Formats;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace CommandSynergy.Application.Tests.Decks;

public sealed class DeckImportServiceTests
{
    [Fact]
    public async Task ImportAsync_maps_commander_and_sections_for_moxfield_input()
    {
        var sut = CreateSut();

        var result = await sut.ImportAsync(new DeckImportRequestContract
        {
            RawDocumentText = DeckPortabilityFixtureLoader.Load("moxfield-sample.txt"),
        });

        result.DetectedFormatId.Should().Be("moxfield-text");
        result.ImportedDeck.NormalizedDeck.CommanderCardIds.Should().ContainSingle("atraxa-praetors-voice");
        result.ImportedDeck.NormalizedDeck.Sections.Should().Contain(section => section.Role == "commander");
    }

    [Fact]
    public async Task ImportAsync_preserves_partial_success_with_diagnostics()
    {
        var sut = CreateSut();

        var result = await sut.ImportAsync(new DeckImportRequestContract
        {
            RawDocumentText = DeckPortabilityFixtureLoader.Load("partial-success-sample.txt"),
        });

        result.ImportedDeck.NormalizedDeck.Entries.Should().Contain(entry => entry.DisplayName == "Sol Ring");
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Code == "card-unresolved");
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Code == "unrecognized-line");
    }

    [Fact]
    public async Task ImportAsync_normalizes_manabox_sections_into_internal_workspace_piles()
    {
        var sut = CreateSut();

        var result = await sut.ImportAsync(new DeckImportRequestContract
        {
            RawDocumentText = DeckPortabilityFixtureLoader.Load("manabox-sample.txt"),
        });

        result.DetectedFormatId.Should().Be("manabox-text");
        result.ImportedDeck.NormalizedDeck.Sections.Should().Contain(section => section.SectionId == "command-zone" && section.Role == "commander");
        result.ImportedDeck.NormalizedDeck.Sections.Should().Contain(section => section.SectionId == "mainboard" && section.Role == "mainboard");
        result.ImportedDeck.NormalizedDeck.Sections.Should().Contain(section => section.SectionId == "maybeboard" && section.Role == "maybeboard");
        result.ImportedDeck.NormalizedDeck.Entries.Should().Contain(entry => entry.IsCommander && entry.SectionId == "command-zone");
        result.ImportedDeck.NormalizedDeck.Entries.Should().Contain(entry =>
            entry.CardId == "sol-ring"
            && entry.ImageUri == "https://cards.example/sol-ring.jpg"
            && entry.TypeLine == "Artifact"
            && entry.ColorIdentity.Count == 0);
    }

    private static DeckImportService CreateSut()
    {
        var registry = new DeckFormatRegistry([new MoxfieldTextFormatProfile(), new ManaBoxTextFormatProfile(), new GenericPlaintextFormatProfile()]);
        var detectionService = new DeckFormatDetectionService(registry);
        return new DeckImportService(new StubCardSearchService(), detectionService, new FakeTimeProvider(DateTimeOffset.Parse("2026-04-20T00:00:00Z")));
    }

    private sealed class StubCardSearchService : ICardSearchService
    {
        private static readonly IReadOnlyDictionary<string, CardSearchResultContract> Cards = new Dictionary<string, CardSearchResultContract>(StringComparer.OrdinalIgnoreCase)
        {
            ["Atraxa, Praetors' Voice"] = new() { CardId = "atraxa-praetors-voice", Name = "Atraxa, Praetors' Voice", ManaCost = "{G}{W}{U}{B}", TypeLine = "Legendary Creature", ColorIdentity = ["W", "U", "B", "G"], ImageUri = "https://cards.example/atraxa-praetors-voice.jpg", CommanderEligibilityBasis = Domain.Cards.CommanderEligibilityBasis.LegendaryCreature },
            ["Isshin, Two Heavens as One"] = new() { CardId = "isshin-two-heavens-as-one", Name = "Isshin, Two Heavens as One", ManaCost = "{R}{W}{B}", TypeLine = "Legendary Creature", ColorIdentity = ["R", "W", "B"], ImageUri = "https://cards.example/isshin-two-heavens-as-one.jpg", CommanderEligibilityBasis = Domain.Cards.CommanderEligibilityBasis.LegendaryCreature },
            ["Sol Ring"] = new() { CardId = "sol-ring", Name = "Sol Ring", ManaCost = "{1}", TypeLine = "Artifact", ColorIdentity = Array.Empty<string>(), ImageUri = "https://cards.example/sol-ring.jpg", SaltScore = 1.1m, CommanderEligibilityBasis = Domain.Cards.CommanderEligibilityBasis.Unknown },
            ["Lightning Greaves"] = new() { CardId = "lightning-greaves", Name = "Lightning Greaves", ManaCost = "{2}", TypeLine = "Artifact", ColorIdentity = Array.Empty<string>(), ImageUri = "https://cards.example/lightning-greaves.jpg", CommanderEligibilityBasis = Domain.Cards.CommanderEligibilityBasis.Unknown },
            ["Boros Signet"] = new() { CardId = "boros-signet", Name = "Boros Signet", ManaCost = "{2}", TypeLine = "Artifact", ColorIdentity = Array.Empty<string>(), ImageUri = "https://cards.example/boros-signet.jpg", CommanderEligibilityBasis = Domain.Cards.CommanderEligibilityBasis.Unknown },
            ["Wear // Tear"] = new() { CardId = "wear-tear", Name = "Wear // Tear", ManaCost = "{1}{R} // {W}", TypeLine = "Instant", ColorIdentity = ["R", "W"], ImageUri = "https://cards.example/wear-tear.jpg", HasMultipleFaces = true, CommanderEligibilityBasis = Domain.Cards.CommanderEligibilityBasis.Unknown },
            ["Swords to Plowshares"] = new() { CardId = "swords-to-plowshares", Name = "Swords to Plowshares", ManaCost = "{W}", TypeLine = "Instant", ColorIdentity = ["W"], ImageUri = "https://cards.example/swords-to-plowshares.jpg", CommanderEligibilityBasis = Domain.Cards.CommanderEligibilityBasis.Unknown },
            ["Cultivate"] = new() { CardId = "cultivate", Name = "Cultivate", ManaCost = "{2}{G}", TypeLine = "Sorcery", ColorIdentity = ["G"], ImageUri = "https://cards.example/cultivate.jpg", CommanderEligibilityBasis = Domain.Cards.CommanderEligibilityBasis.Unknown },
            ["Arcane Signet"] = new() { CardId = "arcane-signet", Name = "Arcane Signet", ManaCost = "{2}", TypeLine = "Artifact", ColorIdentity = Array.Empty<string>(), ImageUri = "https://cards.example/arcane-signet.jpg", CommanderEligibilityBasis = Domain.Cards.CommanderEligibilityBasis.Unknown },
        };

        public Task<CardSearchResponseContract> SearchAsync(CardSearchQueryContract request, CancellationToken cancellationToken = default)
        {
            var results = Cards.TryGetValue(request.Query, out var result)
                ? new[] { result }
                : Array.Empty<CardSearchResultContract>();

            return Task.FromResult(new CardSearchResponseContract
            {
                SnapshotVersion = "test",
                Results = results,
            });
        }
    }
}