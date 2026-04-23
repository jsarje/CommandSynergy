using CommandSynergy.Components.Decks;
using Microsoft.AspNetCore.Components;

namespace CommandSynergy.Components.Pages;

/// <summary>
/// Hosts the interactive commander deck workspace.
/// </summary>
public partial class Home : ComponentBase
{
    /// <summary>
    /// Gets or sets the scoped workspace view model.
    /// </summary>
    [Inject]
    protected DeckWorkspaceViewModel ViewModel { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await ViewModel.InitializeAsync().ConfigureAwait(false);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await ViewModel.HydrateImportedDeckLibraryAsync().ConfigureAwait(false);

        // If a saved deck was active in the local library, automatically open it
        // as the workspace working copy on first page load. Set a transient flag
        // so the UI can render an explicit restore banner while the workspace
        // is being populated and analyzed.
        if (!string.IsNullOrWhiteSpace(ViewModel.ActiveImportedDeckId))
        {
            ViewModel.IsAutoOpeningDeck = true;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);

            try
            {
                await ViewModel.OpenActiveImportedDeckAsync().ConfigureAwait(false);
            }
            finally
            {
                ViewModel.IsAutoOpeningDeck = false;
            }
        }

        await InvokeAsync(StateHasChanged).ConfigureAwait(false);
    }

    private Task UpdateImportDocumentTextAsync(string value) => ViewModel.UpdateImportDocumentTextAsync(value);

    private Task UpdateImportFormatAsync(string? value) => ViewModel.UpdateImportFormatAsync(value);

    private Task ImportDeckAsync() => ViewModel.ImportDeckAsync();

    private Task UpdateExistingImportedDeckAsync() => ViewModel.UpdateExistingImportedDeckAsync();

    private Task ImportDuplicateAsNewDeckAsync() => ViewModel.ImportDuplicateAsNewDeckAsync();

    private Task SelectImportedDeckAsync(string deckId) => ViewModel.SelectImportedDeckAsync(deckId);

    private Task DeleteImportedDeckAsync(string deckId) => ViewModel.DeleteImportedDeckAsync(deckId);

    private Task OpenImportedDeckAsync() => ViewModel.OpenActiveImportedDeckAsync();

    private Task StartNewDeckAsync() => ViewModel.StartNewDeckAsync();

    private Task UpdateNewDeckNameAsync(string value) => ViewModel.UpdateNewDeckNameAsync(value);

    private Task SaveNewDeckAsync() => ViewModel.SaveNewDeckAsync();

    private Task UpdateLinkedDeckNameAsync(string value) => ViewModel.UpdateLinkedDeckNameAsync(value);

    private Task RenameLinkedDeckAsync() => ViewModel.RenameActiveDeckAsync();

    private Task UpdateExportFormatAsync(string value) => ViewModel.UpdateExportFormatAsync(value);

    private Task GenerateExportPreviewAsync() => ViewModel.GenerateExportPreviewAsync();

    private Task UpdateSearchQueryAsync(string value) => ViewModel.UpdateSearchQueryAsync(value);

    private Task SearchAsync() => ViewModel.SearchAsync();

    private Task RetryAsync() => ViewModel.RetryAsync();

    private Task SetCommanderAsync(string cardId) => ViewModel.SetCommanderAsync(cardId);

    private Task AddCardAsync(string cardId) => ViewModel.AddCardAsync(cardId);

    private Task IncrementCardQuantityAsync(string cardId) => ViewModel.IncrementCardQuantityAsync(cardId);

    private Task DecrementCardQuantityAsync(string cardId) => ViewModel.DecrementCardQuantityAsync(cardId);

    private Task MoveCardAsync(MoveCardRequest request) => ViewModel.MoveCardAsync(request);

    private Task RemoveCardAsync(string cardId) => ViewModel.RemoveCardAsync(cardId);
}
