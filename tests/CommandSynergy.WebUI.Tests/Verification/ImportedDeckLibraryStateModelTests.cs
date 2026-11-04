using CommandSynergy.Application.Decks.Portability;
using CommandSynergy.Client.Services;
using Microsoft.Extensions.Time.Testing;

namespace CommandSynergy.WebUI.Tests.Verification;

public sealed class ImportedDeckLibraryStateModelTests
{
    private static readonly DateTimeOffset FixedTime = DateTimeOffset.Parse("2026-04-20T00:00:00Z");
    private const int MaxCommands = 5;

    private static readonly LibraryCommand[] Commands =
    [
        new("save deck-a and activate", LibraryOperation.Save, "deck-a", true),
        new("save deck-a without activation", LibraryOperation.Save, "deck-a", false),
        new("save DECK-A and activate", LibraryOperation.Save, "DECK-A", true),
        new("save DECK-A without activation", LibraryOperation.Save, "DECK-A", false),
        new("save deck-b and activate", LibraryOperation.Save, "deck-b", true),
        new("save deck-b without activation", LibraryOperation.Save, "deck-b", false),
        new("activate deck-a", LibraryOperation.SetActive, "deck-a"),
        new("activate deck-b", LibraryOperation.SetActive, "deck-b"),
        new("clear active selection", LibraryOperation.SetActive),
        new("remove deck-a", LibraryOperation.Remove, "deck-a"),
        new("remove deck-b", LibraryOperation.Remove, "deck-b"),
        new("clear library", LibraryOperation.Clear),
    ];

    [Fact]
    public async Task Library_invariants_hold_for_all_reachable_states_up_to_five_commands()
    {
        var initialState = await CreateStateAsync(ImportedDeckLibraryDocument.Empty);
        var initialPath = Array.Empty<string>();
        AssertInvariants(initialState.Library, initialPath);

        var pending = new Queue<ExplorationNode>();
        var visited = new HashSet<string>(StringComparer.Ordinal)
        {
            StateKey(initialState.Library),
        };
        pending.Enqueue(new ExplorationNode(initialState.Library, initialPath));

        while (pending.TryDequeue(out var current))
        {
            if (current.Path.Count == MaxCommands)
            {
                continue;
            }

            foreach (var command in Commands)
            {
                if (!IsApplicable(command, current.Library))
                {
                    continue;
                }

                var nextState = await CreateStateAsync(current.Library);
                await ExecuteAsync(nextState, command);

                var nextPath = current.Path.Append(command.Label).ToArray();
                AssertInvariants(nextState.Library, nextPath);

                if (visited.Add(StateKey(nextState.Library)))
                {
                    pending.Enqueue(new ExplorationNode(nextState.Library, nextPath));
                }
            }
        }
    }

    private static async Task<ImportedDeckLibraryState> CreateStateAsync(ImportedDeckLibraryDocument document)
    {
        var state = new ImportedDeckLibraryState(
            new MemoryImportedDeckLibraryStore(document),
            new FakeTimeProvider(FixedTime));

        await state.HydrateAsync();
        return state;
    }

    private static bool IsApplicable(LibraryCommand command, ImportedDeckLibraryDocument library) =>
        command.Operation != LibraryOperation.SetActive
        || command.DeckId is null
        || library.Decks.Any(deck => string.Equals(deck.ImportedDeckId, command.DeckId, StringComparison.OrdinalIgnoreCase));

    private static async Task ExecuteAsync(ImportedDeckLibraryState state, LibraryCommand command)
    {
        switch (command.Operation)
        {
            case LibraryOperation.Save when command.DeckId is { } deckId:
                await state.SaveImportedDeckAsync(CreateDeck(deckId), command.SetActive);
                break;
            case LibraryOperation.SetActive:
                await state.SetActiveDeckAsync(command.DeckId);
                break;
            case LibraryOperation.Remove when command.DeckId is { } deckId:
                await state.RemoveImportedDeckAsync(deckId);
                break;
            case LibraryOperation.Clear:
                await state.ClearAsync();
                break;
            default:
                throw new InvalidOperationException($"Command '{command.Label}' is incomplete.");
        }
    }

    private static ImportedDeckRecord CreateDeck(string deckId)
    {
        var importedAt = string.Equals(deckId, "deck-a", StringComparison.OrdinalIgnoreCase)
            ? FixedTime
            : FixedTime.AddMinutes(1);

        return new ImportedDeckRecord(
            deckId,
            deckId,
            "generic-plaintext",
            importedAt,
            null,
            $"Deck: {deckId}",
            new PortableDeckSnapshot(
                deckId,
                Array.Empty<string>(),
                null,
                Array.Empty<PortableDeckEntry>(),
                Array.Empty<DeckSectionState>(),
                0,
                false),
            Array.Empty<ImportDiagnostic>(),
            Array.Empty<string>(),
            new Dictionary<string, string>());
    }

    private static void AssertInvariants(ImportedDeckLibraryDocument library, IReadOnlyList<string> path)
    {
        var trace = path.Count == 0 ? "initial state" : string.Join(" -> ", path);
        var deckIds = library.Decks.Select(deck => deck.ImportedDeckId).ToArray();
        var uniqueDeckCount = deckIds.Distinct(StringComparer.OrdinalIgnoreCase).Count();

        Assert.True(
            deckIds.Length == uniqueDeckCount,
            $"Duplicate deck IDs after [{trace}]: {string.Join(", ", deckIds)}.");

        Assert.True(
            library.ActiveDeckId is null
            || deckIds.Contains(library.ActiveDeckId, StringComparer.OrdinalIgnoreCase),
            $"Active deck '{library.ActiveDeckId}' is missing after [{trace}]. Saved decks: {string.Join(", ", deckIds)}.");
    }

    private static string StateKey(ImportedDeckLibraryDocument library) =>
        $"{library.ActiveDeckId?.ToUpperInvariant() ?? "-"}|{string.Join(",", library.Decks.Select(deck => deck.ImportedDeckId.ToUpperInvariant()))}";

    private sealed record ExplorationNode(ImportedDeckLibraryDocument Library, IReadOnlyList<string> Path);

    private sealed record LibraryCommand(
        string Label,
        LibraryOperation Operation,
        string? DeckId = null,
        bool SetActive = false);

    private enum LibraryOperation
    {
        Save,
        SetActive,
        Remove,
        Clear,
    }

    private sealed class MemoryImportedDeckLibraryStore(ImportedDeckLibraryDocument document) : IImportedDeckLibraryStore
    {
        public ImportedDeckLibraryDocument Document { get; private set; } = document;

        public Task<ImportedDeckLibraryLoadResult> LoadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new ImportedDeckLibraryLoadResult(Document, false, null));
        }

        public Task SaveAsync(ImportedDeckLibraryDocument document, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Document = document;
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Document = ImportedDeckLibraryDocument.Empty;
            return Task.CompletedTask;
        }
    }
}
