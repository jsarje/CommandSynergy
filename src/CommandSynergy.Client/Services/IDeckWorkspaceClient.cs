using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks.Portability;

namespace CommandSynergy.Client.Services;

/// <summary>
/// Synchronizes interactive deck workspace mutations with server-owned endpoints.
/// </summary>
public interface IDeckWorkspaceClient
{
    /// <summary>
    /// Validates the current deck snapshot against commander rules.
    /// </summary>
    Task<DeckValidationResponseContract> ValidateAsync(DeckSnapshotContract deckSnapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests the latest bracket and synergy analysis for the current deck snapshot.
    /// </summary>
    Task<DeckAnalysisResponseContract> AnalyzeAsync(DeckSnapshotContract deckSnapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a portable deck document.
    /// </summary>
    Task<DeckImportResultContract> ImportAsync(DeckImportRequestContract request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a portable deck snapshot.
    /// </summary>
    Task<DeckExportResultContract> ExportAsync(DeckExportRequestContract request, PortableDeckSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a working copy from a portable snapshot.
    /// </summary>
    DeckSnapshotContract CreateWorkingCopy(PortableDeckSnapshot snapshot, string? deckId = null, string? deckName = null);
}
