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
            Label = ResolveLabel(score),
            Summary = BuildSummary(averageManaValue, fastManaCount, efficientTutorCount, freeInteractionCount, comboCount),
            SupportingSections = BuildSupportingSections(score, averageManaValue, fastManaCount, efficientTutorCount, freeInteractionCount, comboCount),
        };
    }

    private static IReadOnlyList<AnalysisSummarySectionContract> BuildSupportingSections(
        decimal score,
        decimal? averageManaValue,
        int fastManaCount,
        int efficientTutorCount,
        int freeInteractionCount,
        int comboCount)
    {
        var paceItems = new List<AnalysisSummaryItemContract>();

        paceItems.Add(new AnalysisSummaryItemContract
        {
            Label = "Curve",
            Value = averageManaValue is null
                ? "Unavailable"
                : averageManaValue.Value.ToString("0.##", CultureInfo.InvariantCulture),
            Description = averageManaValue switch
            {
                null => "The deck needs more nonland data before its average spell cost can be read.",
                <= 2.5m => "Lean curve supports earlier double-spell turns and faster pressure.",
                <= 3.2m => "Middle curve keeps the deck efficient without fully compressing the game plan.",
                _ => "Heavier curve slows the deck's average start and lowers raw speed.",
            },
            Tone = averageManaValue switch
            {
                null => "neutral",
                <= 2.5m => "positive",
                <= 3.2m => "neutral",
                _ => "warning",
            },
        });

        paceItems.Add(new AnalysisSummaryItemContract
        {
            Label = "Finish speed",
            Value = comboCount == 0 ? "No compact combos" : Pluralize(comboCount, "compact combo"),
            Description = comboCount == 0
                ? "Wins appear to come from board development rather than compact combo closes."
                : "Included combos raise the deck's ceiling and shorten the time needed to close games.",
            Tone = comboCount == 0 ? "neutral" : "positive",
        });

        var signalItems = new List<AnalysisSummaryItemContract>
        {
            new()
            {
                Label = "Fast mana",
                Value = Pluralize(fastManaCount, "piece"),
                Description = fastManaCount == 0
                    ? "No premium acceleration is currently pulling the score upward."
                    : "Fast mana compresses setup turns and pushes the deck toward higher-powered tables.",
                Tone = fastManaCount == 0 ? "neutral" : "positive",
            },
            new()
            {
                Label = "Tutors",
                Value = Pluralize(efficientTutorCount, "piece"),
                Description = efficientTutorCount == 0
                    ? "Lines are less repetitive without efficient search tools."
                    : "Efficient tutors make the deck more consistent and improve access to its best lines.",
                Tone = efficientTutorCount == 0 ? "neutral" : "positive",
            },
            new()
            {
                Label = "Free interaction",
                Value = Pluralize(freeInteractionCount, "piece"),
                Description = freeInteractionCount == 0
                    ? "The deck appears to rely on mana-up interaction windows."
                    : "Zero- or low-cost interaction lets the deck protect key turns without falling behind.",
                Tone = freeInteractionCount == 0 ? "neutral" : "positive",
            },
        };

        return
        [
            new AnalysisSummarySectionContract
            {
                Title = "Read",
                Items = paceItems,
            },
            new AnalysisSummarySectionContract
            {
                Title = "Signals",
                Items = signalItems,
            },
            new AnalysisSummarySectionContract
            {
                Title = "Table fit",
                Items =
                [
                    new AnalysisSummaryItemContract
                    {
                        Label = "Current read",
                        Value = ResolveLabel(score),
                        Description = "This blends speed, consistency, and combo access into a single top-line read.",
                        Tone = score >= 7.0m ? "positive" : score < 4.0m ? "warning" : "neutral",
                    },
                ],
            },
        ];
    }

    private static string BuildSummary(
        decimal? averageManaValue,
        int fastManaCount,
        int efficientTutorCount,
        int freeInteractionCount,
        int comboCount)
    {
        var curveText = averageManaValue is null
            ? "Curve unavailable"
            : $"Average ManaCost {averageManaValue.Value.ToString("0.##", CultureInfo.InvariantCulture)}";

        return $"{curveText}; {Pluralize(fastManaCount, "fast mana card")}, {Pluralize(efficientTutorCount, "efficient tutor")}, {Pluralize(freeInteractionCount, "free interaction piece")}, and {Pluralize(comboCount, "included combo")}.";
    }

    private static string Pluralize(int count, string singularLabel) =>
        count == 1
            ? $"1 {singularLabel}"
            : $"{count.ToString(CultureInfo.InvariantCulture)} {singularLabel}s";

    private static string ResolveLabel(decimal score) => score switch
    {
        >= 8.5m => "Explosive",
        >= 7.0m => "Tuned",
        >= 5.5m => "Focused",
        >= 4.0m => "Measured",
        _ => "Deliberate",
    };
}
