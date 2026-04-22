using System.Text.RegularExpressions;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Domain.Decks;

namespace CommandSynergy.Application.Analysis;

/// <summary>
/// Builds chart-friendly deck statistics from the current deck snapshot and resolved card metadata.
/// </summary>
public sealed partial class DeckStatsCalculationService
{
    private static readonly string[] ManaValueBucketOrder = ["0", "1", "2", "3", "4", "5", "6", "7", "8+"];
    private static readonly string[] ManaColorOrder = ["White", "Blue", "Black", "Red", "Green", "Colorless", "Any"];
    private static readonly string[] CardTypeOrder = ["Creature", "Artifact", "Enchantment", "Instant", "Sorcery", "Planeswalker", "Land", "Battle", "Kindred", "Other"];
    private static readonly IReadOnlyDictionary<string, string> ManaColorLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["W"] = "White",
        ["U"] = "Blue",
        ["B"] = "Black",
        ["R"] = "Red",
        ["G"] = "Green",
        ["C"] = "Colorless",
        ["ANY"] = "Any",
    };
    private static readonly IReadOnlyDictionary<string, int> NumberWords = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["one"] = 1,
        ["two"] = 2,
        ["three"] = 3,
        ["four"] = 4,
        ["five"] = 5,
        ["six"] = 6,
        ["seven"] = 7,
    };

    /// <summary>
    /// Calculates all deck-stat visualizations for the current deck.
    /// </summary>
    public DeckStatsContract Calculate(Deck deck, IReadOnlyDictionary<string, CardProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(deck);
        ArgumentNullException.ThrowIfNull(profiles);

        return new DeckStatsContract
        {
            ManaValueHistogram = BuildManaValueHistogram(deck, profiles),
            ManaCostDistribution = BuildManaCostDistribution(deck, profiles),
            ManaGenerationDistribution = BuildManaGenerationDistribution(deck, profiles),
            CardTypeDistribution = BuildCardTypeDistribution(deck, profiles),
            ManaCurve = BuildManaCurve(deck, profiles),
        };
    }

    private static IReadOnlyList<DeckStatSliceContract> BuildManaValueHistogram(Deck deck, IReadOnlyDictionary<string, CardProfile> profiles)
    {
        Dictionary<string, decimal> counts = new(StringComparer.OrdinalIgnoreCase);

        foreach (var (entry, profile) in EnumerateEntries(deck, profiles, includeCommander: true, includeLands: false))
        {
            var bucket = GetManaValueBucket(profile.ManaValue);
            counts[bucket] = counts.GetValueOrDefault(bucket) + entry.Quantity;
        }

        return CreateSlices(counts, ManaValueBucketOrder, includeEmptyBuckets: true);
    }

    private static IReadOnlyList<DeckStatSliceContract> BuildManaCostDistribution(Deck deck, IReadOnlyDictionary<string, CardProfile> profiles)
    {
        Dictionary<string, decimal> counts = new(StringComparer.OrdinalIgnoreCase);

        foreach (var (entry, profile) in EnumerateEntries(deck, profiles, includeCommander: true, includeLands: false))
        {
            foreach (var match in ManaSymbolRegex().Matches(profile.ManaCost ?? string.Empty).Cast<Match>())
            {
                foreach (var (label, weight) in ParseManaSymbol(match.Groups["token"].Value))
                {
                    counts[label] = counts.GetValueOrDefault(label) + (weight * entry.Quantity);
                }
            }
        }

        return CreateSlices(counts, ManaColorOrder, includeEmptyBuckets: false);
    }

    private static IReadOnlyList<DeckStatSliceContract> BuildManaGenerationDistribution(Deck deck, IReadOnlyDictionary<string, CardProfile> profiles)
    {
        Dictionary<string, decimal> counts = new(StringComparer.OrdinalIgnoreCase);

        foreach (var (entry, profile) in EnumerateEntries(deck, profiles, includeCommander: true, includeLands: true))
        {
            foreach (var match in AddManaRegex().Matches(profile.OracleText ?? string.Empty).Cast<Match>())
            {
                var segment = match.Groups["segment"].Value;
                var amount = GetReferencedManaAmount(segment);

                if (ContainsAnyColorReference(segment))
                {
                    counts["Any"] = counts.GetValueOrDefault("Any") + (amount * entry.Quantity);
                    continue;
                }

                if (ContainsColorlessReference(segment) && ManaSymbolRegex().Matches(segment).Count == 0)
                {
                    counts["Colorless"] = counts.GetValueOrDefault("Colorless") + (amount * entry.Quantity);
                    continue;
                }

                foreach (var tokenMatch in ManaSymbolRegex().Matches(segment).Cast<Match>())
                {
                    foreach (var (label, weight) in ParseManaSymbol(tokenMatch.Groups["token"].Value))
                    {
                        counts[label] = counts.GetValueOrDefault(label) + (weight * entry.Quantity);
                    }
                }
            }
        }

        return CreateSlices(counts, ManaColorOrder, includeEmptyBuckets: false);
    }

    private static IReadOnlyList<DeckStatSliceContract> BuildCardTypeDistribution(Deck deck, IReadOnlyDictionary<string, CardProfile> profiles)
    {
        Dictionary<string, decimal> counts = new(StringComparer.OrdinalIgnoreCase);

        foreach (var (entry, profile) in EnumerateEntries(deck, profiles, includeCommander: true, includeLands: true))
        {
            var cardType = GetPrimaryCardType(profile.TypeLine);
            counts[cardType] = counts.GetValueOrDefault(cardType) + entry.Quantity;
        }

        return CreateSlices(counts, CardTypeOrder, includeEmptyBuckets: false);
    }

    private static ManaCurveContract BuildManaCurve(Deck deck, IReadOnlyDictionary<string, CardProfile> profiles)
    {
        Dictionary<string, decimal> counts = new(StringComparer.OrdinalIgnoreCase);
        decimal weightedManaValue = 0m;
        int spellCount = 0;

        foreach (var (entry, profile) in EnumerateEntries(deck, profiles, includeCommander: false, includeLands: false))
        {
            var bucket = GetManaValueBucket(profile.ManaValue);
            counts[bucket] = counts.GetValueOrDefault(bucket) + entry.Quantity;
            weightedManaValue += profile.ManaValue * entry.Quantity;
            spellCount += entry.Quantity;
        }

        return new ManaCurveContract
        {
            Buckets = CreateSlices(counts, ManaValueBucketOrder, includeEmptyBuckets: true),
            AverageManaValue = spellCount == 0 ? 0m : weightedManaValue / spellCount,
            SpellCount = spellCount,
        };
    }

    private static IEnumerable<(DeckEntry entry, CardProfile profile)> EnumerateEntries(
        Deck deck,
        IReadOnlyDictionary<string, CardProfile> profiles,
        bool includeCommander,
        bool includeLands)
    {
        foreach (var entry in deck.Entries)
        {
            if (!includeCommander && entry.IsCommander)
            {
                continue;
            }

            if (!profiles.TryGetValue(entry.CardId, out var profile))
            {
                continue;
            }

            if (!includeLands && profile.IsLand)
            {
                continue;
            }

            yield return (entry, profile);
        }
    }

    private static IReadOnlyList<DeckStatSliceContract> CreateSlices(
        IReadOnlyDictionary<string, decimal> counts,
        IReadOnlyList<string> order,
        bool includeEmptyBuckets)
    {
        var total = counts.Values.Sum();
        List<DeckStatSliceContract> slices = new(order.Count);

        foreach (var key in order)
        {
            var value = counts.GetValueOrDefault(key);
            if (!includeEmptyBuckets && value <= 0m)
            {
                continue;
            }

            slices.Add(new DeckStatSliceContract
            {
                Label = key,
                Value = value,
                Share = total == 0m ? 0m : value / total,
            });
        }

        return slices;
    }

    private static string GetManaValueBucket(decimal manaValue)
    {
        var wholeManaValue = decimal.ToInt32(decimal.Truncate(manaValue));
        return wholeManaValue >= 8 ? "8+" : wholeManaValue.ToString();
    }

    private static IEnumerable<(string Label, decimal Weight)> ParseManaSymbol(string rawToken)
    {
        var matches = ManaColorKeyRegex().Matches(rawToken);
        if (matches.Count == 0)
        {
            return Array.Empty<(string Label, decimal Weight)>();
        }

        var labels = matches
            .Select(match => ManaColorLabels[match.Value])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var share = 1m / labels.Length;
        return labels.Select(label => (label, share)).ToArray();
    }

    private static bool ContainsAnyColorReference(string segment) =>
        segment.Contains("any color", StringComparison.OrdinalIgnoreCase)
        || segment.Contains("any combination of colors", StringComparison.OrdinalIgnoreCase)
        || segment.Contains("any one color", StringComparison.OrdinalIgnoreCase)
        || segment.Contains("one mana of any type", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsColorlessReference(string segment) =>
        segment.Contains("colorless", StringComparison.OrdinalIgnoreCase);

    private static int GetReferencedManaAmount(string segment)
    {
        var numericMatch = NumericAmountRegex().Match(segment);
        if (numericMatch.Success && int.TryParse(numericMatch.Value, out var numericAmount))
        {
            return Math.Max(numericAmount, 1);
        }

        foreach (var (word, amount) in NumberWords)
        {
            if (segment.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                return amount;
            }
        }

        var tokenCount = ManaSymbolRegex().Matches(segment).Count;
        return tokenCount > 0 ? tokenCount : 1;
    }

    private static string GetPrimaryCardType(string typeLine)
    {
        if (typeLine.Contains("Creature", StringComparison.OrdinalIgnoreCase))
        {
            return "Creature";
        }

        if (typeLine.Contains("Artifact", StringComparison.OrdinalIgnoreCase))
        {
            return "Artifact";
        }

        if (typeLine.Contains("Enchantment", StringComparison.OrdinalIgnoreCase))
        {
            return "Enchantment";
        }

        if (typeLine.Contains("Instant", StringComparison.OrdinalIgnoreCase))
        {
            return "Instant";
        }

        if (typeLine.Contains("Sorcery", StringComparison.OrdinalIgnoreCase))
        {
            return "Sorcery";
        }

        if (typeLine.Contains("Planeswalker", StringComparison.OrdinalIgnoreCase))
        {
            return "Planeswalker";
        }

        if (typeLine.Contains("Land", StringComparison.OrdinalIgnoreCase))
        {
            return "Land";
        }

        if (typeLine.Contains("Battle", StringComparison.OrdinalIgnoreCase))
        {
            return "Battle";
        }

        if (typeLine.Contains("Kindred", StringComparison.OrdinalIgnoreCase))
        {
            return "Kindred";
        }

        return "Other";
    }

    [GeneratedRegex(@"\{(?<token>[^}]+)\}", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex ManaSymbolRegex();

    [GeneratedRegex(@"Add\s+(?<segment>[^\.]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex AddManaRegex();

    [GeneratedRegex(@"[WUBRGC]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex ManaColorKeyRegex();

    [GeneratedRegex(@"\b\d+\b", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex NumericAmountRegex();
}
