using CommandSynergy.Domain.Cards;

namespace CommandSynergy.Application.Abstractions;

/// <summary>
/// Represents an application-facing EDHREC client abstraction.
/// </summary>
public interface IEdhrecClient
{
    /// <summary>
    /// Loads commander-specific synergy data for the supplied commander profile.
    /// </summary>
    Task<CommanderThemeInsights> GetCommanderThemeInsightsAsync(CardProfile commanderProfile, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents flattened EDHREC commander synergy data.
/// </summary>
public sealed record CommanderThemeInsights(
    string Slug,
    bool IsAvailable,
    IReadOnlyDictionary<string, decimal> SynergyByCardId)
{
    /// <summary>
    /// Gets an empty EDHREC result.
    /// </summary>
    public static CommanderThemeInsights Empty(string slug = "") => new(slug, false, new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase));
}