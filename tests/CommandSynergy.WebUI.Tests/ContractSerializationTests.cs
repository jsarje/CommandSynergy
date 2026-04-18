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
                Summary = "The deck is strongly aligned with the commander plan.",
                CommanderSpecificHits = ["graveyard enabler"],
                StapleOverloadIndicators = ["rhystic study"],
            },
        };

        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        var roundTrip = JsonSerializer.Deserialize<DeckAnalysisResponseContract>(json, SerializerOptions);

        json.Should().Contain("\"level\":3");
        json.Should().Contain("\"commanderSpecificHits\"");
        roundTrip.Should().NotBeNull();
        roundTrip!.Bracket.Level.Should().Be(3);
        roundTrip.Synergy.StapleOverloadIndicators.Should().ContainSingle("rhystic study");
    }
}