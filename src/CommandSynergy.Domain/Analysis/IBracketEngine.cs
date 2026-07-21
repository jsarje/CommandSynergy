namespace CommandSynergy.Domain.Analysis;

/// <summary>
/// Maps weighted bracket factors to an official bracket level.
/// </summary>
public interface IBracketEngine
{
    /// <summary>
    /// Calculates a bracket assessment from the supplied normalized bracket inputs.
    /// </summary>
    BracketAssessment Calculate(BracketResolutionInput input);
}
