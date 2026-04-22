using System.ComponentModel.DataAnnotations;

namespace CommandSynergy.Application.Configuration;

/// <summary>
/// Represents configurable Commander Spellbook client settings.
/// </summary>
public sealed class CommanderSpellbookOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "CommanderSpellbook";

    /// <summary>
    /// Gets or sets the pinned Commander Spellbook API base URL.
    /// </summary>
    [Required]
    public string BaseUrl { get; set; } = "https://backend.commanderspellbook.com/";

    /// <summary>
    /// Gets or sets the user agent sent to Commander Spellbook.
    /// </summary>
    [Required]
    public string UserAgent { get; set; } = "CommandSynergy/0.1";
}
