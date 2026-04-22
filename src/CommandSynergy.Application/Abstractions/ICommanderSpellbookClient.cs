using CommandSynergy.Domain.Analysis;

namespace CommandSynergy.Application.Abstractions;

/// <summary>
/// Represents an application-facing Commander Spellbook client abstraction.
/// </summary>
public interface ICommanderSpellbookClient
{
    /// <summary>
    /// Finds combos for the supplied commander and main-deck card names.
    /// </summary>
    Task<ComboAnalysis> FindCombosAsync(IEnumerable<string> commanderNames, IEnumerable<string> mainDeckNames, CancellationToken cancellationToken = default);
}
