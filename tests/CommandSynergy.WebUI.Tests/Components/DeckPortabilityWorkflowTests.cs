using Bunit;
using Bunit.JSInterop;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks;
using CommandSynergy.Application.Decks.Portability;
using CommandSynergy.Components.Decks;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
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

        cut.FindAll("[data-testid='workspace-utility-menu']").Should().BeEmpty();

        OpenUtilityMenu(cut);

        cut.Find("[data-testid='imported-deck-library']").TextContent.Should().Contain("browser only");
    }

    [Fact]
    public void Deck_workspace_renders_import_diagnostics_as_plain_text()
    {
        var cut = RenderWorkspace(activeDiagnostics:
        [
            new ImportDiagnostic("1", DiagnosticSeverity.Warning, "xss", "<script>alert(1)</script>", 1, "line", "fix it"),
        ]);

        OpenUtilityMenu(cut);

        cut.Markup.Should().Contain("&lt;script&gt;alert(1)&lt;/script&gt;");
    }

    [Fact]
    public void Deck_workspace_closes_utility_drawer_when_backdrop_is_clicked()
    {
        var cut = RenderWorkspace();

        OpenUtilityMenu(cut);
        cut.Find("[data-testid='workspace-utility-menu']");

        cut.Find("[data-testid='workspace-utility-drawer-backdrop']").Click();

        cut.FindAll("[data-testid='workspace-utility-menu']").Should().BeEmpty();
    }

    [Fact]
    public void Deck_workspace_renders_duplicate_reimport_actions()
    {
        var cut = RenderWorkspace(hasPendingDuplicateImport: true, pendingDuplicateImportName: "Isshin Pressure", pendingDuplicateImportTargetName: "Isshin Pressure 001");

        OpenUtilityMenu(cut);

        cut.Find("[data-testid='duplicate-import-resolution']").TextContent.Should().Contain("Update the existing Isshin Pressure entry");
        cut.Find("[data-testid='duplicate-import-resolution']").TextContent.Should().Contain("Isshin Pressure 001");
    }

    [Fact]
    public void Deck_workspace_requires_confirmation_before_deleting_imported_deck()
    {
        var deletedDeckIds = new List<string>();
        var cut = RenderWorkspace(
            importedDecks:
            [
                CreateImportedDeck("deck-1", "Isshin Pressure"),
            ],
            onDeleteImportedDeck: deckId => deletedDeckIds.Add(deckId));

        OpenUtilityMenu(cut);

        cut.Find("[data-testid='delete-imported-deck-deck-1']").Click();

        deletedDeckIds.Should().BeEmpty();
        cut.Find("[data-testid='delete-imported-deck-dialog']").TextContent.Should().Contain("Delete Isshin Pressure?");

        cut.Find("[data-testid='cancel-delete-imported-deck']").Click();

        cut.Markup.Should().NotContain("delete-imported-deck-dialog");
        deletedDeckIds.Should().BeEmpty();

        cut.Find("[data-testid='delete-imported-deck-deck-1']").Click();
        cut.Find("[data-testid='confirm-delete-imported-deck']").Click();

        deletedDeckIds.Should().Equal(["deck-1"]);
    }

    private static void OpenUtilityMenu(IRenderedComponent<DeckWorkspace> cut)
    {
        cut.Find("button[aria-label='Open Deck Library']").Click();
    }

    private IRenderedComponent<DeckWorkspace> RenderWorkspace(
        IReadOnlyList<ImportDiagnostic>? activeDiagnostics = null,
        bool hasPendingDuplicateImport = false,
        string? pendingDuplicateImportName = null,
        string? pendingDuplicateImportTargetName = null,
        IReadOnlyList<ImportedDeckRecord>? importedDecks = null,
        Action<string>? onDeleteImportedDeck = null)
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
            .Add(component => component.ImportedDecks, importedDecks ?? Array.Empty<ImportedDeckRecord>())
            .Add(component => component.HasPendingDuplicateImport, hasPendingDuplicateImport)
            .Add(component => component.PendingDuplicateImportName, pendingDuplicateImportName)
            .Add(component => component.PendingDuplicateImportTargetName, pendingDuplicateImportTargetName)
            .Add(component => component.ImportedDeckDeleteRequested, EventCallback.Factory.Create<string>(this, deckId => onDeleteImportedDeck?.Invoke(deckId)))
            .Add(component => component.ActiveImportedDeckDiagnostics, activeDiagnostics ?? Array.Empty<ImportDiagnostic>()));
    }

    private static IReadOnlyList<PileDefinitionContract> CreatePiles() =>
    [
        new PileDefinitionContract { PileId = DeckWorkspaceViewModel.CommandZonePileId, Name = "Command Zone", SortOrder = 0 },
        new PileDefinitionContract { PileId = DeckWorkspaceViewModel.MainboardPileId, Name = "Mainboard", SortOrder = 1 },
    ];

    private static ImportedDeckRecord CreateImportedDeck(string deckId, string name) =>
        new(
            deckId,
            name,
            "manabox-text",
            DateTimeOffset.Parse("2026-04-20T00:00:00Z"),
            null,
            $"Deck: {name}",
            new PortableDeckSnapshot(
                name,
                ["isshin-two-heavens-as-one"],
                null,
                [
                    new PortableDeckEntry("isshin-two-heavens-as-one", "1 Isshin, Two Heavens as One", "Isshin, Two Heavens as One", "{R}{W}{B}", "Legendary Creature", ["R", "W", "B"], 0.7m, "https://cards.example/isshin.jpg", false, Domain.Cards.CommanderEligibilityBasis.LegendaryCreature, 1, "command-zone", true, false, ParseConfidence.Exact),
                ],
                [
                    new DeckSectionState("command-zone", "Command Zone", DeckSectionRole.Commander, 0, 1),
                ],
                1,
                false),
            Array.Empty<ImportDiagnostic>(),
            Array.Empty<string>(),
            new Dictionary<string, string>());
}