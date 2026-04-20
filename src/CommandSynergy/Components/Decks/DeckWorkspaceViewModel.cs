using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks;
using CommandSynergy.Application.Decks.Portability;
using CommandSynergy.Client.Services;
using CommandSynergy.Domain.Cards;
using System.Globalization;
using Microsoft.JSInterop;

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
    private readonly ImportedDeckLibraryState importedDeckLibraryState;
    private readonly List<PileDefinitionContract> piles = [];
    private readonly List<DeckEntryState> entries = [];
    private readonly Dictionary<string, WorkspaceCardView> knownCards = new(StringComparer.OrdinalIgnoreCase);
    private readonly IReadOnlyList<FormatOptionView> supportedImportFormats;
    private readonly IReadOnlyList<FormatOptionView> supportedExportFormats;
    private ImportedDeckRecord? pendingImportedDeck;
    private CancellationTokenSource? refreshCancellationTokenSource;

    /// <summary>
    /// Creates a workspace state coordinator.
    /// </summary>
    public DeckWorkspaceViewModel(
        DeckWorkspaceStateFactory stateFactory,
        CardSearchIndexClient cardSearchIndexClient,
        DeckWorkspaceClient deckWorkspaceClient,
        ImportedDeckLibraryState importedDeckLibraryState)
    {
        this.stateFactory = stateFactory;
        this.cardSearchIndexClient = cardSearchIndexClient;
        this.deckWorkspaceClient = deckWorkspaceClient;
        this.importedDeckLibraryState = importedDeckLibraryState;
        State = stateFactory.CreateLoading();
        Piles = Array.Empty<PileDefinitionContract>();
        Cards = Array.Empty<WorkspaceCardView>();
        SearchResults = Array.Empty<WorkspaceCardView>();
        supportedImportFormats =
        [
            new FormatOptionView("", "Auto-detect"),
            new FormatOptionView("moxfield-text", "Moxfield Text"),
            new FormatOptionView("manabox-text", "ManaBox Text"),
            new FormatOptionView("generic-plaintext", "Generic Plaintext"),
        ];
        supportedExportFormats = supportedImportFormats.Where(static option => !string.IsNullOrWhiteSpace(option.Value)).ToArray();
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

    public bool IsHydratingLibrary => importedDeckLibraryState.IsHydrating;

    public string? LibraryRecoveryMessage => importedDeckLibraryState.RecoveryMessage;

    public IReadOnlyList<ImportedDeckRecord> ImportedDecks => importedDeckLibraryState.Library.Decks;

    public string? ActiveImportedDeckId => importedDeckLibraryState.Library.ActiveDeckId;

    public IReadOnlyList<ImportDiagnostic> ActiveImportedDeckDiagnostics =>
        ImportedDecks.FirstOrDefault(deck => string.Equals(deck.ImportedDeckId, ActiveImportedDeckId, StringComparison.OrdinalIgnoreCase))?.Diagnostics
        ?? Array.Empty<ImportDiagnostic>();

    public IReadOnlyList<FormatOptionView> SupportedImportFormats => supportedImportFormats;

    public IReadOnlyList<FormatOptionView> SupportedExportFormats => supportedExportFormats;

    public string ImportDocumentText { get; private set; } = string.Empty;

    public string? SelectedImportFormatId { get; private set; }

    public string SelectedExportFormatId { get; private set; } = "moxfield-text";

    public string? ImportStatusMessage { get; private set; }

    public bool HasPendingDuplicateImport => pendingImportedDeck is not null;

    public string? PendingDuplicateImportName => pendingImportedDeck?.Name;

    public string? PendingDuplicateImportTargetName => pendingImportedDeck is null ? null : GetNextImportedDeckCopyName(pendingImportedDeck.Name);

    public string? ExportStatusMessage { get; private set; }

    public ExportPreviewContract? ExportPreview { get; private set; }

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

    public async Task HydrateImportedDeckLibraryAsync()
    {
        await importedDeckLibraryState.HydrateAsync().ConfigureAwait(false);
    }

    public Task UpdateImportDocumentTextAsync(string importDocumentText)
    {
        ImportDocumentText = importDocumentText;
        ImportStatusMessage = null;
        ClearPendingDuplicateImport();
        return Task.CompletedTask;
    }

    public Task UpdateImportFormatAsync(string? formatId)
    {
        SelectedImportFormatId = string.IsNullOrWhiteSpace(formatId) ? null : formatId;
        ClearPendingDuplicateImport();
        return Task.CompletedTask;
    }

    public async Task ImportDeckAsync(CancellationToken cancellationToken = default)
    {
        ImportStatusMessage = null;
        ExportPreview = null;
        ExportStatusMessage = null;
        ClearPendingDuplicateImport();

        try
        {
            var result = await deckWorkspaceClient.ImportAsync(new DeckImportRequestContract
            {
                RawDocumentText = ImportDocumentText,
                HintedFormatId = SelectedImportFormatId,
            }, cancellationToken).ConfigureAwait(false);

            var importedDeck = ImportedDeckRecord.FromContract(result.ImportedDeck);
            if (result.RequiresFormatConfirmation && string.IsNullOrWhiteSpace(SelectedImportFormatId))
            {
                ImportStatusMessage = $"Multiple formats matched. Choose one of: {string.Join(", ", result.CandidateFormatIds)}.";
                return;
            }

            var existingDeck = FindImportedDeckByName(importedDeck.Name);
            if (existingDeck is not null)
            {
                pendingImportedDeck = importedDeck;
                ImportStatusMessage = $"A deck named '{importedDeck.Name}' already exists. Update the saved deck or import a suffixed copy.";
                return;
            }

            await importedDeckLibraryState.SaveImportedDeckAsync(importedDeck, setActive: true, cancellationToken).ConfigureAwait(false);
            ImportStatusMessage = importedDeck.Diagnostics.Count == 0
                ? $"Imported '{importedDeck.Name}' and saved it locally."
                : $"Imported '{importedDeck.Name}' with {importedDeck.Diagnostics.Count} diagnostic(s).";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (JSException exception)
        {
            ImportStatusMessage = $"Imported deck data could not be saved locally. {exception.Message}";
        }
        catch (InvalidOperationException exception)
        {
            ImportStatusMessage = exception.Message;
        }
    }

    public async Task UpdateExistingImportedDeckAsync(CancellationToken cancellationToken = default)
    {
        if (pendingImportedDeck is null)
        {
            ImportStatusMessage = "Import a deck before resolving a duplicate name.";
            return;
        }

        var importedDeck = pendingImportedDeck;
        var existingDeck = FindImportedDeckByName(importedDeck.Name);
        if (existingDeck is null)
        {
            await importedDeckLibraryState.SaveImportedDeckAsync(importedDeck, setActive: true, cancellationToken).ConfigureAwait(false);
            ClearPendingDuplicateImport();
            ImportStatusMessage = importedDeck.Diagnostics.Count == 0
                ? $"Imported '{importedDeck.Name}' and saved it locally."
                : $"Imported '{importedDeck.Name}' with {importedDeck.Diagnostics.Count} diagnostic(s).";
            return;
        }

        var updatedDeck = importedDeck with
        {
            ImportedDeckId = existingDeck.ImportedDeckId,
            Name = existingDeck.Name,
            LastOpenedUtc = existingDeck.LastOpenedUtc,
            NormalizedDeck = importedDeck.NormalizedDeck with
            {
                DeckName = existingDeck.Name,
            },
        };

        await importedDeckLibraryState.SaveImportedDeckAsync(updatedDeck, setActive: true, cancellationToken).ConfigureAwait(false);
        ClearPendingDuplicateImport();
        ImportStatusMessage = updatedDeck.Diagnostics.Count == 0
            ? $"Updated '{updatedDeck.Name}' from the latest import and kept it selected."
            : $"Updated '{updatedDeck.Name}' with {updatedDeck.Diagnostics.Count} diagnostic(s).";
    }

    public async Task ImportDuplicateAsNewDeckAsync(CancellationToken cancellationToken = default)
    {
        if (pendingImportedDeck is null)
        {
            ImportStatusMessage = "Import a deck before resolving a duplicate name.";
            return;
        }

        var importedDeck = pendingImportedDeck;
        var nextDeckName = GetNextImportedDeckCopyName(importedDeck.Name);
        var renamedDeck = importedDeck with
        {
            Name = nextDeckName,
            NormalizedDeck = importedDeck.NormalizedDeck with
            {
                DeckName = nextDeckName,
            },
        };

        await importedDeckLibraryState.SaveImportedDeckAsync(renamedDeck, setActive: true, cancellationToken).ConfigureAwait(false);
        ClearPendingDuplicateImport();
        ImportStatusMessage = renamedDeck.Diagnostics.Count == 0
            ? $"Imported '{renamedDeck.Name}' as a new saved deck."
            : $"Imported '{renamedDeck.Name}' with {renamedDeck.Diagnostics.Count} diagnostic(s).";
    }

    public Task SelectImportedDeckAsync(string deckId) => importedDeckLibraryState.SetActiveDeckAsync(deckId);

    /// <summary>
    /// Deletes an imported deck from the local browser library.
    /// </summary>
    public async Task DeleteImportedDeckAsync(string deckId, CancellationToken cancellationToken = default)
    {
        var deck = ImportedDecks.FirstOrDefault(candidate => string.Equals(candidate.ImportedDeckId, deckId, StringComparison.OrdinalIgnoreCase));
        if (deck is null)
        {
            ImportStatusMessage = "Choose an imported deck before deleting it.";
            return;
        }

        ClearPendingDuplicateImport();
        await importedDeckLibraryState.RemoveImportedDeckAsync(deckId, cancellationToken).ConfigureAwait(false);
        ImportStatusMessage = $"Deleted '{deck.Name}' from the local library.";
    }

    public async Task OpenActiveImportedDeckAsync()
    {
        var deck = ImportedDecks.FirstOrDefault(candidate => string.Equals(candidate.ImportedDeckId, ActiveImportedDeckId, StringComparison.OrdinalIgnoreCase));
        if (deck is null)
        {
            ImportStatusMessage = "Choose an imported deck before opening a workspace copy.";
            return;
        }

        var snapshot = deckWorkspaceClient.CreateWorkingCopy(deck.NormalizedDeck, deck.ImportedDeckId, deck.Name);
        LoadSnapshotIntoWorkspace(snapshot, deck.NormalizedDeck);
        await RefreshInsightsAsync(CancellationToken.None).ConfigureAwait(false);
        ImportStatusMessage = $"Opened '{deck.Name}' as a working copy in the workspace.";
    }

    public Task UpdateExportFormatAsync(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            SelectedExportFormatId = value;
        }

        return Task.CompletedTask;
    }

    public async Task GenerateExportPreviewAsync(CancellationToken cancellationToken = default)
    {
        ExportPreview = null;
        ExportStatusMessage = null;

        PortableDeckSnapshot snapshot;
        try
        {
            snapshot = CreatePortableSnapshotFromWorkspace();
        }
        catch (InvalidOperationException exception)
        {
            ExportStatusMessage = exception.Message;
            return;
        }

        var request = new DeckExportRequestContract
        {
            ImportedDeckId = ActiveImportedDeckId ?? "workspace",
            TargetFormatId = SelectedExportFormatId,
        };

        var preview = await deckWorkspaceClient.ExportAsync(request, snapshot, cancellationToken).ConfigureAwait(false);
        ExportPreview = new ExportPreviewContract
        {
            TargetFormatId = preview.TargetFormatId,
            DocumentText = preview.DocumentText,
            Warnings = preview.Warnings,
            GeneratedUtc = DateTimeOffset.UtcNow,
        };

        ExportStatusMessage = preview.Warnings.Count == 0
            ? $"Prepared {GetFormatDisplayName(preview.TargetFormatId)} export preview."
            : $"Prepared export preview with {preview.Warnings.Count} warning(s).";
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
        if (!commanderCard.IsCommanderEligible)
        {
            if (string.IsNullOrWhiteSpace(GetCommanderCardId()))
            {
                Analysis = null;
                State = stateFactory.CreateEmpty("Choose a legal commander to activate validation and synergy analysis.");
            }

            return;
        }

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
        var card = GetKnownCard(cardId);
        knownCards[cardId] = card;

        if (string.IsNullOrWhiteSpace(GetCommanderCardId()))
        {
            if (card.IsCommanderEligible)
            {
                await SetCommanderAsync(cardId).ConfigureAwait(false);
                return;
            }

            var commanderlessEntry = entries.SingleOrDefault(existing => string.Equals(existing.CardId, cardId, StringComparison.OrdinalIgnoreCase));
            if (commanderlessEntry is null)
            {
                entries.Add(new DeckEntryState(cardId)
                {
                    Quantity = 1,
                    AssignedPileId = MainboardPileId,
                });
            }
            else if (!commanderlessEntry.IsCommander)
            {
                commanderlessEntry.Quantity += 1;
                commanderlessEntry.AssignedPileId ??= MainboardPileId;
            }

            RefreshDerivedCards();
            Analysis = null;
            State = stateFactory.CreateEmpty("Choose a legal commander to activate validation and synergy analysis.");
            return;
        }

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

    private ImportedDeckRecord? FindImportedDeckByName(string deckName) =>
        ImportedDecks.FirstOrDefault(deck => string.Equals(deck.Name, deckName, StringComparison.OrdinalIgnoreCase));

    private string GetNextImportedDeckCopyName(string baseDeckName)
    {
        var existingNames = new HashSet<string>(ImportedDecks.Select(static deck => deck.Name), StringComparer.OrdinalIgnoreCase);
        var suffix = 1;

        while (true)
        {
            var candidateName = string.Create(CultureInfo.InvariantCulture, $"{baseDeckName} {suffix:000}");
            if (!existingNames.Contains(candidateName))
            {
                return candidateName;
            }

            suffix += 1;
        }
    }

    private void ClearPendingDuplicateImport()
    {
        pendingImportedDeck = null;
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

    private PortableDeckSnapshot CreatePortableSnapshotFromWorkspace()
    {
        var commanderCardId = GetCommanderCardId();
        if (string.IsNullOrWhiteSpace(commanderCardId))
        {
            throw new InvalidOperationException("Choose a commander before exporting the current workspace.");
        }

        var portableEntries = entries
            .Select(entry =>
            {
                var card = GetKnownCard(entry.CardId);
                return new PortableDeckEntry(
                    entry.CardId,
                    card.Name,
                    card.Name,
                    card.ManaCost,
                    card.TypeLine,
                    card.ColorIdentity,
                    card.SaltScore,
                    card.ImageUri,
                    card.HasMultipleFaces,
                    card.CommanderEligibilityBasis,
                    entry.Quantity,
                    entry.IsCommander ? CommandZonePileId : entry.AssignedPileId ?? MainboardPileId,
                    entry.IsCommander,
                    entry.IsCompanion,
                    ParseConfidence.Exact);
            })
            .ToArray();

        var sections = Piles
            .Select(pile => new DeckSectionState(
                pile.PileId,
                pile.Name,
                ResolvePileRole(pile.PileId),
                pile.SortOrder,
                portableEntries.Where(entry => string.Equals(entry.SectionId, pile.PileId, StringComparison.OrdinalIgnoreCase)).Sum(static entry => entry.Quantity)))
            .ToArray();

        return new PortableDeckSnapshot(
            "Commander Synergy Sphere",
            [commanderCardId],
            portableEntries.FirstOrDefault(static entry => entry.IsCompanion)?.CardId,
            portableEntries,
            sections,
            portableEntries.Sum(static entry => entry.Quantity),
            false);
    }

    private void LoadSnapshotIntoWorkspace(DeckSnapshotContract snapshot, PortableDeckSnapshot portableSnapshot)
    {
        if (snapshot.Piles.Count > 0)
        {
            piles.Clear();
            piles.AddRange(snapshot.Piles.OrderBy(static pile => pile.SortOrder));
            Piles = piles.ToArray();
        }

        entries.Clear();

        foreach (var entry in snapshot.Entries)
        {
            entries.Add(new DeckEntryState(entry.CardId)
            {
                Quantity = entry.Quantity,
                AssignedPileId = entry.AssignedPileId,
                IsCommander = entry.IsCommander,
                IsCompanion = entry.IsCompanion,
            });
        }

        foreach (var portableEntry in portableSnapshot.Entries.Where(static entry => !string.IsNullOrWhiteSpace(entry.CardId)))
        {
            knownCards[portableEntry.CardId!] = new WorkspaceCardView
            {
                CardId = portableEntry.CardId!,
                Name = portableEntry.DisplayName,
                ManaCost = portableEntry.ManaCost,
                TypeLine = portableEntry.TypeLine ?? (portableEntry.IsCommander ? "Commander" : "Imported Card"),
                ColorIdentity = portableEntry.ColorIdentity,
                SaltScore = portableEntry.SaltScore,
                ImageUri = portableEntry.ImageUri,
                HasMultipleFaces = portableEntry.HasMultipleFaces,
                Faces = CreateFacesFromPortableEntry(portableEntry),
                Quantity = portableEntry.Quantity,
                AssignedPileId = portableEntry.SectionId,
                IsCommander = portableEntry.IsCommander,
                IsCompanion = portableEntry.IsCompanion,
                CommanderEligibilityBasis = portableEntry.CommanderEligibilityBasis,
            };
        }

        RefreshDerivedCards();
    }

    private static DeckSectionRole ResolvePileRole(string? pileId)
    {
        return pileId?.Trim().ToLowerInvariant() switch
        {
            CommandZonePileId => DeckSectionRole.Commander,
            "companion" => DeckSectionRole.Companion,
            "sideboard" => DeckSectionRole.Sideboard,
            "maybeboard" => DeckSectionRole.Maybeboard,
            MainboardPileId => DeckSectionRole.Mainboard,
            _ => DeckSectionRole.Custom,
        };
    }

    private string GetFormatDisplayName(string formatId) =>
        supportedExportFormats.Concat(supportedImportFormats)
            .FirstOrDefault(option => string.Equals(option.Value, formatId, StringComparison.OrdinalIgnoreCase))?.Label
        ?? formatId;

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
            CommanderEligibilityBasis = result.CommanderEligibilityBasis,
        };
    }

    private static IReadOnlyList<WorkspaceCardFaceView> CreateFacesFromPortableEntry(PortableDeckEntry portableEntry)
    {
        var primaryTypeLine = portableEntry.TypeLine ?? (portableEntry.IsCommander ? "Commander" : "Imported Card");
        if (portableEntry.HasMultipleFaces)
        {
            return
            [
                new WorkspaceCardFaceView(portableEntry.DisplayName, portableEntry.ManaCost, primaryTypeLine, portableEntry.ImageUri, true),
                new WorkspaceCardFaceView($"{portableEntry.DisplayName} Reverse", null, "Alternate face", portableEntry.ImageUri, false),
            ];
        }

        return [new WorkspaceCardFaceView(portableEntry.DisplayName, portableEntry.ManaCost, primaryTypeLine, portableEntry.ImageUri, true)];
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
            CommanderEligibilityBasis = CommanderEligibilityBasis.Unknown,
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

public sealed record FormatOptionView(string Value, string Label);

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

    public CommanderEligibilityBasis CommanderEligibilityBasis { get; init; } = CommanderEligibilityBasis.Unknown;

    public required IReadOnlyList<WorkspaceCardFaceView> Faces { get; init; }

    public string AssignedPileId { get; init; } = DeckWorkspaceViewModel.MainboardPileId;

    public int Quantity { get; init; } = 1;

    public bool IsCommander { get; init; }

    public bool IsCompanion { get; init; }

    public bool IsCommanderEligible =>
        CommanderEligibilityBasis is CommanderEligibilityBasis.LegendaryCreature or CommanderEligibilityBasis.OracleTextException;
}

/// <summary>
/// Represents a single renderable card face.
/// </summary>
public sealed record WorkspaceCardFaceView(string Name, string? ManaCost, string TypeLine, string? ImageUri, bool IsPrimaryFace);

/// <summary>
/// Represents a requested pile move for a specific card.
/// </summary>
public sealed record MoveCardRequest(string CardId, string TargetPileId);