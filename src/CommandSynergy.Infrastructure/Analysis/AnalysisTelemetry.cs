using Microsoft.Extensions.Logging;

namespace CommandSynergy.Infrastructure.Analysis;

/// <summary>
/// Records structured telemetry for deck analysis execution.
/// </summary>
public sealed class AnalysisTelemetry
{
    private readonly ILogger<AnalysisTelemetry> logger;

    /// <summary>
    /// Creates an analysis telemetry helper.
    /// </summary>
    public AnalysisTelemetry(ILogger<AnalysisTelemetry> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Records a cache hit for a deck analysis request.
    /// </summary>
    public void RecordCacheHit(string cacheKey)
    {
        logger.LogInformation("Deck analysis cache hit for {CacheKey}", cacheKey);
    }

    /// <summary>
    /// Records a cache miss for a deck analysis request.
    /// </summary>
    public void RecordCacheMiss(string cacheKey)
    {
        logger.LogInformation("Deck analysis cache miss for {CacheKey}", cacheKey);
    }

    /// <summary>
    /// Records a successful analysis execution.
    /// </summary>
    public void RecordAnalysisCompleted(int bracketLevel, decimal synergyScore)
    {
        logger.LogInformation(
            "Deck analysis completed with bracket level {BracketLevel} and synergy score {SynergyScore}",
            bracketLevel,
            synergyScore);
    }
}