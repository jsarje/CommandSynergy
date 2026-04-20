using System.Net;
using System.Net.Http.Json;
using System.Text;
using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks.Portability;
using CommandSynergy.Client.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace CommandSynergy.WebUI.Tests.Security;

public sealed class DeckWorkspaceSecurityTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory factory;

    public DeckWorkspaceSecurityTests(TestApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Get_search_endpoint_rejects_blank_queries()
    {
        using var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICardSearchService>();
                services.AddSingleton<ICardSearchService>(new StubCardSearchService());
            });
        }).CreateClient();

        var response = await client.GetAsync("/api/cards/search?q=%20%20%20");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_validate_endpoint_rejects_pathological_entry_counts()
    {
        using var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDeckValidationService>();
                services.AddSingleton<IDeckValidationService>(new StubDeckValidationService());
            });
        }).CreateClient();

        var payload = new DeckSnapshotContract
        {
            CommanderCardId = "commander",
            Entries = Enumerable.Range(0, 251)
                .Select(index => new DeckEntryContract
                {
                    CardId = $"card-{index}",
                    Quantity = 1,
                    IsCommander = index == 0,
                })
                .ToArray(),
        };

        var response = await client.PostAsJsonAsync("/api/decks/validate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Imported_deck_library_store_recovers_from_corrupted_payloads()
    {
        var localStorage = new StubLocalStorageService
        {
            [DeckPortabilityContract.StorageKey] = "{not-json",
        };

        var sut = new ImportedDeckLibraryStore(localStorage, new ImportedDeckLibrarySerializer(), new FakeTimeProvider());

        var result = await sut.LoadAsync();

        result.RecoveredFromInvalidState.Should().BeTrue();
        result.Document.Decks.Should().BeEmpty();
        localStorage.ContainsKey(DeckPortabilityContract.StorageKey).Should().BeFalse();
    }

    [Fact]
    public async Task Imported_deck_library_store_rejects_oversized_payloads()
    {
        var localStorage = new StubLocalStorageService();
        var sut = new ImportedDeckLibraryStore(localStorage, new ImportedDeckLibrarySerializer(), new FakeTimeProvider());
        var largeDocumentText = string.Concat(Enumerable.Range(0, ImportedDeckLibraryStore.MaxPersistedPayloadLength / 24)
            .Select(index => Convert.ToHexString(Encoding.UTF8.GetBytes($"deck-line-{index:D6}-{Guid.NewGuid():N}"))));

        Func<Task> action = async () => await sut.SaveAsync(new ImportedDeckLibraryDocument(
            DeckPortabilityContract.CurrentSchemaVersion,
            null,
            [new ImportedDeckRecord("deck-1", "Big", "generic-plaintext", DateTimeOffset.UtcNow, null, largeDocumentText, new PortableDeckSnapshot("Big", Array.Empty<string>(), null, Array.Empty<PortableDeckEntry>(), Array.Empty<DeckSectionState>(), 0, false), Array.Empty<ImportDiagnostic>(), Array.Empty<string>(), new Dictionary<string, string>())],
            null));

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Imported_deck_library_store_round_trips_compressed_payloads()
    {
        var localStorage = new StubLocalStorageService();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-04-20T00:00:00Z"));
        var sut = new ImportedDeckLibraryStore(localStorage, new ImportedDeckLibrarySerializer(), timeProvider);
        var document = new ImportedDeckLibraryDocument(
            DeckPortabilityContract.CurrentSchemaVersion,
            "deck-1",
            [new ImportedDeckRecord(
                "deck-1",
                "Isshin Pressure",
                "generic-plaintext",
                timeProvider.GetUtcNow(),
                null,
                string.Join(Environment.NewLine, Enumerable.Range(1, 120).Select(index => $"1 Card {index:D3}")),
                new PortableDeckSnapshot("Isshin Pressure", Array.Empty<string>(), null, Array.Empty<PortableDeckEntry>(), Array.Empty<DeckSectionState>(), 120, false),
                Array.Empty<ImportDiagnostic>(),
                Array.Empty<string>(),
                new Dictionary<string, string>())],
            null);

        await sut.SaveAsync(document);
        var result = await sut.LoadAsync();

        localStorage[DeckPortabilityContract.StorageKey].Should().StartWith("gz:");
        result.Document.ActiveDeckId.Should().Be("deck-1");
        result.Document.Decks.Should().ContainSingle();
        result.Document.Decks[0].OriginalDocumentText.Should().Contain("1 Card 120");
    }

    private sealed class StubCardSearchService : ICardSearchService
    {
        public Task<CardSearchResponseContract> SearchAsync(CardSearchQueryContract request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CardSearchResponseContract
            {
                SnapshotVersion = "test",
                Results = Array.Empty<CardSearchResultContract>(),
            });
    }

    private sealed class StubDeckValidationService : IDeckValidationService
    {
        public Task<DeckValidationResponseContract> ValidateAsync(DeckSnapshotContract request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DeckValidationResponseContract
            {
                IsValid = true,
                DeckCardCount = request.Entries.Sum(entry => entry.Quantity),
                Findings = Array.Empty<ValidationFindingContract>(),
            });
    }

        private sealed class StubLocalStorageService : ILocalStorageStringStore
    {
        private readonly Dictionary<string, string?> items = new(StringComparer.OrdinalIgnoreCase);

        public string? this[string key]
        {
            get => items[key];
            set => items[key] = value;
        }

        public bool ContainsKey(string key) => items.ContainsKey(key);

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