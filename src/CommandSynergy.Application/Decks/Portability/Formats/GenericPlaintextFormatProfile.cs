namespace CommandSynergy.Application.Decks.Portability.Formats;

public sealed class GenericPlaintextFormatProfile : DeckFormatProfileBase
{
    public override string FormatId => "generic-plaintext";

    public override string DisplayName => "Generic Plaintext";

    public override int Detect(string documentText)
    {
        var quantityLines = documentText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Count(line => TryParseEntryLine(line, out _, out _, out _, out _, out _));

        return quantityLines > 0 ? 1 : 0;
    }

    public override FormatParseResult Parse(string documentText)
    {
        return MoxfieldStyleParser.Parse(documentText, static line =>
            line.TrimStart().StartsWith('#') || line.EndsWith(':'));
    }

    public override string Render(PortableDeckSnapshot snapshot, IReadOnlyList<string> warnings)
    {
        return MoxfieldStyleRenderer.Render(snapshot, includeBracketHeaders: false, includeHashHeaders: true);
    }
}