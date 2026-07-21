using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommandSynergy.WebUI.Tests.Endpoints;

public sealed class DeckAnalysisIntegrationTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory factory;

    public DeckAnalysisIntegrationTests(TestApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Post_analyze_endpoint_returns_focused_fixture_payload_within_budget()
    {
        var fixture = ThemeAnalysisTestData.LoadFixture("focused-deck.json");
        using var client = CreateClient(fixture);

        var started = Stopwatch.GetTimestamp();
        var response = await client.PostAsJsonAsync("/api/decks/analyze", fixture.Snapshot);
        var elapsed = Stopwatch.GetElapsedTime(started);
        var payload = await response.Content.ReadFromJsonAsync<DeckAnalysisResponseContract>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.ThemeAnalysis!.PrimaryThemes.Should().Contain(theme => theme.Name == "Tokens");
        payload.ThemeAnalysis.RankedThemes.Take(3).Should().NotBeEmpty();
        payload.Synergy.FinalScore.Should().BeGreaterThanOrEqualTo(60m);
        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Post_analyze_endpoint_returns_low_alignment_for_commander_misaligned_fixture()
    {
        var fixture = ThemeAnalysisTestData.LoadFixture("commander-misaligned-deck.json");
        using var client = CreateClient(fixture);

        var response = await client.PostAsJsonAsync("/api/decks/analyze", fixture.Snapshot);
        var payload = await response.Content.ReadFromJsonAsync<DeckAnalysisResponseContract>();

        payload.Should().NotBeNull();
        payload!.ThemeAnalysis!.CommanderAlignment.Level.Should().Be("Low");
        payload.ThemeAnalysis.CommanderAlignment.CommanderTopTheme.Should().Be("Tokens");
    }

    [Fact]
    public async Task Post_analyze_endpoint_surfaces_off_theme_reason_in_initial_payload()
    {
        var fixture = ThemeAnalysisTestData.LoadFixture("unfocused-deck.json");
        using var client = CreateClient(fixture);

        var response = await client.PostAsJsonAsync("/api/decks/analyze", fixture.Snapshot);
        var payload = await response.Content.ReadFromJsonAsync<DeckAnalysisResponseContract>();

        payload.Should().NotBeNull();
        payload!.ThemeAnalysis!.OffThemeCards.Should().Contain(card =>
            card.CardName.StartsWith("Misc Card", StringComparison.OrdinalIgnoreCase)
            && card.Reason.Contains("No strong theme signal", StringComparison.OrdinalIgnoreCase));
    }

    private HttpClient CreateClient(ThemeEndpointFixture fixture) => factory.WithWebHostBuilder(builder =>
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICardCatalogGateway>();
            services.RemoveAll<IEdhrecClient>();
            services.AddSingleton<ICardCatalogGateway>(new StubCardCatalogGateway(fixture.Profiles));
            services.AddSingleton<IEdhrecClient>(new StubEdhrecClient(fixture.EdhrecInsights));
        });
    }).CreateClient();

    private sealed class StubCardCatalogGateway : ICardCatalogGateway
    {
        private readonly IReadOnlyDictionary<string, Domain.Cards.CardProfile> profiles;

        public StubCardCatalogGateway(IReadOnlyDictionary<string, Domain.Cards.CardProfile> profiles)
        {
            this.profiles = profiles;
        }

        public Task<IReadOnlyDictionary<string, Domain.Cards.CardProfile>> GetCardProfilesAsync(IEnumerable<string> cardIds, CancellationToken cancellationToken = default) =>
            Task.FromResult(profiles);

        public Task<IReadOnlyList<Domain.Cards.CardProfile>> GetCommanderLegalCardProfilesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<Domain.Cards.CardProfile>)profiles.Values.ToArray());

        public Task<IReadOnlyList<CardSearchResultContract>> SearchAsync(CardSearchQueryContract request, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<CardSearchResultContract>)Array.Empty<CardSearchResultContract>());

        public Task<string?> GetSnapshotVersionAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>("integration-fixture");
    }

    private sealed class StubEdhrecClient : IEdhrecClient
    {
        private readonly CommanderThemeInsights insights;

        public StubEdhrecClient(CommanderThemeInsights insights)
        {
            this.insights = insights;
        }

        public Task<CommanderThemeInsights> GetCommanderThemeInsightsAsync(Domain.Cards.CardProfile commanderProfile, CancellationToken cancellationToken = default) =>
            Task.FromResult(insights);
    }
}
