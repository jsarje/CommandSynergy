using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace CommandSynergy.Domain.Analysis;

/// <summary>
/// Provides the canonical static theme taxonomy used for Commander deck analysis.
/// </summary>
public static class ThemeTaxonomy
{
    private static readonly RegexOptions RegexOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking;
    private static readonly IReadOnlyList<ThemeDefinition> Definitions = BuildDefinitions();
    private static readonly IReadOnlyDictionary<string, ThemeDefinition> DefinitionsByName =
        new ReadOnlyDictionary<string, ThemeDefinition>(Definitions.ToDictionary(definition => definition.Name, StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Gets the default taxonomy definitions.
    /// </summary>
    public static IReadOnlyList<ThemeDefinition> Default => Definitions;

    /// <summary>
    /// Gets a theme definition by name.
    /// </summary>
    public static ThemeDefinition? GetByName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return DefinitionsByName.TryGetValue(name, out var definition)
            ? definition
            : null;
    }

    private static IReadOnlyList<ThemeDefinition> BuildDefinitions() =>
    [
        Create("Ramp", "Accelerates mana production and land development.", ["Landfall"], [@"search your library for (a|up to .*?) land", @"add \{", @"additional land"], @"\bLand\b"),
        Create("Card Draw / Wheels", "Refills hands or resets the table's resources.", ["Cycling"], [@"draw (a|two|three|four|five|x) card", @"each player draws", @"discard (their|your) hand", @"wheel"], null),
        Create("Spellslinger", "Rewards chaining instants and sorceries.", ["Prowess", "Magecraft", "Storm"], [@"instant or sorcery", @"cast an instant", @"copy target instant", @"copy target sorcery"], @"\b(Instant|Sorcery)\b"),
        Create("Combo", "Uses compact interactions and tutors to assemble a game-ending engine.", ["Storm"], [@"search your library", @"untap target permanent", @"take an extra turn", @"exile target player's library", @"player loses the game"], null, signalConfidence: "Low"),
        Create("Control / Stax", "Constrains opponents while trading efficiently.", ["Flash"], [@"counter target spell", @"destroy target", @"exile target", @"tap all", @"players can't", @"opponents can't"], null),
        Create("Pillowfort", "Discourages attacks and prolongs the game.", [], [@"can't attack you", @"must attack", @"prevent all combat damage", @"you don't lose the game"], null),
        Create("Tribal", "Builds around a single creature type.", [], [@"\b(tribal|kindred)\b", @"creatures you control of the chosen type", @"choose a creature type"], @"\b(Creature|Kindred)\b"),
        Create("Tokens", "Creates and multiplies creature or artifact tokens.", ["Populate"], [@"create (a|two|three|x).* token", @"token", @"populate"], null),
        Create("Voltron", "Concentrates power on one attacker, usually the commander.", ["Equip"], [@"attach to", @"equipped creature", @"commander deals combat damage"], null),
        Create("Aggro", "Pushes combat damage quickly and repeatedly.", ["Haste", "First strike", "Double strike"], [@"deal combat damage", @"attack each combat", @"whenever .* attacks"], null),
        Create("Enchantments", "Leverages enchantments as the core engine.", ["Constellation"], [@"enchantment you control", @"whenever an enchantment", @"enchantment enters"], @"\bEnchantment\b"),
        Create("Artifacts", "Builds around artifact density and recursion.", ["Affinity", "Improvise"], [@"artifact you control", @"artifact enters", @"sacrifice an artifact"], @"\bArtifact\b"),
        Create("Equipment", "Centers around Equipment tutoring and combat scaling.", ["Equip"], [@"equipment you control", @"equipment enters", @"equip"], @"\bEquipment\b"),
        Create("Auras", "Uses Auras to enhance creatures or generate value.", ["Bestow"], [@"enchanted creature", @"aura", @"enchant creature"], @"\bAura\b"),
        Create("+1/+1 Counters", "Stacks +1/+1 counters as the primary growth engine.", ["Proliferate", "Bolster", "Adapt", "Outlast"], [@"\+1/\+1 counter"], null),
        Create("Counters Matter", "Manipulates counters of many kinds for value.", ["Proliferate"], [@"remove a counter", @"counter on it", @"counter of any kind", @"loyalty counter"], null),
        Create("Reanimator", "Returns creatures or permanents from graveyards to the battlefield.", [], [@"return target .* from your graveyard to the battlefield", @"put .* from a graveyard onto the battlefield", @"reanimate"], null),
        Create("Aristocrats", "Sacrifices creatures for death triggers and life drain.", ["Exploit"], [@"sacrifice a creature", @"whenever a creature dies", @"each opponent loses life"], null),
        Create("Self-Mill / Dredge", "Loads the graveyard as a resource.", ["Dredge"], [@"mill \d+", @"put the top .* into your graveyard", @"from your library into your graveyard"], null),
        Create("Blink / Flicker", "Reuses enter-the-battlefield effects by exiling and returning permanents.", [], [@"exile .* then return", @"return it to the battlefield", @"flicker"], null),
        Create("Life Gain", "Accumulates life to fuel payoffs or survivability.", ["Lifelink"], [@"you gain life", @"gain life equal"], null),
        Create("Mill", "Empties opponents' libraries.", [], [@"target player mills", @"put the top .* of target player's library into their graveyard"], null),
        Create("Lands Matter", "Rewards land drops and land-based value loops.", ["Landfall"], [@"land enters the battlefield", @"land you control", @"return target land"], @"\bLand\b"),
        Create("Land Destruction", "Attacks mana bases directly.", [], [@"destroy target land", @"sacrifice a land", @"return target land to its owner's hand"], null),
        Create("Extra Turns", "Takes additional turns as a primary payoff.", [], [@"take an extra turn", @"additional turn"], null),
        Create("Extra Combats", "Creates extra combat steps and repeated attack triggers.", [], [@"additional combat phase", @"after this phase, there is an additional combat phase", @"untap all creatures you control"], null),
        Create("Infect / Poison", "Wins through poison counters or toxic combat.", ["Infect", "Toxic", "Poisonous"], [@"poison counter"], null),
        Create("Group Hug / Group Slug", "Gives everyone resources or punishes the whole table symmetrically.", [], [@"each player draws", @"each player gains", @"each opponent loses", @"each player sacrifices"], null),
        Create("Superfriends", "Leverages planeswalker density and loyalty effects.", [], [@"planeswalker you control", @"loyalty counter"], @"\bPlaneswalker\b"),
        Create("Sacrifice", "Uses sacrifice as a repeated engine even outside Aristocrats shells.", [], [@"sacrifice a permanent", @"whenever you sacrifice", @"sacrifice another"], null),
        Create("Discard", "Attacks opponents' hands or rewards discard.", ["Madness"], [@"discard a card", @"each player discards", @"whenever you discard"], null),
        Create("Burn", "Converts cards and mana into direct damage.", [], [@"deals? \d+ damage to any target", @"deals? .* damage to each opponent", @"damage to any target"], null),
        Create("Hatebears", "Deploys creatures that tax, lock, or restrict opponents.", [], [@"players can't", @"opponents can't", @"spells your opponents cast cost", @"activated abilities can't be activated"], @"\bCreature\b"),
        Create("Goodstuff", "Residual label for individually powerful cards without a dominant shared engine.", [], [], null, signalConfidence: "Low"),
    ];

    private static ThemeDefinition Create(
        string name,
        string description,
        IReadOnlyList<string> keywordPatterns,
        IReadOnlyList<string> oraclePatterns,
        string? typePattern,
        decimal keywordWeight = 0.4m,
        decimal oracleWeight = 0.25m,
        decimal typeWeight = 0.3m,
        string signalConfidence = "High") => new(
            name,
            description,
            keywordPatterns,
            oraclePatterns.Select(static pattern => new Regex(pattern, RegexOptions)).ToArray(),
            string.IsNullOrWhiteSpace(typePattern) ? null : new Regex(typePattern, RegexOptions),
            keywordWeight,
            oracleWeight,
            typeWeight,
            signalConfidence);
}