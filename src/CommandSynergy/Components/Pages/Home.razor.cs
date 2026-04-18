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

    private Task UpdateSearchQueryAsync(string value) => ViewModel.UpdateSearchQueryAsync(value);

    private Task SearchAsync() => ViewModel.SearchAsync();

    private Task RetryAsync() => ViewModel.RetryAsync();

    private Task SetCommanderAsync(string cardId) => ViewModel.SetCommanderAsync(cardId);

    private Task AddCardAsync(string cardId) => ViewModel.AddCardAsync(cardId);

    private Task MoveCardAsync(MoveCardRequest request) => ViewModel.MoveCardAsync(request);

    private Task RemoveCardAsync(string cardId) => ViewModel.RemoveCardAsync(cardId);
}