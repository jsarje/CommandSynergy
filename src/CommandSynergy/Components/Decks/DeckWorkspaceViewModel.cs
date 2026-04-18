using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks;
using CommandSynergy.Client.Services;

namespace CommandSynergy.Components.Decks;

/// <summary>
/// Owns the interactive workspace state and keeps it synchronized with server validation and analysis.
/// </summary>
public sealed class DeckWorkspaceViewModel : IDisposable
{
    /// <summary>
    /// The fixed pile used for the selected commander.
    /// </summary>
    public const string CommandZonePileId = "command-zone";

    /// <summary>
    /// The default pile used for cards without an explicit assignment.
    /// </summary>
    public const string MainboardPileId = "mainboard";

    private readonly DeckWorkspaceStateFactory stateFactory;
    private readonly CardSearchIndexClient cardSearchIndexClient;
    private readonly DeckWorkspaceClient deckWorkspaceClient;
    private readonly List<PileDefinitionContract> piles = [];
    private readonly List<DeckEntryState> entries = [];
    private readonly Dictionary<string, WorkspaceCardView> knownCards = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource? refreshCancellationTokenSource;

    /// <summary>
    /// Creates a workspace state coordinator.
    /// </summary>
    public DeckWorkspaceViewModel(
        DeckWorkspaceStateFactory stateFactory,
        CardSearchIndexClient cardSearchIndexClient,
        DeckWorkspaceClient deckWorkspaceClient)
    {
        this.stateFactory = stateFactory;
        this.cardSearchIndexClient = cardSearchIndexClient;
        this.deckWorkspaceClient = deckWorkspaceClient;
        State = stateFactory.CreateLoading();
        Piles = Array.Empty<PileDefinitionContract>();
        Cards = Array.Empty<WorkspaceCardView>();
        SearchResults = Array.Empty<WorkspaceCardView>();
    }

    /// <summary>
    /// Gets the current workspace lifecycle state.
    /// </summary>
    public DeckWorkspaceState State { get; private set; }

    /// <summary>
    /// Gets the latest server-owned analysis result.
    /// </summary>
    public DeckAnalysisResponseContract? Analysis { get; private set; }

    /// <summary>
    /// Gets the current search query text.
    /// </summary>
    public string SearchQuery { get; private set; } = string.Empty;

    /// <summary>
    /// Gets whether the workspace is performing a server-backed operation.
    /// </summary>
    public bool IsBusy { get; private set; }

    /// <summary>
    /// Gets the available workspace piles.
    /// </summary>
    public IReadOnlyList<PileDefinitionContract> Piles { get; private set; }

    /// <summary>
    /// Gets the cards currently present in the workspace.
    /// </summary>
    public IReadOnlyList<WorkspaceCardView> Cards { get; private set; }

    /// <summary>
    /// Gets the current search results.
    /// </summary>
    public IReadOnlyList<WorkspaceCardView> SearchResults { get; private set; }

