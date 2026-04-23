using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Application.Decks;

/// <summary>
/// Creates deck-workspace state models for loading, empty, recovery, and validation scenarios.
/// </summary>
public sealed class DeckWorkspaceStateFactory : IDeckWorkspaceStateFactory
{
    /// <summary>
    /// Creates a loading state for the workspace.
    /// </summary>
    public DeckWorkspaceState CreateLoading() => new(DeckWorkspaceStatus.Loading, null, null, Array.Empty<ValidationFindingContract>());

    /// <summary>
    /// Creates an empty state for a new deck workspace.
    /// </summary>
    public DeckWorkspaceState CreateEmpty(string? message = null) => new(DeckWorkspaceStatus.Empty, message ?? "Start by choosing a commander.", null, Array.Empty<ValidationFindingContract>());

    /// <summary>
    /// Creates a ready state that reflects the latest validation response.
    /// </summary>
    public DeckWorkspaceState CreateReady(DeckValidationResponseContract validation) => new(DeckWorkspaceStatus.Ready, null, validation, validation.Findings);

    /// <summary>
    /// Creates a recoverable error state for transient failures.
    /// </summary>
    public DeckWorkspaceState CreateRecovery(string message) => new(DeckWorkspaceStatus.Recovery, message, null, Array.Empty<ValidationFindingContract>());
}

/// <summary>
/// Represents the current UI-facing deck workspace state.
/// </summary>
public sealed record DeckWorkspaceState(
    DeckWorkspaceStatus Status,
    string? Message,
    DeckValidationResponseContract? Validation,
    IReadOnlyList<ValidationFindingContract> Findings);

/// <summary>
/// Enumerates the current deck-workspace state kinds.
/// </summary>
public enum DeckWorkspaceStatus
{
    Loading,
    Empty,
    Ready,
    Recovery,
}
