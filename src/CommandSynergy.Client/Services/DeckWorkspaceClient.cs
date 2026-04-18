using System.Net.Http.Json;
using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Client.Services;

/// <summary>
/// Synchronizes interactive deck workspace mutations with server-owned validation and analysis endpoints.
/// </summary>
public class DeckWorkspaceClient
{
    private readonly HttpClient httpClient;

    /// <summary>
    /// Creates a deck workspace endpoint client.
    /// </summary>
    public DeckWorkspaceClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
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
}