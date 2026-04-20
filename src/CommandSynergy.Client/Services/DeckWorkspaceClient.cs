using System.Net.Http.Json;
using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks.Portability;

namespace CommandSynergy.Client.Services;

/// <summary>
/// Synchronizes interactive deck workspace mutations with server-owned validation and analysis endpoints.
/// </summary>
public class DeckWorkspaceClient
{
    private readonly HttpClient httpClient;
    private readonly IDeckImportService deckImportService;
    private readonly IDeckExportService deckExportService;
    private readonly IWorkingCopyProjectionService workingCopyProjectionService;

    /// <summary>
    /// Creates a deck workspace endpoint client.
    /// </summary>
    public DeckWorkspaceClient(
        HttpClient httpClient,
        IDeckImportService deckImportService,
        IDeckExportService deckExportService,
        IWorkingCopyProjectionService workingCopyProjectionService)
    {
        this.httpClient = httpClient;
        this.deckImportService = deckImportService;
        this.deckExportService = deckExportService;
        this.workingCopyProjectionService = workingCopyProjectionService;
    }

    /// <summary>
    /// Validates the current deck snapshot against commander rules.
    /// </summary>
    public virtual async Task<DeckValidationResponseContract> ValidateAsync(DeckSnapshotContract deckSnapshot, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/decks/validate", deckSnapshot, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<DeckValidationResponseContract>(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The validation response payload was empty.");
    }

    /// <summary>
    /// Requests the latest bracket and synergy analysis for the current deck snapshot.
    /// </summary>
    public virtual async Task<DeckAnalysisResponseContract> AnalyzeAsync(DeckSnapshotContract deckSnapshot, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/decks/analyze", deckSnapshot, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<DeckAnalysisResponseContract>(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The analysis response payload was empty.");
    }

    public virtual Task<DeckImportResultContract> ImportAsync(DeckImportRequestContract request, CancellationToken cancellationToken = default) =>
        deckImportService.ImportAsync(request, cancellationToken);

    public virtual Task<DeckExportResultContract> ExportAsync(DeckExportRequestContract request, PortableDeckSnapshot snapshot, CancellationToken cancellationToken = default) =>
        deckExportService.ExportAsync(request, snapshot, cancellationToken);

    public virtual DeckSnapshotContract CreateWorkingCopy(PortableDeckSnapshot snapshot, string? deckId = null, string? deckName = null) =>
        workingCopyProjectionService.CreateWorkingCopy(snapshot, deckId, deckName);
}