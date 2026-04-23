namespace CommandSynergy.Infrastructure.Analysis;

/// <summary>
/// Records cache and analysis telemetry.
/// </summary>
public interface IAnalysisTelemetry
{
    /// <summary>
    /// Records a cache hit for the supplied key.
    /// </summary>
    void RecordCacheHit(string cacheKey);

    /// <summary>
    /// Records a cache miss for the supplied key.
    /// </summary>
    void RecordCacheMiss(string cacheKey);

    /// <summary>
    /// Records a completed analysis result.
    /// </summary>
    void RecordAnalysisCompleted(int bracketLevel, decimal synergyScore);
}
