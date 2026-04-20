using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Application.Decks.Portability;

public sealed record ExportPreview(
    string TargetFormatId,
    string DocumentText,
    IReadOnlyList<string> Warnings,
    DateTimeOffset GeneratedUtc)
{
    public ExportPreviewContract ToContract() => new()
    {
        TargetFormatId = TargetFormatId,
        DocumentText = DocumentText,
        Warnings = Warnings,
        GeneratedUtc = GeneratedUtc,
    };
}