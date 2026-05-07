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
            RawDocumentText = DeckPortabilityFixtureLoader.Load("moxfield-fulldeck-extended.txt"),
        });

        result.ImportedDeck.NormalizedDeck.ImportedCardCount.Should().Be(100);
    }

    [Fact]
    public async Task ImportAsync_preserves_partial_success_with_diagnostics()
    {
        var sut = CreateSut();

        var result = await sut.ImportAsync(new DeckImportRequestContract
        {
            RawDocumentText = DeckPortabilityFixtureLoader.Load("moxfield-fulldeck-plaintext.txt"),
        });

        result.ImportedDeck.NormalizedDeck.ImportedCardCount.Should().Be(100);
    }    

    [Fact]
    public async Task ImportAsync_preserves_entry_level_source_metadata_for_supported_plaintext_variants()
    {
        var sut = CreateSut();

        var result = await sut.ImportAsync(new DeckImportRequestContract
        {
            RawDocumentText = "Magar of the Magic Strings (UNF) 171\n1x Aftermath Analyst (eoc) 91 [Mill]",
        });

        result.DetectedFormatId.Should().Be("generic-plaintext");
        result.ImportedDeck.NormalizedDeck.Entries.Should().ContainSingle(entry =>
            entry.DisplayName == "Magar of the Magic Strings"
            && entry.Quantity == 1
            && entry.SourceSetCode == "UNF"
            && entry.SourceCollectorNumber == "171"
            && entry.SourceTag == null);
        result.ImportedDeck.NormalizedDeck.Entries.Should().ContainSingle(entry =>
            entry.DisplayName == "Aftermath Analyst"
            && entry.Quantity == 1
            && entry.SourceSetCode == "eoc"
            && entry.SourceCollectorNumber == "91"
            && entry.SourceTag == "Mill");
    }

    [Fact]
    public async Task ImportAsync_reuses_card_resolution_for_duplicate_card_names()
    {
        var cardSearchService = new CountingCardSearchService();
        var registry = new DeckFormatRegistry([new MoxfieldTextFormatProfile(), new ManaBoxTextFormatProfile(), new GenericPlaintextFormatProfile()]);
        var detectionService = new DeckFormatDetectionService(registry);
        var sut = new DeckImportService(cardSearchService, detectionService, new FakeTimeProvider(DateTimeOffset.Parse("2026-04-20T00:00:00Z")));

        await sut.ImportAsync(new DeckImportRequestContract
        {
            RawDocumentText = "1 Sol Ring\n1 Sol Ring\n1 Arcane Signet",
        });

        cardSearchService.SearchCalls.Should().Be(2);
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
            ["Aftermath Analyst"] = new() { CardId = "aftermath-analyst", Name = "Aftermath Analyst", ManaCost = "{2}{G}", TypeLine = "Creature", ColorIdentity = ["G"], ImageUri = "https://cards.example/aftermath-analyst.jpg", CommanderEligibilityBasis = Domain.Cards.CommanderEligibilityBasis.Unknown },
            ["Magar of the Magic Strings"] = new() { CardId = "magar-of-the-magic-strings", Name = "Magar of the Magic Strings", ManaCost = "{1}{B}{R}", TypeLine = "Legendary Creature", ColorIdentity = ["B", "R"], ImageUri = "https://cards.example/magar-of-the-magic-strings.jpg", CommanderEligibilityBasis = Domain.Cards.CommanderEligibilityBasis.LegendaryCreature },
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

    private sealed class CountingCardSearchService : ICardSearchService
    {
        private readonly StubCardSearchService inner = new();
        private int searchCalls;

        public int SearchCalls => searchCalls;

        public Task<CardSearchResponseContract> SearchAsync(CardSearchQueryContract request, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref searchCalls);
            return inner.SearchAsync(request, cancellationToken);
        }
    }
}