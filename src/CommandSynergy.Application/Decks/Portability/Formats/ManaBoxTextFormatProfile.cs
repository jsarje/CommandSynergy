namespace CommandSynergy.Application.Decks.Portability.Formats;

public sealed class ManaBoxTextFormatProfile : DeckFormatProfileBase
{
    public override string FormatId => "manabox-text";

    public override string DisplayName => "ManaBox Text";

    public override int Detect(string documentText)
    {
        var score = 0;
        if (documentText.Contains("[Commander]", StringComparison.OrdinalIgnoreCase))
        {
            score += 4;
        }

        if (documentText.Contains("[Mainboard]", StringComparison.OrdinalIgnoreCase))
        {
            score += 4;
        }

        return score;
    }

    public override FormatParseResult Parse(string documentText)
    {
        return MoxfieldStyleParser.Parse(documentText, line => line.StartsWith('[') && line.EndsWith(']'));
    }

    public override string Render(PortableDeckSnapshot snapshot, IReadOnlyList<string> warnings)
    {
        return MoxfieldStyleRenderer.Render(snapshot, includeBracketHeaders: true);
    }
}