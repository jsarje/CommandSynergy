using Bunit;
using CommandSynergy.Components.Decks;
using FluentAssertions;
using MudBlazor.Services;

namespace CommandSynergy.WebUI.Tests.Components;

public sealed class ThemeAnalysisPanelTests : BunitContext
{
    public ThemeAnalysisPanelTests()
    {
        Services.AddMudServices();
    }

    [Fact]
    public void Theme_analysis_panel_renders_loading_state()
    {
        var cut = Render<ThemeAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, true)
            .Add(component => component.HasError, false)
            .Add(component => component.Analysis, null));

        cut.Find("[data-testid='theme-analysis-loading']").Should().NotBeNull();
    }

    [Fact]
    public void Theme_analysis_panel_renders_insufficient_state()
    {
        var cut = Render<ThemeAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.HasError, false)
            .Add(component => component.Analysis, ThemeAnalysisTestData.CreateReadyAnalysis(isInsufficient: true)));

        cut.Find("[data-testid='theme-analysis-insufficient']").TextContent.Should().Contain("Add more cards");
    }

    [Fact]
    public void Theme_analysis_panel_renders_ready_state_with_off_theme_cards()
    {
        var cut = Render<ThemeAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.HasError, false)
            .Add(component => component.Analysis, ThemeAnalysisTestData.CreateReadyAnalysis()));

        cut.Find("[data-testid='theme-analysis-ready']").Should().NotBeNull();
        cut.Find("[data-testid='theme-analysis-pill']").TextContent.Should().Contain("Strong alignment");
        cut.Find("[data-testid='off-theme-cards']").TextContent.Should().Contain("Staple Card");
        cut.Find("[data-testid='theme-preview-tokens']").TextContent.Should().Contain("Token Card");
        cut.Find("[data-testid='off-theme-cards']").TextContent.Should().Contain("No strong theme signal was detected for this card.");
    }

    [Fact]
    public void Theme_analysis_panel_preserves_stale_results_while_refreshing()
    {
        var cut = Render<ThemeAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, true)
            .Add(component => component.HasError, false)
            .Add(component => component.Analysis, ThemeAnalysisTestData.CreateReadyAnalysis()));

        cut.Find("[data-testid='theme-analysis-ready']").HasAttribute("aria-busy").Should().BeTrue();
        cut.Find("[data-testid='theme-analysis-refreshing']").TextContent.Should().Contain("Showing the previous result");
    }

    [Fact]
    public void Theme_analysis_panel_expands_each_theme_to_show_all_contributors()
    {
        var cut = Render<ThemeAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.HasError, false)
            .Add(component => component.Analysis, ThemeAnalysisTestData.CreateReadyAnalysis()));

        var tokenToggle = cut.Find("[data-testid='theme-toggle-tokens']");
        tokenToggle.Click();
        tokenToggle.HasAttribute("aria-expanded").Should().BeTrue();
        cut.Find("[data-testid='theme-details-tokens']").HasAttribute("hidden").Should().BeFalse();
        cut.Find("[data-testid='theme-contributors-tokens']").TextContent.Should().Contain("Dual Theme Card");

        var aristocratsToggle = cut.Find("[data-testid='theme-toggle-aristocrats']");
        aristocratsToggle.Click();
        cut.Find("[data-testid='theme-contributors-aristocrats']").TextContent.Should().Contain("Dual Theme Card");
    }

    [Fact]
    public void Theme_analysis_panel_exposes_accessible_roles_and_metadata_indicators()
    {
        var cut = Render<ThemeAnalysisPanel>(parameters => parameters
            .Add(component => component.IsLoading, false)
            .Add(component => component.HasError, false)
            .Add(component => component.Analysis, ThemeAnalysisTestData.CreateReadyAnalysis()));

        cut.Find("[data-testid='theme-analysis-ready']").GetAttribute("role").Should().Be("region");
        cut.Find("[data-testid='off-theme-cards']").GetAttribute("aria-label").Should().Be("Off-theme cards");
        cut.Markup.Should().Contain("Metadata unavailable");
    }
}