using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks.Portability.Formats;

namespace CommandSynergy.Application.Decks.Portability;

public sealed class DeckExportService(IDeckFormatRegistry deckFormatRegistry) : IDeckExportService
{
    private readonly IDeckFormatRegistry deckFormatRegistry = deckFormatRegistry;

    public Task<DeckExportResultContract> ExportAsync(DeckExportRequestContract request, PortableDeckSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(snapshot);

        var profile = deckFormatRegistry.GetById(request.TargetFormatId)
            ?? throw new InvalidOperationException($"Unknown export format '{request.TargetFormatId}'.");

        var warnings = snapshot.HasUnresolvedLines
            ? new[] { "Unresolved lines remain in the imported deck and were omitted from deterministic export rendering." }
            : Array.Empty<string>();

        var externalSnapshot = PortableDeckSectionMapper.ToExternalSnapshot(snapshot);

        return Task.FromResult(new DeckExportResultContract
        {
            TargetFormatId = profile.FormatId,
            DocumentText = profile.Render(externalSnapshot, warnings),
            Warnings = warnings,
        });
    }
}