namespace CommandSynergy.Application.Decks.Portability.Formats;

public sealed class MoxfieldTextFormatProfile : DeckFormatProfileBase
{
    public override string FormatId => "moxfield-text";

    public override string DisplayName => "Moxfield Text";

    public override int Detect(string documentText)
    {
        var score = 0;
        if (documentText.Contains("Commander:", StringComparison.OrdinalIgnoreCase))
        {
            score += 3;
        }

        if (documentText.Contains("Deck:", StringComparison.OrdinalIgnoreCase))
        {
            score += 3;
        }

        if (documentText.Contains("Maybeboard:", StringComparison.OrdinalIgnoreCase))
        {
            score += 1;
        }

        return score;
    }

    public override FormatParseResult Parse(string documentText)
    {
        return MoxfieldStyleParser.Parse(documentText, line => line.EndsWith(':'));
    }

    public override string Render(PortableDeckSnapshot snapshot, IReadOnlyList<string> warnings)
    {
        return MoxfieldStyleRenderer.Render(snapshot, includeBracketHeaders: false);
    }
}