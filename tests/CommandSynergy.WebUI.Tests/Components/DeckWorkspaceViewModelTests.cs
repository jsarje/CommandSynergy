using System.Text.Json;
using Blazored.LocalStorage;
using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks;
using CommandSynergy.Application.Decks.Portability;
using CommandSynergy.Client.Services;
using CommandSynergy.Components.Decks;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace CommandSynergy.WebUI.Tests.Components;

public sealed class DeckWorkspaceViewModelTests
{
    [Fact]
    public async Task OpenActiveImportedDeckAsync_restores_imported_card_metadata_into_workspace_cards()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-04-20T00:00:00Z"));
        var libraryState = new ImportedDeckLibraryState(
            new ImportedDeckLibraryStore(new StubLocalStorageService(), new ImportedDeckLibrarySerializer(), timeProvider),
            timeProvider);

        var importedDeck = new ImportedDeckRecord(
            "deck-1",
            "Isshin Pressure",
            "manabox-text",
            timeProvider.GetUtcNow(),
            null,
            "Deck: Isshin Pressure",
            new PortableDeckSnapshot(
                "Isshin Pressure",
                ["isshin-two-heavens-as-one"],
                null,
                [
                    new PortableDeckEntry("isshin-two-heavens-as-one", "1 Isshin, Two Heavens as One", "Isshin, Two Heavens as One", "{R}{W}{B}", "Legendary Creature", ["R", "W", "B"], 0.7m, "https://cards.example/isshin.jpg", false, Domain.Cards.CommanderEligibilityBasis.LegendaryCreature, 1, "command-zone", true, false, ParseConfidence.Exact),
                    new PortableDeckEntry("wear-tear", "1 Wear // Tear", "Wear // Tear", "{1}{R} // {W}", "Instant", ["R", "W"], 0.2m, "https://cards.example/wear-tear.jpg", true, Domain.Cards.CommanderEligibilityBasis.Unknown, 1, "maybeboard", false, false, ParseConfidence.Exact),
                ],
                [
                    new DeckSectionState("command-zone", "Command Zone", DeckSectionRole.Commander, 0, 1),
                    new DeckSectionState("maybeboard", "Maybeboard", DeckSectionRole.Maybeboard, 1, 1),
                ],
                2,
                false),
            Array.Empty<ImportDiagnostic>(),
            Array.Empty<string>(),
            new Dictionary<string, string>());

        await libraryState.SaveImportedDeckAsync(importedDeck, setActive: true);

        using var sut = new DeckWorkspaceViewModel(
            new DeckWorkspaceStateFactory(),
            new StubCardSearchIndexClient(),
            new StubDeckWorkspaceClient(),
            libraryState);

        await sut.InitializeAsync();
        await sut.OpenActiveImportedDeckAsync();

        var importedCard = sut.Cards.Should().ContainSingle(card => card.CardId == "wear-tear").Subject;
        importedCard.Name.Should().Be("Wear // Tear");
        importedCard.ManaCost.Should().Be("{1}{R} // {W}");
        importedCard.TypeLine.Should().Be("Instant");
        importedCard.ColorIdentity.Should().BeEquivalentTo(["R", "W"]);
        importedCard.ImageUri.Should().Be("https://cards.example/wear-tear.jpg");
        importedCard.HasMultipleFaces.Should().BeTrue();
        importedCard.Faces.Should().HaveCount(2);
    }

    private sealed class StubCardSearchIndexClient : CardSearchIndexClient
    {
        public StubCardSearchIndexClient()
            : base(new HttpClient())
        {
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
                    TotalWeight = 0,
                    Summary = "Stub",
                },
                Synergy = new SynergyAssessmentContract
                {
                    Score = 0,
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

    private sealed class StubLocalStorageService : ILocalStorageService
    {
        private readonly Dictionary<string, string?> items = new(StringComparer.OrdinalIgnoreCase);

#pragma warning disable CS0067
        public event EventHandler<ChangingEventArgs>? Changing;

        public event EventHandler<ChangedEventArgs>? Changed;
#pragma warning restore CS0067

        public ValueTask ClearAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            items.Clear();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> ContainKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(items.ContainsKey(key));
        }

        public ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!items.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            {
                return ValueTask.FromResult<T?>(default);
            }

            return ValueTask.FromResult(JsonSerializer.Deserialize<T>(value));
        }

        public ValueTask<string?> GetItemAsStringAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(items.TryGetValue(key, out var value) ? value : null);
        }

        public ValueTask<string?> KeyAsync(int index, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult<string?>(items.Keys.ElementAt(index));
        }

        public ValueTask<IEnumerable<string>> KeysAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult<IEnumerable<string>>(items.Keys.ToArray());
        }

        public ValueTask<int> LengthAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(items.Count);
        }

        public ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            items.Remove(key);
            return ValueTask.CompletedTask;
        }

        public ValueTask RemoveItemsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var key in keys)
            {
                items.Remove(key);
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            items[key] = JsonSerializer.Serialize(data);
            return ValueTask.CompletedTask;
        }

        public ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            items[key] = data;
            return ValueTask.CompletedTask;
        }
    }
}