using CommandSynergy.Application.Analysis;
using CommandSynergy.Application.Configuration;
using Microsoft.Extensions.Options;

namespace CommandSynergy.Infrastructure.Configuration;

/// <summary>
/// Loads the configured game changer catalog used by bracket analysis.
/// </summary>
public sealed class BracketCatalogLoader
{
    private readonly IOptions<BracketOptions> options;

    /// <summary>
    /// Creates a loader for bracket catalog configuration.
    /// </summary>
    public BracketCatalogLoader(IOptions<BracketOptions> options)
    {
        this.options = options;
    }

    /// <summary>
    /// Loads the current configured game changer catalog.
    /// </summary>
    public GameChangerCatalog Load() => GameChangerCatalog.FromOptions(options.Value.GameChangers);
}