using Bunit;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks;
using CommandSynergy.Application.Decks.Portability;
using CommandSynergy.Components.Decks;
using CommandSynergy.Domain.Cards;
using FluentAssertions;
using MudBlazor;
using MudBlazor.Services;

namespace CommandSynergy.WebUI.Tests.Components;

public sealed class DeckWorkspaceStateTests : BunitContext, IAsyncLifetime
{
    public DeckWorkspaceStateTests()
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
    public void Deck_workspace_renders_loading_state()
    {
        Render<MudPopoverProvider>();

        var cut = Render<DeckWorkspace>(parameters => parameters
            .Add(component => component.State, new DeckWorkspaceState(DeckWorkspaceStatus.Loading, null, null, Array.Empty<ValidationFindingContract>()))
            .Add(component => component.SearchQuery, string.Empty)
            .Add(component => component.ImportDocumentText, string.Empty)
            .Add(component => component.SelectedExportFormatId, "moxfield-text")
            .Add(component => component.SupportedImportFormats, [new FormatOptionView("", "Auto-detect")])
            .Add(component => component.SupportedExportFormats, [new FormatOptionView("moxfield-text", "Moxfield Text")])
            .Add(component => component.Piles, CreatePiles())
            .Add(component => component.Cards, Array.Empty<WorkspaceCardView>())
            .Add(component => component.SearchResults, Array.Empty<WorkspaceCardView>())
            .Add(component => component.ImportedDecks, Array.Empty<ImportedDeckRecord>())
            .Add(component => component.ActiveImportedDeckDiagnostics, Array.Empty<ImportDiagnostic>()));

        cut.Find("[data-testid='workspace-loading']").TextContent.Should().Contain("Loading workspace shell");
    }

    [Fact]
    public void Deck_workspace_renders_empty_state_message()
    {
        Render<MudPopoverProvider>();

        var cut = Render<DeckWorkspace>(parameters => parameters
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
            .Add(component => component.ActiveImportedDeckDiagnostics, Array.Empty<ImportDiagnostic>()));

        cut.Find("[data-testid='workspace-empty']").TextContent.Should().Contain("Pick a commander.");
    }

    [Fact]
    public void Deck_workspace_shows_commander_needed_banner_when_cards_exist_without_commander()
    {
        Render<MudPopoverProvider>();

        var cut = Render<DeckWorkspace>(parameters => parameters
            .Add(component => component.State, new DeckWorkspaceState(DeckWorkspaceStatus.Empty, "Pick a commander.", null, Array.Empty<ValidationFindingContract>()))
            .Add(component => component.SearchQuery, string.Empty)
            .Add(component => component.ImportDocumentText, string.Empty)
            .Add(component => component.SelectedExportFormatId, "moxfield-text")
            .Add(component => component.SupportedImportFormats, [new FormatOptionView("", "Auto-detect")])
            .Add(component => component.SupportedExportFormats, [new FormatOptionView("moxfield-text", "Moxfield Text")])
            .Add(component => component.Piles, CreatePiles())
            .Add(component => component.Cards, [CreateDeckCard("sol-ring")])
            .Add(component => component.SearchResults, Array.Empty<WorkspaceCardView>())
            .Add(component => component.ImportedDecks, Array.Empty<ImportedDeckRecord>())
            .Add(component => component.ActiveImportedDeckDiagnostics, Array.Empty<ImportDiagnostic>()));

        cut.Find("[data-testid='commander-needed-banner']").TextContent.Should().Contain("Pick a legal commander to resume validation, bracket, and theme analysis.");
    }

