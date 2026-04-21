using System.ComponentModel.DataAnnotations;

namespace CommandSynergy.Application.Configuration;

/// <summary>
/// Represents configurable EDHREC client settings.
/// </summary>
public sealed class EdhrecOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Edhrec";

    /// <summary>
    /// Gets or sets the pinned EDHREC JSON API base URL.
    /// </summary>
    [Required]
    public string BaseUrl { get; set; } = "https://json.edhrec.com/";

    /// <summary>
    /// Gets or sets the user agent sent to EDHREC.
    /// </summary>
    [Required]
    public string UserAgent { get; set; } = "CommandSynergy/0.1";
}