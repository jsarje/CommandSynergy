using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Application.Decks;

/// <summary>
/// Creates UI-facing deck workspace states.
/// </summary>
public interface IDeckWorkspaceStateFactory
{
    /// <summary>
    /// Creates a loading state for the workspace.
    /// </summary>
    DeckWorkspaceState CreateLoading();

    /// <summary>
    /// Creates an empty state for a new deck workspace.
    /// </summary>
    DeckWorkspaceState CreateEmpty(string? message = null);

    /// <summary>
    /// Creates a ready state that reflects the latest validation response.
    /// </summary>
    DeckWorkspaceState CreateReady(DeckValidationResponseContract validation);

    /// <summary>
    /// Creates a recoverable error state for transient failures.
    /// </summary>
    DeckWorkspaceState CreateRecovery(string message);
}
