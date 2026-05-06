using Bunit;
using Bunit.JSInterop;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Components.Decks;
using FluentAssertions;
using MudBlazor;
using MudBlazor.Services;

namespace CommandSynergy.WebUI.Tests.Components;

public sealed class DeckExportPanelTests : BunitContext, IAsyncLifetime
{
    public DeckExportPanelTests()
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
    public void Export_panel_renders_empty_state_guidance()
    {
        Render<MudPopoverProvider>();

        var cut = Render<DeckExportPanel>(parameters => parameters
            .Add(component => component.SupportedFormats, [new FormatOptionView("moxfield-text", "Moxfield Text")])
            .Add(component => component.SelectedFormatId, "moxfield-text"));

        cut.Markup.Should().Contain("Generate a preview from the current workspace.");
    }

    [Fact]
    public void Export_panel_renders_warning_and_preview_text()
    {
        Render<MudPopoverProvider>();

        var cut = Render<DeckExportPanel>(parameters => parameters
            .Add(component => component.SupportedFormats, [new FormatOptionView("moxfield-text", "Moxfield Text")])
            .Add(component => component.SelectedFormatId, "moxfield-text")
            .Add(component => component.ExportPreview, new ExportPreviewContract
            {
                TargetFormatId = "moxfield-text",
                DocumentText = "Commander:\n1 Atraxa, Praetors' Voice",
                Warnings = ["Unresolved lines remain."],
                GeneratedUtc = DateTimeOffset.UtcNow,
            }));

        cut.Markup.Should().Contain("Unresolved lines remain.");
        cut.Markup.Should().Contain("Atraxa, Praetors' Voice");
    }
}
