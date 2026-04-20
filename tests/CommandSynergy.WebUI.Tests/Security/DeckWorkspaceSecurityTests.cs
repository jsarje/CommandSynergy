using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;
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

        Func<Task> action = async () => await sut.SaveAsync(new ImportedDeckLibraryDocument(
            DeckPortabilityContract.CurrentSchemaVersion,
            null,
            [new ImportedDeckRecord("deck-1", "Big", "generic-plaintext", DateTimeOffset.UtcNow, null, new string('a', ImportedDeckLibraryStore.MaxPersistedPayloadLength), new PortableDeckSnapshot("Big", Array.Empty<string>(), null, Array.Empty<PortableDeckEntry>(), Array.Empty<DeckSectionState>(), 0, false), Array.Empty<ImportDiagnostic>(), Array.Empty<string>(), new Dictionary<string, string>())],
            null));

        await action.Should().ThrowAsync<InvalidOperationException>();
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

    private sealed class StubLocalStorageService : ILocalStorageService
    {
        private readonly Dictionary<string, string?> items = new(StringComparer.OrdinalIgnoreCase);

#pragma warning disable CS0067
        public event EventHandler<ChangingEventArgs>? Changing;

        public event EventHandler<ChangedEventArgs>? Changed;
#pragma warning restore CS0067

        public string? this[string key]
        {
            get => items[key];
            set => items[key] = value;
        }

        public bool ContainsKey(string key) => items.ContainsKey(key);

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