    /// <summary>
    /// Initializes the default workspace piles and empty state.
    /// </summary>
    public Task InitializeAsync()
    {
        if (piles.Count == 0)
        {
            piles.AddRange(
            [
                new PileDefinitionContract { PileId = CommandZonePileId, Name = "Command Zone", SortOrder = 0 },
                new PileDefinitionContract { PileId = MainboardPileId, Name = "Mainboard", SortOrder = 1 },
                new PileDefinitionContract { PileId = "engine", Name = "Engine", SortOrder = 2 },
                new PileDefinitionContract { PileId = "interaction", Name = "Interaction", SortOrder = 3 },
                new PileDefinitionContract { PileId = "finishers", Name = "Finishers", SortOrder = 4 },
            ]);
        }

        Piles = piles.OrderBy(static pile => pile.SortOrder).ToArray();
        RefreshDerivedCards();
        State = stateFactory.CreateEmpty("Choose a commander to activate validation and synergy analysis.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates the current search query text.
    /// </summary>
    public Task UpdateSearchQueryAsync(string searchQuery)
    {
        SearchQuery = searchQuery;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes a search against the current server-backed search index.
    /// </summary>
    public async Task SearchAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            SearchResults = Array.Empty<WorkspaceCardView>();
            return;
        }

        try
        {
            IsBusy = true;
            var response = await cardSearchIndexClient.SearchAsync(SearchQuery, GetCommanderCardId(), GetCommanderColors(), cancellationToken).ConfigureAwait(false);
            SearchResults = response.Results.Select(MapSearchResult).ToArray();

            foreach (var card in SearchResults)
            {
                knownCards[card.CardId] = card;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch
        {
            State = stateFactory.CreateRecovery("Search is temporarily unavailable. Retry after the card index finishes loading.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Selects the commander and immediately refreshes authoritative validation and analysis.
    /// </summary>
    public async Task SetCommanderAsync(string cardId)
    {
        var commanderCard = GetKnownCard(cardId);

        foreach (var entry in entries)
        {
            entry.IsCommander = false;
            if (string.Equals(entry.AssignedPileId, CommandZonePileId, StringComparison.OrdinalIgnoreCase))
            {
                entry.AssignedPileId = MainboardPileId;
            }
        }

        var commanderEntry = entries.SingleOrDefault(entry => string.Equals(entry.CardId, cardId, StringComparison.OrdinalIgnoreCase));
        if (commanderEntry is null)
        {
            commanderEntry = new DeckEntryState(cardId)
            {
                Quantity = 1,
            };
            entries.Add(commanderEntry);
        }

        commanderEntry.IsCommander = true;
        commanderEntry.Quantity = 1;
        commanderEntry.AssignedPileId = CommandZonePileId;
        knownCards[cardId] = commanderCard;

        RefreshDerivedCards();
        await RefreshInsightsAsync(CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds a card to the mainboard and debounces the next validation or analysis refresh.
    /// </summary>
    public async Task AddCardAsync(string cardId)
    {
        if (string.IsNullOrWhiteSpace(GetCommanderCardId()))
        {
            await SetCommanderAsync(cardId).ConfigureAwait(false);
            return;
        }

        var card = GetKnownCard(cardId);
        knownCards[cardId] = card;

        var entry = entries.SingleOrDefault(existing => string.Equals(existing.CardId, cardId, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            entries.Add(new DeckEntryState(cardId)
            {
                Quantity = 1,
                AssignedPileId = MainboardPileId,
            });
        }
        else if (!entry.IsCommander)
        {
            entry.Quantity += 1;
            entry.AssignedPileId ??= MainboardPileId;
        }

        RefreshDerivedCards();
        await ScheduleRefreshAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Moves a card to a new pile while preserving immediate local feedback.
    /// </summary>
    public async Task MoveCardAsync(MoveCardRequest request)
    {
        var entry = entries.SingleOrDefault(existing => string.Equals(existing.CardId, request.CardId, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            return;
        }

        entry.AssignedPileId = entry.IsCommander ? CommandZonePileId : request.TargetPileId;
        RefreshDerivedCards();
        await ScheduleRefreshAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Removes a card from the current deck and refreshes server feedback.
    /// </summary>
    public async Task RemoveCardAsync(string cardId)
    {
        entries.RemoveAll(entry => string.Equals(entry.CardId, cardId, StringComparison.OrdinalIgnoreCase));
        RefreshDerivedCards();

        if (string.IsNullOrWhiteSpace(GetCommanderCardId()))
        {
            Analysis = null;
            State = stateFactory.CreateEmpty("Choose a commander to activate validation and synergy analysis.");
            return;
        }

        await ScheduleRefreshAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Retries server synchronization after a recoverable failure.
    /// </summary>
    public async Task RetryAsync()
    {
        if (string.IsNullOrWhiteSpace(GetCommanderCardId()))
        {
            State = stateFactory.CreateEmpty("Choose a commander to activate validation and synergy analysis.");
            return;
        }

        await RefreshInsightsAsync(CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Releases debounced refresh resources when the view model leaves scope.
    /// </summary>
    public void Dispose()
    {
        refreshCancellationTokenSource?.Cancel();
        refreshCancellationTokenSource?.Dispose();
    }

    private async Task ScheduleRefreshAsync()
    {
        refreshCancellationTokenSource?.Cancel();
        refreshCancellationTokenSource?.Dispose();
        refreshCancellationTokenSource = new CancellationTokenSource();

        try
        {
            await Task.Delay(180, refreshCancellationTokenSource.Token).ConfigureAwait(false);
            await RefreshInsightsAsync(refreshCancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (refreshCancellationTokenSource.IsCancellationRequested)
        {
        }
    }

    private async Task RefreshInsightsAsync(CancellationToken cancellationToken)
    {
        var commanderCardId = GetCommanderCardId();
        if (string.IsNullOrWhiteSpace(commanderCardId))
        {
            Analysis = null;
            State = stateFactory.CreateEmpty("Choose a commander to activate validation and synergy analysis.");
            return;
        }

        try
        {
            IsBusy = true;
            var snapshot = CreateSnapshot(commanderCardId);
            var validationTask = deckWorkspaceClient.ValidateAsync(snapshot, cancellationToken);
            var analysisTask = deckWorkspaceClient.AnalyzeAsync(snapshot, cancellationToken);

            await Task.WhenAll(validationTask, analysisTask).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            State = stateFactory.CreateReady(await validationTask.ConfigureAwait(false));
            Analysis = await analysisTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch
        {
            Analysis = null;
            State = stateFactory.CreateRecovery("The workspace lost sync with the server. Retry to refresh validation and analysis.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private DeckSnapshotContract CreateSnapshot(string commanderCardId) => new()
    {
        DeckId = "workspace-demo",
        Name = "Commander Synergy Sphere",
        CommanderCardId = commanderCardId,
        Entries = entries.Select(static entry => new DeckEntryContract
        {
            CardId = entry.CardId,
            Quantity = entry.Quantity,
            AssignedPileId = entry.AssignedPileId,
            IsCommander = entry.IsCommander,
            IsCompanion = entry.IsCompanion,
        }).ToArray(),
        Piles = Piles,
    };

    private void RefreshDerivedCards()
    {
        Cards = entries
            .Select(MapEntry)
            .OrderByDescending(static card => card.IsCommander)
            .ThenBy(static card => card.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private WorkspaceCardView MapEntry(DeckEntryState entry)
    {
        var knownCard = GetKnownCard(entry.CardId);
        return knownCard with
        {
            Quantity = entry.Quantity,
            AssignedPileId = entry.IsCommander ? CommandZonePileId : entry.AssignedPileId ?? MainboardPileId,
            IsCommander = entry.IsCommander,
            IsCompanion = entry.IsCompanion,
        };
    }

    private WorkspaceCardView MapSearchResult(CardSearchResultContract result)
    {
        var faces = result.HasMultipleFaces
            ? new[]
            {
                new WorkspaceCardFaceView(result.Name, result.ManaCost, result.TypeLine, result.ImageUri, true),
                new WorkspaceCardFaceView($"{result.Name} Reverse", null, "Alternate face", result.ImageUri, false),
            }
            : new[]
            {
                new WorkspaceCardFaceView(result.Name, result.ManaCost, result.TypeLine, result.ImageUri, true),
            };

        return new WorkspaceCardView
        {
            CardId = result.CardId,
            Name = result.Name,
            ManaCost = result.ManaCost,
            TypeLine = result.TypeLine,
            ColorIdentity = result.ColorIdentity,
            SaltScore = result.SaltScore,
            ImageUri = result.ImageUri,
            HasMultipleFaces = result.HasMultipleFaces,
            Faces = faces,
            AssignedPileId = MainboardPileId,
            Quantity = 1,
        };
    }

    private WorkspaceCardView GetKnownCard(string cardId)
    {
        if (knownCards.TryGetValue(cardId, out var knownCard))
        {
            return knownCard;
        }

        var placeholderCard = new WorkspaceCardView
        {
            CardId = cardId,
            Name = cardId,
            TypeLine = "Unknown Card",
            ColorIdentity = Array.Empty<string>(),
            Faces = [new WorkspaceCardFaceView(cardId, null, "Unknown Card", null, true)],
            AssignedPileId = MainboardPileId,
            Quantity = 1,
        };

        knownCards[cardId] = placeholderCard;
        return placeholderCard;
    }

    private string? GetCommanderCardId() => entries.SingleOrDefault(static entry => entry.IsCommander)?.CardId;

    private IReadOnlyList<string> GetCommanderColors()
    {
        var commanderCardId = GetCommanderCardId();
        return commanderCardId is null
            ? Array.Empty<string>()
            : GetKnownCard(commanderCardId).ColorIdentity;
    }

    private sealed class DeckEntryState
    {
        public DeckEntryState(string cardId)
        {
            CardId = cardId;
        }

        public string CardId { get; }

        public int Quantity { get; set; } = 1;

        public string? AssignedPileId { get; set; }

        public bool IsCommander { get; set; }

        public bool IsCompanion { get; set; }
    }
}

/// <summary>
/// Represents a rendered card in the interactive workspace.
/// </summary>
public sealed record WorkspaceCardView
{
    public required string CardId { get; init; }

    public required string Name { get; init; }

    public string? ManaCost { get; init; }

    public required string TypeLine { get; init; }

    public required IReadOnlyList<string> ColorIdentity { get; init; }

    public decimal? SaltScore { get; init; }

    public string? ImageUri { get; init; }

    public bool HasMultipleFaces { get; init; }

    public required IReadOnlyList<WorkspaceCardFaceView> Faces { get; init; }

    public string AssignedPileId { get; init; } = DeckWorkspaceViewModel.MainboardPileId;

    public int Quantity { get; init; } = 1;

    public bool IsCommander { get; init; }

    public bool IsCompanion { get; init; }
}

/// <summary>
/// Represents a single renderable card face.
/// </summary>
public sealed record WorkspaceCardFaceView(string Name, string? ManaCost, string TypeLine, string? ImageUri, bool IsPrimaryFace);

/// <summary>
/// Represents a requested pile move for a specific card.
/// </summary>
public sealed record MoveCardRequest(string CardId, string TargetPileId);