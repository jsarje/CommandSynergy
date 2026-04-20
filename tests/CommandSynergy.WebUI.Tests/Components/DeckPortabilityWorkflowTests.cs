using Bunit;
using Bunit.JSInterop;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks;
using CommandSynergy.Application.Decks.Portability;
using CommandSynergy.Components.Decks;
using FluentAssertions;
using MudBlazor;
using MudBlazor.Services;

namespace CommandSynergy.WebUI.Tests.Components;

public sealed class DeckPortabilityWorkflowTests : BunitContext, IAsyncLifetime
{
    public DeckPortabilityWorkflowTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }

    [Fact]
    public void Deck_workspace_renders_import_library_empty_state()
    {
        var cut = RenderWorkspace();

        cut.Find("[data-testid='imported-deck-library']").TextContent.Should().Contain("browser only");
    }

    [Fact]
    public void Deck_workspace_renders_import_diagnostics_as_plain_text()
    {
        var cut = RenderWorkspace(activeDiagnostics:
        [
            new ImportDiagnostic("1", DiagnosticSeverity.Warning, "xss", "<script>alert(1)</script>", 1, "line", "fix it"),
        ]);

        cut.Markup.Should().Contain("&lt;script&gt;alert(1)&lt;/script&gt;");
    }

    private IRenderedComponent<DeckWorkspace> RenderWorkspace(IReadOnlyList<ImportDiagnostic>? activeDiagnostics = null)
    {
        Render<MudPopoverProvider>();

        return Render<DeckWorkspace>(parameters => parameters
            .Add(component => component.State, new DeckWorkspaceState(DeckWorkspaceStatus.Empty, "Pick a commander.", null, Array.Empty<ValidationFindingContract>()))
            .Add(component => component.SearchQuery, string.Empty)
            .Add(component => component.ImportDocumentText, string.Empty)
            .Add(component => component.SelectedExportFormatId, "moxfield-text")
            .Add(component => component.SupportedImportFormats, [new FormatOptionView("", "Auto-detect")])
            .Add(component => component.SupportedExportFormats, [new FormatOptionView("moxfield-text", "Moxfield Text")])
            .Add(component => component.Piles, CreatePiles())
            .Add(component => component.Cards, Array.Empty<WorkspaceCardView>())
            .Add(component => component.SearchResults, Array.Empty<WorkspaceCardView>())
            .Add(component => component.ImportedDecks, Array.Empty<ImportedDeckRecord>())
            .Add(component => component.ActiveImportedDeckDiagnostics, activeDiagnostics ?? Array.Empty<ImportDiagnostic>()));
    }

    private static IReadOnlyList<PileDefinitionContract> CreatePiles() =>
    [
        new PileDefinitionContract { PileId = DeckWorkspaceViewModel.CommandZonePileId, Name = "Command Zone", SortOrder = 0 },
        new PileDefinitionContract { PileId = DeckWorkspaceViewModel.MainboardPileId, Name = "Mainboard", SortOrder = 1 },
    ];
}