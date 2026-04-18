using CommandSynergy.Application.Configuration;

namespace CommandSynergy.Application.Analysis;

/// <summary>
/// Provides lookup support for configured high-impact bracket cards.
/// </summary>
public sealed class GameChangerCatalog
{
    private readonly Dictionary<string, GameChangerDefinition> definitions;

    /// <summary>
    /// Creates a catalog from the configured game changer definitions.
    /// </summary>
    public GameChangerCatalog(IEnumerable<GameChangerDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        this.definitions = definitions.ToDictionary(
            definition => definition.CardId,
            definition => definition,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates a catalog from option-bound game changer entries.
    /// </summary>
    public static GameChangerCatalog FromOptions(IEnumerable<BracketGameChangerOption> options) =>
        new(options.Select(option => new GameChangerDefinition(option.CardId, option.Category, option.Weight, option.Explanation)));

    /// <summary>
    /// Tries to resolve a definition by card identifier or oracle identifier.
    /// </summary>
    public bool TryGetDefinition(string cardId, string? oracleId, out GameChangerDefinition? definition)
    {
        if (definitions.TryGetValue(cardId, out definition))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(oracleId) && definitions.TryGetValue(oracleId, out definition))
        {
            return true;
        }

        definition = null;
        return false;
    }
}

/// <summary>
/// Represents a configured game changer match and bracket contribution.
/// </summary>
/// <param name="CardId">The card or oracle identifier to match.</param>
/// <param name="Category">The reported bracket category.</param>
/// <param name="Weight">The configured weight contribution.</param>
/// <param name="Explanation">The explanation used in the bracket result.</param>
public sealed record GameChangerDefinition(string CardId, string Category, decimal Weight, string Explanation);