    [Fact]
    public void Deck_workspace_renders_recovery_state_with_retry()
    {
        Render<MudPopoverProvider>();

        var cut = Render<DeckWorkspace>(parameters => parameters
            .Add(component => component.State, new DeckWorkspaceState(DeckWorkspaceStatus.Recovery, "Retry the sync.", null, Array.Empty<ValidationFindingContract>()))
            .Add(component => component.SearchQuery, string.Empty)
            .Add(component => component.ImportDocumentText, string.Empty)
            .Add(component => component.SelectedExportFormatId, "moxfield-text")
            .Add(component => component.SupportedImportFormats, [new FormatOptionView("", "Auto-detect")])
            .Add(component => component.SupportedExportFormats, [new FormatOptionView("moxfield-text", "Moxfield Text")])
            .Add(component => component.Piles, CreatePiles())
            .Add(component => component.Cards, Array.Empty<WorkspaceCardView>())
            .Add(component => component.SearchResults, Array.Empty<WorkspaceCardView>())
            .Add(component => component.ImportedDecks, Array.Empty<ImportedDeckRecord>())
            .Add(component => component.ActiveImportedDeckDiagnostics, Array.Empty<ImportDiagnostic>()));

        cut.Find("[data-testid='workspace-recovery']").TextContent.Should().Contain("Retry the sync.");
        cut.Markup.Should().Contain("Retry sync");
    }

    [Fact]
    public void Deck_workspace_disables_commander_selection_for_ineligible_search_results()
    {
        Render<MudPopoverProvider>();

        var cut = Render<DeckWorkspace>(parameters => parameters
            .Add(component => component.State, new DeckWorkspaceState(DeckWorkspaceStatus.Empty, "Pick a commander.", null, Array.Empty<ValidationFindingContract>()))
            .Add(component => component.SearchQuery, "lightning")
            .Add(component => component.ImportDocumentText, string.Empty)
            .Add(component => component.SelectedExportFormatId, "moxfield-text")
            .Add(component => component.SupportedImportFormats, [new FormatOptionView("", "Auto-detect")])
            .Add(component => component.SupportedExportFormats, [new FormatOptionView("moxfield-text", "Moxfield Text")])
            .Add(component => component.Piles, CreatePiles())
            .Add(component => component.Cards, Array.Empty<WorkspaceCardView>())
            .Add(component => component.SearchResults, [CreateSearchResult("lightning-bolt", CommanderEligibilityBasis.Unknown)])
            .Add(component => component.ImportedDecks, Array.Empty<ImportedDeckRecord>())
            .Add(component => component.ActiveImportedDeckDiagnostics, Array.Empty<ImportDiagnostic>()));

        cut.Find("[data-testid='set-commander-lightning-bolt']").HasAttribute("disabled").Should().BeTrue();
    }

    private static IReadOnlyList<PileDefinitionContract> CreatePiles() =>
    [
        new PileDefinitionContract { PileId = DeckWorkspaceViewModel.CommandZonePileId, Name = "Command Zone", SortOrder = 0 },
        new PileDefinitionContract { PileId = DeckWorkspaceViewModel.MainboardPileId, Name = "Mainboard", SortOrder = 1 },
    ];

    private static WorkspaceCardView CreateSearchResult(string cardId, CommanderEligibilityBasis commanderEligibilityBasis) => new()
    {
        CardId = cardId,
        Name = "Lightning Bolt",
        ManaCost = "{R}",
        TypeLine = "Instant",
        ColorIdentity = ["R"],
        Faces = [new WorkspaceCardFaceView("Lightning Bolt", "{R}", "Instant", null, true)],
        Quantity = 1,
        CommanderEligibilityBasis = commanderEligibilityBasis,
    };

    private static WorkspaceCardView CreateDeckCard(string cardId) => new()
    {
        CardId = cardId,
        Name = "Sol Ring",
        ManaCost = "{1}",
        TypeLine = "Artifact",
        ColorIdentity = Array.Empty<string>(),
        Faces = [new WorkspaceCardFaceView("Sol Ring", "{1}", "Artifact", null, true)],
        AssignedPileId = DeckWorkspaceViewModel.MainboardPileId,
        Quantity = 1,
        CommanderEligibilityBasis = CommanderEligibilityBasis.Unknown,
    };
}