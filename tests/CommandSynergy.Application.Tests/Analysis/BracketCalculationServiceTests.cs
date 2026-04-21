using CommandSynergy.Application.Analysis;
using CommandSynergy.Application.Configuration;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Decks;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace CommandSynergy.Application.Tests.Analysis;

public sealed class BracketCalculationServiceTests
{
    [Fact]
    public void Calculate_adds_mass_land_denial_factor_when_present_on_a_card_profile()
    {
        var deck = new Deck();
        deck.UpsertEntry("armageddon", 1);

        var profiles = new Dictionary<string, CardProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["armageddon"] = new()
            {
                CardId = "armageddon",
                Name = "Armageddon",
                ManaValue = 4,
                TypeLine = "Sorcery",
                IsMassLandDenial = true,
                FaceProfiles = [ new CardFaceProfile("0", "Armageddon", null, "Sorcery", null, null, true) ],
            },
        };

        var options = Options.Create(new BracketOptions
        {
            LevelThresholds = [0m, 1m, 2m, 3m, 4m],
            MassLandDenialWeight = 3.0m,
        });

        var sut = new BracketCalculationService(new Domain.Analysis.BracketEngine(), new AnalysisExplanationBuilder(), options);

        var result = sut.Calculate(deck, profiles);

        result.ContributingFactors.Should().ContainSingle(factor =>
            factor.Category == "mass-land-denial"
            && factor.Weight == 3.0m
            && factor.SourceCardId == "armageddon");
    }
}
