namespace CommandSynergy.Application.Decks.Portability;

/// <summary>
/// Detects which import format best matches a raw deck document.
/// </summary>
public interface IDeckFormatDetectionService
{
    /// <summary>
    /// Detects the most suitable deck format for the supplied document.
    /// </summary>
    DeckFormatSelectionResult Detect(string documentText, string? hintedFormatId = null);
}
