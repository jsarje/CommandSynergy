using Bunit;
using Bunit.JSInterop;
using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks;
using CommandSynergy.Application.Decks.Portability;
using CommandSynergy.Client.Services;
using CommandSynergy.Components.Decks;
using CommandSynergy.Components.Pages;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using MudBlazor.Services;

namespace CommandSynergy.WebUI.Tests.Pages;

public sealed class HomeTests : BunitContext
{
    public HomeTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public async Task Home_auto_opens_the_last_selected_saved_deck_on_first_render()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-04-20T00:00:00Z"));
        var localStorage = new StubLocalStorageService();
        var persistedLibraryState = new ImportedDeckLibraryState(
            new ImportedDeckLibraryStore(localStorage, new ImportedDeckLibrarySerializer(), timeProvider),
            timeProvider);

        await persistedLibraryState.SaveImportedDeckAsync(CreateImportedDeck("deck-1", "Mystery Stack"), setActive: true);

        var runtimeLibraryState = new ImportedDeckLibraryState(
            new ImportedDeckLibraryStore(localStorage, new ImportedDeckLibrarySerializer(), timeProvider),
            timeProvider);

        var viewModel = new DeckWorkspaceViewModel(
            new DeckWorkspaceStateFactory(),
            new StubCardSearchIndexClient(),
            new StubDeckWorkspaceClient(),
            runtimeLibraryState);

        Services.AddSingleton(viewModel);
        Render<MudPopoverProvider>();

        var cut = Render<Home>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='workspace-summary']").TextContent.Should().Contain("Mystery Stack");
            cut.Find("[data-testid='pile-card-sol-ring']").Should().NotBeNull();
        });

        viewModel.IsAutoOpeningDeck.Should().BeFalse();
    }

    private static ImportedDeckRecord CreateImportedDeck(string deckId, string name) =>
        new(
            deckId,
            name,
            "generic-plaintext",
            DateTimeOffset.Parse("2026-04-20T00:00:00Z"),
            null,
            $"Deck: {name}",
            new PortableDeckSnapshot(
                name,
                Array.Empty<string>(),
                null,
                [
                    new PortableDeckEntry("sol-ring", "1 Sol Ring", "Sol Ring", "{1}", "Artifact", Array.Empty<string>(), 0.5m, "https://cards.example/sol-ring.jpg", false, Domain.Cards.CommanderEligibilityBasis.Unknown, 1, "mainboard", false, false, ParseConfidence.Exact),
                ],
                [
                    new DeckSectionState("mainboard", "Mainboard", DeckSectionRole.Mainboard, 0, 1),
                ],
                1,
                false),
            Array.Empty<ImportDiagnostic>(),
            Array.Empty<string>(),
            new Dictionary<string, string>());

    private sealed class StubCardSearchIndexClient : CardSearchIndexClient
    {
        public StubCardSearchIndexClient()
            : base(new HttpClient())
        {
        }

        public override Task<CardSearchResponseContract> SearchAsync(
            string query,
            string? commanderCardId,
            IReadOnlyList<string>? colors = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new CardSearchResponseContract
            {
                Results = Array.Empty<CardSearchResultContract>(),
            });
        }
    }

    private sealed class StubDeckWorkspaceClient : DeckWorkspaceClient
    {
        public StubDeckWorkspaceClient()
            : base(new HttpClient(), new StubDeckImportService(), new StubDeckExportService(), new WorkingCopyProjectionService())
        {
        }

        public override Task<DeckValidationResponseContract> ValidateAsync(DeckSnapshotContract deckSnapshot, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DeckValidationResponseContract
            {
                IsValid = true,
                DeckCardCount = deckSnapshot.Entries.Sum(entry => entry.Quantity),
                Findings = Array.Empty<ValidationFindingContract>(),
            });

        public override Task<DeckAnalysisResponseContract> AnalyzeAsync(DeckSnapshotContract deckSnapshot, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DeckAnalysisResponseContract
            {
                Bracket = new BracketAssessmentContract
                {
                    Level = 1,
                    TotalWeight = 0m,
                    Summary = "Stub",
                    Factors = Array.Empty<BracketFactorContract>(),
                },
                Synergy = new SynergyAssessmentContract
                {
                    Score = 0m,
                    Summary = "Stub",
                    CommanderSpecificHits = Array.Empty<string>(),
                    StapleOverloadIndicators = Array.Empty<string>(),
                },
                PowerLevel = new PowerLevelAssessmentContract
                {
                    Score = 0m,
                    Summary = "Stub",
                },
            });
    }

    private sealed class StubDeckImportService : IDeckImportService
    {
        public Task<DeckImportResultContract> ImportAsync(DeckImportRequestContract request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubDeckExportService : IDeckExportService
    {
        public Task<DeckExportResultContract> ExportAsync(DeckExportRequestContract request, PortableDeckSnapshot snapshot, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubLocalStorageService : ILocalStorageStringStore
    {
        private readonly Dictionary<string, string?> items = new(StringComparer.OrdinalIgnoreCase);

        public ValueTask<string?> GetItemAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(items.TryGetValue(key, out var value) ? value : null);
        }

        public ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            items.Remove(key);
            return ValueTask.CompletedTask;
        }

        public ValueTask SetItemAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            items[key] = value;
            return ValueTask.CompletedTask;
        }
    }
}
