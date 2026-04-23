using Bunit;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks.Portability;
using CommandSynergy.Components.Decks;
using FluentAssertions;
using MudBlazor.Services;

namespace CommandSynergy.WebUI.Tests.Components;

public sealed class DeckUtilityPanelTests : BunitContext
{
    public DeckUtilityPanelTests()
    {
        Services.AddMudServices();
    }

    [Fact]
    public void Deck_utility_panel_renders_linked_deck_controls_and_status()
    {
        var cut = RenderPanel(parameters => parameters
            .Add(component => component.ImportedDecks, [CreateImportedDeck("deck-1", "Isshin Pressure")])
            .Add(component => component.ActiveImportedDeckId, "deck-1")
            .Add(component => component.IsWorkspaceLinkedToSavedDeck, true)
            .Add(component => component.ActiveWorkspaceDeckName, "Isshin Pressure")
            .Add(component => component.LinkedDeckName, "Isshin Pressure")
            .Add(component => component.CanRenameLinkedDeck, true)
            .Add(component => component.LinkedDeckStatusMessage, "Deck renamed."));

        cut.Find("[data-testid='workspace-link-status']").TextContent.Should().Contain("Editing Isshin Pressure");
        cut.Find("[data-testid='linked-deck-rename-controls']").Should().NotBeNull();
        cut.Find("[data-testid='linked-deck-rename-status']").TextContent.Should().Contain("Deck renamed.");
        cut.FindAll("[data-testid='new-deck-save-controls']").Should().BeEmpty();
    }

    [Fact]
    public void Deck_utility_panel_blocks_close_requests_while_importing()
    {
        var closeRequestCount = 0;

        var cut = RenderPanel(parameters => parameters
            .Add(component => component.IsImporting, true)
            .Add(component => component.CloseRequested, () => closeRequestCount++));

        cut.Find("[data-testid='workspace-utility-drawer-backdrop']").Click();

        closeRequestCount.Should().Be(0);
        cut.Find("button[aria-label='Close menu']").HasAttribute("disabled").Should().BeTrue();
        cut.Markup.Should().Contain("Importing deck…");
    }

    private IRenderedComponent<DeckUtilityPanel> RenderPanel(Action<ComponentParameterCollectionBuilder<DeckUtilityPanel>>? configure = null)
    {
        return Render<DeckUtilityPanel>(parameters =>
        {
            parameters
                .Add(component => component.ImportedDecks, Array.Empty<ImportedDeckRecord>())
                .Add(component => component.LinkedDeckName, string.Empty)
                .Add(component => component.NewDeckName, string.Empty)
                .Add(component => component.ActiveImportedDeckDiagnostics, Array.Empty<ImportDiagnostic>())
                .Add(component => component.SupportedImportFormats, [new FormatOptionView("moxfield-text", "Moxfield Text")])
                .Add(component => component.ImportDocumentText, string.Empty)
                .Add(component => component.SupportedExportFormats, [new FormatOptionView("moxfield-text", "Moxfield Text")])
                .Add(component => component.SelectedExportFormatId, "moxfield-text");

            configure?.Invoke(parameters);
        });
    }

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
