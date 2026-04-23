using System.Globalization;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Domain.Analysis;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Decks;

namespace CommandSynergy.Application.Analysis;

/// <summary>
/// Calculates a heuristic Commander power-level score for a submitted deck.
/// </summary>
public sealed class PowerLevelCalculationService : IPowerLevelCalculationService
{
    private static readonly HashSet<string> FastMana = new(StringComparer.OrdinalIgnoreCase)
    {
        "Mana Crypt", "Jeweled Lotus", "Mox Diamond", "Chrome Mox",
        "Mox Opal", "Mox Amber", "Lotus Petal", "Lion's Eye Diamond",
        "Sol Ring", "Mana Vault", "Grim Monolith", "Basalt Monolith",
        "Dark Ritual", "Cabal Ritual", "Rite of Flame", "Culling the Weak",
        "Burnt Offering", "Sacrifice", "Rain of Filth", "Jeska's Will",
        "Dockside Extortionist", "Elvish Spirit Guide", "Simian Spirit Guide",
        "Tinder Wall", "Blood Pet", "Carpet of Flowers", "Gaea's Cradle",
        "Serra's Sanctum", "Ancient Tomb", "City of Traitors",
    };

    private static readonly HashSet<string> FreeInteraction = new(StringComparer.OrdinalIgnoreCase)
    {
        "Fierce Guardianship", "Deflecting Swat", "Deadly Rollick", "Flawless Maneuver", "Obscuring Haze",
        "Force of Will", "Force of Negation", "Force of Vigor", "Force of Despair",
        "Misdirection", "Commandeer", "Foil", "Pyrokinesis", "Cave-In",
        "Subtlety", "Endurance", "Solitude", "Grief", "Fury",
        "Flare of Denial", "Flare of Malice", "Flare of Fortitude", "Flare of Duplication",
        "Disrupting Shoal", "Sickening Shoal", "Shining Shoal",
        "Pact of Negation", "Slaughter Pact", "Summoner's Pact", "Mental Misstep",
        "Mindbreak Trap", "Noxious Revival", "Gut Shot",
        "Snuff Out", "Daze", "Thunderclap", "Mine Collapse", "Mogg Salvage",
        "Massacre", "Abolish", "Reverent Silence", "Invigorate", "Submerge",
    };

    private static readonly HashSet<string> EfficientTutors = new(StringComparer.OrdinalIgnoreCase)
    {
        "Vampiric Tutor", "Imperial Seal", "Enlightened Tutor", "Mystical Tutor",
        "Worldly Tutor", "Sylvan Tutor", "Personal Tutor", "Gamble", "Entomb",
        "Crop Rotation", "Green Sun's Zenith", "Steelshaper's Gift", "Scheming Symmetry",
        "Demonic Tutor", "Diabolic Intent", "Tainted Pact", "Demonic Consultation",
        "Survival of the Fittest", "Neoform", "Profane Tutor", "Muddle the Mixture",
        "Finale of Devastation", "Invasion of Ikoria", "Sylvan Scrying", "Merchant Scroll",
        "Grim Tutor", "Buried Alive", "Intuition", "Eldritch Evolution", "Fabricate",
        "Idyllic Tutor", "Chord of Calling", "Doomsday", "Search for Glory", "Solve the Equation",
        "Wishclaw Talisman", "Spellseeker", "Trinket Mage", "Imperial Recruiter",
        "Recruiter of the Guard", "Stoneforge Mystic", "Goblin Engineer", "Oswald Fiddlebender",
    };

    /// <summary>
    /// Calculates the heuristic power level for the supplied deck and combo analysis.
    /// </summary>
    public PowerLevelAssessmentContract Calculate(
        Deck deck,
        IReadOnlyDictionary<string, CardProfile> profiles,
        ComboAnalysis comboAnalysis)
    {
        ArgumentNullException.ThrowIfNull(deck);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(comboAnalysis);

        var score = 4.0m;
        var totalManaValue = 0m;
        var nonLandCount = 0;
        var fastManaCount = 0;
        var efficientTutorCount = 0;
        var freeInteractionCount = 0;

        foreach (var entry in deck.Entries)
        {
            if (!profiles.TryGetValue(entry.CardId, out var profile))
            {
                continue;
            }

            if (!profile.IsLand)
            {
                totalManaValue += profile.ManaValue * entry.Quantity;
                nonLandCount += entry.Quantity;
            }

            if (FastMana.Contains(profile.Name))
            {
                fastManaCount += entry.Quantity;
            }

            if (EfficientTutors.Contains(profile.Name))
            {
                efficientTutorCount += entry.Quantity;
            }

            if (FreeInteraction.Contains(profile.Name))
            {
                freeInteractionCount += entry.Quantity;
            }
        }

        decimal? averageManaValue = null;
        if (nonLandCount > 0)
        {
            averageManaValue = totalManaValue / nonLandCount;
            score += averageManaValue switch
            {
                <= 1.8m => 2.5m,
                <= 2.5m => 1.5m,
                <= 3.2m => 0.5m,
                _ => -0.5m,
            };
        }

        score += fastManaCount * 0.4m;
        score += efficientTutorCount * 0.25m;
        score += freeInteractionCount * 0.3m;

        var comboCount = comboAnalysis.IncludedCombos.Count;
        if (comboCount > 0)
        {
            score += 1.5m + (comboCount * 0.2m);
        }

        score = decimal.Round(decimal.Clamp(score, 1.0m, 10.0m), 1, MidpointRounding.AwayFromZero);

        return new PowerLevelAssessmentContract
        {
            Score = score,
            Summary = BuildSummary(averageManaValue, fastManaCount, efficientTutorCount, freeInteractionCount, comboCount),
        };
    }

    private static string BuildSummary(
        decimal? averageManaValue,
        int fastManaCount,
        int efficientTutorCount,
        int freeInteractionCount,
        int comboCount)
    {
        var curveText = averageManaValue is null
            ? "curve unavailable"
            : $"avg MV {averageManaValue.Value.ToString("0.##", CultureInfo.InvariantCulture)}";

        return $"{curveText}; {Pluralize(fastManaCount, "fast mana card")}, {Pluralize(efficientTutorCount, "efficient tutor")}, {Pluralize(freeInteractionCount, "free interaction piece")}, and {Pluralize(comboCount, "included combo")}.";
    }

    private static string Pluralize(int count, string singularLabel) =>
        count == 1
            ? $"1 {singularLabel}"
            : $"{count.ToString(CultureInfo.InvariantCulture)} {singularLabel}s";
}
