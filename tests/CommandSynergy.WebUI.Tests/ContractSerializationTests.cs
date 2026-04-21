using System.Text.Json;
using CommandSynergy.Application.Contracts;
using FluentAssertions;

namespace CommandSynergy.WebUI.Tests;

public sealed class ContractSerializationTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Deck_validation_contracts_round_trip_with_expected_json_shape()
    {
        var payload = new DeckValidationResponseContract
        {
            IsValid = false,
            DeckCardCount = 99,
            Findings =
            [
                new ValidationFindingContract
                {
                    Severity = "error",
                    Code = "deck-size",
                    Message = "Commander decks must contain exactly 100 cards.",
                    AffectedCardIds = ["sol-ring"],
                },
            ],
        };

        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        var roundTrip = JsonSerializer.Deserialize<DeckValidationResponseContract>(json, SerializerOptions);

        json.Should().Contain("\"isValid\":false");
        json.Should().Contain("\"deckCardCount\":99");
        json.Should().Contain("\"severity\":\"error\"");
        roundTrip.Should().NotBeNull();
        roundTrip!.Findings.Should().ContainSingle();
        roundTrip.Findings[0].Code.Should().Be("deck-size");
    }

    [Fact]
    public void Deck_analysis_contracts_round_trip_with_nested_collections()
    {
        var payload = new DeckAnalysisResponseContract
        {
            Bracket = new BracketAssessmentContract
            {
                Level = 3,
                TotalWeight = 12.5m,
                Summary = "Mid-power table",
                Factors =
                [
                    new BracketFactorContract
                    {
                        Category = "acceleration",
                        Weight = 3.5m,
                        Explanation = "Fast mana density is above baseline.",
                        SourceCardId = "mana-crypt",
                    },
                ],
            },
            Synergy = new SynergyAssessmentContract
            {
                Score = 74.2m,
                ThemeScore = 72.5m,
                FinalScore = 74.2m,
                QualitativeLabel = "Focused",
                EdhrecEnhanced = true,
                Summary = "The deck is strongly aligned with the commander plan.",
                CommanderSpecificHits = ["graveyard enabler"],
                StapleOverloadIndicators = ["rhystic study"],
            },
            ThemeAnalysis = new ThemeAnalysisContract
            {
                RankedThemes =
                [
                    new DeckThemeContract
                    {
                        Name = "Tokens",
                        Description = "Creates a wide board.",
                        Strength = 0.70m,
                        StrengthLabel = "Strong",
                        ContributingCardIds = ["secure-the-wastes"],
                        ContributingCardCount = 1,
                        Contributors =
                        [
                            new ThemeContributorContract
                            {
                                CardId = "secure-the-wastes",
                                CardName = "Secure the Wastes",
                                Signal = 0.70m,
                                Reason = "Matched the card's oracle text.",
                            },
                        ],
                    },
                ],
                PrimaryThemes = Array.Empty<DeckThemeContract>(),
                OffThemeCards =
                [
                    new OffThemeCardContract
                    {
                        CardId = "rhystic-study",
                        CardName = "Rhystic Study",
                        Reason = "No strong theme signal was detected for this card.",
                        MetadataUnavailable = true,
                    },
                ],
                CommanderAlignment = new CommanderAlignmentContract
                {
                    Level = "Strong",
                    CommanderTopTheme = "Tokens",
                    DeckStrengthForCommanderTheme = 0.70m,
                    EvidenceCardIds = ["secure-the-wastes"],
                    Summary = "The 99 strongly reinforce the commander's plan.",
                },
                AnalysedCardCount = 100,
                AnalysedAtUtc = DateTimeOffset.Parse("2026-04-21T00:00:00Z"),
                UsedEdhrecFallback = true,
            },
        };

        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        var roundTrip = JsonSerializer.Deserialize<DeckAnalysisResponseContract>(json, SerializerOptions);

        json.Should().Contain("\"level\":3");
        json.Should().Contain("\"commanderSpecificHits\"");
        json.Should().Contain("\"themeAnalysis\"");
        json.Should().Contain("\"usedEdhrecFallback\":true");
        json.Should().Contain("\"metadataUnavailable\":true");
        roundTrip.Should().NotBeNull();
        roundTrip!.Bracket.Level.Should().Be(3);
        roundTrip.Synergy.StapleOverloadIndicators.Should().ContainSingle("rhystic study");
        roundTrip.ThemeAnalysis!.CommanderAlignment.Level.Should().Be("Strong");
        roundTrip.ThemeAnalysis.OffThemeCards[0].MetadataUnavailable.Should().BeTrue();
    }

    [Fact]
    public void Deck_portability_contracts_round_trip_with_expected_json_shape()
    {
        var payload = new ImportedDeckLibraryDocumentContract
        {
            SchemaVersion = DeckPortabilityContract.CurrentSchemaVersion,
            ActiveDeckId = "deck-1",
            LastSavedUtc = DateTimeOffset.Parse("2026-04-20T00:00:00Z"),
            Decks =
            [
                new ImportedDeckRecordContract
                {
                    ImportedDeckId = "deck-1",
                    Name = "Atraxa Blink",
                    SourceFormatId = "moxfield-text",
                    ImportedAtUtc = DateTimeOffset.Parse("2026-04-20T00:00:00Z"),
                    NormalizedDeck = new PortableDeckSnapshotContract
                    {
                        DeckName = "Atraxa Blink",
                        CommanderCardIds = ["atraxa-praetors-voice"],
                        ImportedCardCount = 2,
                        HasUnresolvedLines = false,
                        Entries =
                        [
                            new PortableDeckEntryContract
                            {
                                CardId = "atraxa-praetors-voice",
                                DisplayName = "Atraxa, Praetors' Voice",
                                ManaCost = "{G}{W}{U}{B}",
                                TypeLine = "Legendary Creature",
                                ColorIdentity = ["W", "U", "B", "G"],
                                ImageUri = "https://cards.example/atraxa-praetors-voice.jpg",
                                CommanderEligibilityBasis = Domain.Cards.CommanderEligibilityBasis.LegendaryCreature,
                                Quantity = 1,
                                SectionId = "commander",
                                ParseConfidence = "exact",
                                IsCommander = true,
                                SourceSetCode = "2XM",
                                SourceCollectorNumber = "190",
                                SourceTag = "Blink",
                            },
                        ],
                        Sections =
                        [
                            new DeckSectionStateContract
                            {
                                SectionId = "commander",
                                DisplayName = "Commander",
                                Role = "commander",
                            },
                        ],
                    },
                    Diagnostics =
                    [
                        new ImportDiagnosticContract
                        {
                            DiagnosticId = "diag-1",
                            Severity = "warning",
                            Code = "unrecognized-line",
                            Message = "Example warning",
                        },
                    ],
                },
            ],
        };

        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        var roundTrip = JsonSerializer.Deserialize<ImportedDeckLibraryDocumentContract>(json, SerializerOptions);

        json.Should().Contain($"\"schemaVersion\":{DeckPortabilityContract.CurrentSchemaVersion}");
        json.Should().Contain("\"sourceFormatId\":\"moxfield-text\"");
        json.Should().Contain("\"parseConfidence\":\"exact\"");
        json.Should().Contain("\"imageUri\":\"https://cards.example/atraxa-praetors-voice.jpg\"");
        json.Should().Contain("\"sourceSetCode\":\"2XM\"");
        json.Should().Contain("\"sourceCollectorNumber\":\"190\"");
        json.Should().Contain("\"sourceTag\":\"Blink\"");
        roundTrip.Should().NotBeNull();
        roundTrip!.Decks.Should().ContainSingle();
        roundTrip.Decks[0].NormalizedDeck.CommanderCardIds.Should().ContainSingle("atraxa-praetors-voice");
        roundTrip.Decks[0].NormalizedDeck.Entries[0].TypeLine.Should().Be("Legendary Creature");
        roundTrip.Decks[0].NormalizedDeck.Entries[0].SourceTag.Should().Be("Blink");
    }
}