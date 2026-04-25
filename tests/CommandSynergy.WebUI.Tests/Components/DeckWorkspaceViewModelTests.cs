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
    public async Task OpenActiveImportedDeckAsync_keeps_commanderless_import_editable_without_server_refresh()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-04-20T00:00:00Z"));
        var libraryState = new ImportedDeckLibraryState(
            new ImportedDeckLibraryStore(new StubLocalStorageService(), new ImportedDeckLibrarySerializer(), timeProvider),
            timeProvider);

        var importedDeck = new ImportedDeckRecord(
            "deck-1",
            "Mystery Stack",
            "generic-plaintext",
            timeProvider.GetUtcNow(),
            null,
            "Deck: Mystery Stack",
            new PortableDeckSnapshot(
                "Mystery Stack",
                Array.Empty<string>(),
                null,
                [
                    new PortableDeckEntry("sol-ring", "1 Sol Ring", "Sol Ring", "{1}", "Artifact", Array.Empty<string>(), 0.5m, "https://cards.example/sol-ring.jpg", false, Domain.Cards.CommanderEligibilityBasis.Unknown, 1, "mainboard", false, false, ParseConfidence.Exact),
                ],
                [
                    new DeckSectionState("mainboard", "Mainboard", DeckSectionRole.Mainboard, 1, 1),
                ],
                1,
                false),
            Array.Empty<ImportDiagnostic>(),
            Array.Empty<string>(),
            new Dictionary<string, string>());

        await libraryState.SaveImportedDeckAsync(importedDeck, setActive: true);

        var workspaceClient = new CountingDeckWorkspaceClient();

        using var sut = new DeckWorkspaceViewModel(
            new DeckWorkspaceStateFactory(),
            new StubCardSearchIndexClient(),
            workspaceClient,
            libraryState);

        await sut.InitializeAsync();
        await sut.OpenActiveImportedDeckAsync();

        sut.Cards.Should().ContainSingle(card => card.CardId == "sol-ring");
        sut.Piles.Should().Contain(pile => pile.PileId == DeckWorkspaceViewModel.CommandZonePileId);
        sut.Piles.Should().Contain(pile => pile.PileId == DeckWorkspaceViewModel.MainboardPileId);
        sut.State.Status.Should().Be(DeckWorkspaceStatus.Empty);
        sut.Analysis.Should().BeNull();
        workspaceClient.ValidateCallCount.Should().Be(0);
        workspaceClient.AnalyzeCallCount.Should().Be(0);
    }

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

    [Fact]
    public async Task SetCommanderAsync_promotes_existing_deck_card_and_refreshes_server_feedback()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-04-20T00:00:00Z"));
        var libraryState = new ImportedDeckLibraryState(
            new ImportedDeckLibraryStore(new StubLocalStorageService(), new ImportedDeckLibrarySerializer(), timeProvider),
            timeProvider);

        var importedDeck = new ImportedDeckRecord(
            "deck-1",
            "Commander Candidate",
            "generic-plaintext",
            timeProvider.GetUtcNow(),
            null,
            "Deck: Commander Candidate",
            new PortableDeckSnapshot(
                "Commander Candidate",
                Array.Empty<string>(),
                null,
                [
                    new PortableDeckEntry("isshin-two-heavens-as-one", "1 Isshin, Two Heavens as One", "Isshin, Two Heavens as One", "{R}{W}{B}", "Legendary Creature", ["R", "W", "B"], 0.7m, "https://cards.example/isshin.jpg", false, Domain.Cards.CommanderEligibilityBasis.LegendaryCreature, 1, "mainboard", false, false, ParseConfidence.Exact),
                    new PortableDeckEntry("sol-ring", "1 Sol Ring", "Sol Ring", "{1}", "Artifact", Array.Empty<string>(), 0.5m, "https://cards.example/sol-ring.jpg", false, Domain.Cards.CommanderEligibilityBasis.Unknown, 1, "mainboard", false, false, ParseConfidence.Exact),
                ],
                [
                    new DeckSectionState("mainboard", "Mainboard", DeckSectionRole.Mainboard, 1, 2),
                ],
                2,
                false),
            Array.Empty<ImportDiagnostic>(),
            Array.Empty<string>(),
            new Dictionary<string, string>());

        await libraryState.SaveImportedDeckAsync(importedDeck, setActive: true);
        var workspaceClient = new CountingDeckWorkspaceClient();

        using var sut = new DeckWorkspaceViewModel(
            new DeckWorkspaceStateFactory(),
            new StubCardSearchIndexClient(),
            workspaceClient,
            libraryState);

        await sut.InitializeAsync();
        await sut.OpenActiveImportedDeckAsync();
        await sut.SetCommanderAsync("isshin-two-heavens-as-one");

        sut.Cards.Should().ContainSingle(card => card.CardId == "isshin-two-heavens-as-one" && card.IsCommander);
        sut.State.Status.Should().Be(DeckWorkspaceStatus.Ready);
        sut.Analysis.Should().NotBeNull();
        workspaceClient.ValidateCallCount.Should().Be(1);
        workspaceClient.AnalyzeCallCount.Should().Be(1);
        workspaceClient.LastValidationSnapshot.Should().NotBeNull();
        workspaceClient.LastValidationSnapshot!.CommanderCardId.Should().Be("isshin-two-heavens-as-one");
        workspaceClient.LastValidationSnapshot.Entries.Should().ContainSingle(entry => entry.CardId == "isshin-two-heavens-as-one" && entry.IsCommander);
    }

    [Fact]
    public async Task SetCommanderAsync_loads_initial_suggestions_and_reroll_skips_seen_cards()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-04-20T00:00:00Z"));
        var libraryState = CreateLibraryState(timeProvider);

        using var sut = new DeckWorkspaceViewModel(
            new DeckWorkspaceStateFactory(),
            new StubCardSearchIndexClient(),
            new SuggestionDeckWorkspaceClient(),
            libraryState);

        await sut.InitializeAsync();
        await sut.StartNewDeckAsync();
        await sut.UpdateSearchQueryAsync("Isshin");
        await sut.SearchAsync();
        await sut.SetCommanderAsync("isshin-two-heavens-as-one");

        sut.SuggestedCards.Select(static suggestion => suggestion.Card.CardId).Should().Equal("suggestion-a", "suggestion-b", "suggestion-c");

        await sut.RerollSuggestionsAsync();

        sut.SuggestedCards.Select(static suggestion => suggestion.Card.CardId).Should().Equal("suggestion-d", "suggestion-e", "suggestion-f");
    }

    [Fact]
    public async Task AddCardAsync_refreshes_suggestions_to_remove_added_card_from_future_pools()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-04-20T00:00:00Z"));
        var libraryState = CreateLibraryState(timeProvider);

        using var sut = new DeckWorkspaceViewModel(
            new DeckWorkspaceStateFactory(),
            new StubCardSearchIndexClient(),
            new SuggestionDeckWorkspaceClient(),
            libraryState);

        await sut.InitializeAsync();
        await sut.StartNewDeckAsync();
        await sut.UpdateSearchQueryAsync("Isshin");
        await sut.SearchAsync();
        await sut.SetCommanderAsync("isshin-two-heavens-as-one");

        sut.SuggestedCards.Select(static suggestion => suggestion.Card.CardId).Should().Equal("suggestion-a", "suggestion-b", "suggestion-c");

        await sut.AddCardAsync("suggestion-a");

        sut.Cards.Should().Contain(card => card.CardId == "suggestion-a" && !card.IsCommander);
        sut.SuggestedCards.Select(static suggestion => suggestion.Card.CardId).Should().Equal("suggestion-d", "suggestion-e", "suggestion-f");
    }

    [Fact]
    public async Task IncrementCardQuantityAsync_allows_commander_legal_duplicate_cards_to_grow_and_shrink()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-04-20T00:00:00Z"));
        var libraryState = CreateLibraryState(timeProvider);

        using var sut = new DeckWorkspaceViewModel(
            new DeckWorkspaceStateFactory(),
            new StubCardSearchIndexClient(),
            new StubDeckWorkspaceClient(),
            libraryState);

        await sut.InitializeAsync();
        await sut.StartNewDeckAsync();
        await sut.UpdateSearchQueryAsync("Shadowborn Apostle");
        await sut.SearchAsync();
        await sut.AddCardAsync("shadowborn-apostle");
        await sut.IncrementCardQuantityAsync("shadowborn-apostle");
        await sut.IncrementCardQuantityAsync("shadowborn-apostle");
        await sut.DecrementCardQuantityAsync("shadowborn-apostle");
        await sut.DecrementCardQuantityAsync("shadowborn-apostle");
        await sut.DecrementCardQuantityAsync("shadowborn-apostle");

        var apostle = sut.Cards.Should().ContainSingle(card => card.CardId == "shadowborn-apostle").Subject;
        apostle.Quantity.Should().Be(1);
        apostle.AllowsMultipleCopies.Should().BeTrue();
    }

    [Fact]
    public async Task IncrementCardQuantityAsync_ignores_singleton_cards()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-04-20T00:00:00Z"));
        var libraryState = CreateLibraryState(timeProvider);

        using var sut = new DeckWorkspaceViewModel(
            new DeckWorkspaceStateFactory(),
            new StubCardSearchIndexClient(),
            new StubDeckWorkspaceClient(),
            libraryState);

        await sut.InitializeAsync();
        await sut.StartNewDeckAsync();
        await sut.UpdateSearchQueryAsync("Sol Ring");
        await sut.SearchAsync();
        await sut.AddCardAsync("sol-ring");
        await sut.IncrementCardQuantityAsync("sol-ring");
        await sut.DecrementCardQuantityAsync("sol-ring");

        sut.Cards.Should().ContainSingle(card => card.CardId == "sol-ring" && card.Quantity == 1);
    }

    [Fact]
    public async Task StartNewDeckAsync_clears_workspace_and_detaches_active_imported_deck()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-04-20T00:00:00Z"));
        var libraryState = CreateLibraryState(timeProvider);
        var importedDeck = CreateImportedDeck("deck-1", "Isshin Pressure", timeProvider.GetUtcNow(), "Deck: Isshin");
        await libraryState.SaveImportedDeckAsync(importedDeck, setActive: true);

        using var sut = new DeckWorkspaceViewModel(
            new DeckWorkspaceStateFactory(),
            new StubCardSearchIndexClient(),
            new StubDeckWorkspaceClient(),
            libraryState);

        await sut.InitializeAsync();
        await sut.OpenActiveImportedDeckAsync();

        sut.Cards.Should().NotBeEmpty();
        sut.ActiveImportedDeckId.Should().Be("deck-1");

        await sut.StartNewDeckAsync();

        sut.ActiveImportedDeckId.Should().BeNull();
        sut.Cards.Should().BeEmpty();
        sut.SearchResults.Should().BeEmpty();
        sut.Analysis.Should().BeNull();
        sut.State.Status.Should().Be(DeckWorkspaceStatus.Empty);
        sut.ImportStatusMessage.Should().Be("Started a brand new deck workspace.");
        sut.ImportedDecks.Should().ContainSingle(deck => deck.ImportedDeckId == "deck-1");
    }

    [Fact]
    public async Task StartNewDeckAsync_breaks_live_persistence_link_to_previously_active_imported_deck()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-04-20T00:00:00Z"));
        var libraryState = CreateLibraryState(timeProvider);
        var importedDeck = CreateImportedDeck("deck-1", "Isshin Pressure", timeProvider.GetUtcNow(), "Deck: Isshin");
        await libraryState.SaveImportedDeckAsync(importedDeck, setActive: true);

        using var sut = new DeckWorkspaceViewModel(
            new DeckWorkspaceStateFactory(),
            new StubCardSearchIndexClient(),
            new StubDeckWorkspaceClient(),
            libraryState);

        await sut.InitializeAsync();
        await sut.OpenActiveImportedDeckAsync();
        await sut.StartNewDeckAsync();
        await sut.UpdateSearchQueryAsync("Sol Ring");
        await sut.SearchAsync();
        await sut.AddCardAsync("sol-ring");

        var persistedDeck = sut.ImportedDecks.Should().ContainSingle(deck => deck.ImportedDeckId == "deck-1").Subject;
        persistedDeck.NormalizedDeck.CommanderCardIds.Should().ContainSingle().Which.Should().Be("isshin-two-heavens-as-one");
        persistedDeck.NormalizedDeck.Entries.Should().ContainSingle();
        persistedDeck.NormalizedDeck.Entries.Should().ContainSingle(entry => entry.CardId == "isshin-two-heavens-as-one" && entry.SectionId == "command-zone");
        persistedDeck.NormalizedDeck.Entries.Should().NotContain(entry => entry.CardId == "sol-ring");
        sut.Cards.Should().ContainSingle(entry => entry.CardId == "sol-ring");
        sut.ActiveImportedDeckId.Should().BeNull();
    }

    [Fact]
    public async Task SaveNewDeckAsync_persists_detached_workspace_and_links_it_to_library()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-04-20T00:00:00Z"));
        var libraryState = CreateLibraryState(timeProvider);

        using var sut = new DeckWorkspaceViewModel(
            new DeckWorkspaceStateFactory(),
            new StubCardSearchIndexClient(),
            new StubDeckWorkspaceClient(),
            libraryState);

        await sut.InitializeAsync();
        await sut.StartNewDeckAsync();
        await sut.UpdateSearchQueryAsync("Sol Ring");
        await sut.SearchAsync();
        await sut.AddCardAsync("sol-ring");
        await sut.UpdateNewDeckNameAsync("Fresh Brew");
        await sut.SaveNewDeckAsync();

        sut.ImportedDecks.Should().ContainSingle();
        sut.IsWorkspaceLinkedToSavedDeck.Should().BeTrue();
        sut.ActiveWorkspaceDeckName.Should().Be("Fresh Brew");
        sut.ActiveImportedDeckId.Should().NotBeNullOrWhiteSpace();

        var savedDeck = sut.ImportedDecks[0];
        savedDeck.Name.Should().Be("Fresh Brew");
        savedDeck.SourceFormatId.Should().Be("workspace");
        savedDeck.NormalizedDeck.DeckName.Should().Be("Fresh Brew");
        savedDeck.NormalizedDeck.CommanderCardIds.Should().BeEmpty();
        savedDeck.NormalizedDeck.Entries.Should().ContainSingle(entry => entry.CardId == "sol-ring" && entry.SectionId == DeckWorkspaceViewModel.MainboardPileId);
        savedDeck.OriginalDocumentText.Should().Contain("Deck: Fresh Brew");
        sut.ImportStatusMessage.Should().Contain("Saved 'Fresh Brew'");
    }

    [Fact]
    public async Task SaveNewDeckAsync_suffixes_name_when_requested_name_already_exists()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-04-20T00:00:00Z"));
        var libraryState = CreateLibraryState(timeProvider);
        await libraryState.SaveImportedDeckAsync(CreateImportedDeck("deck-1", "Fresh Brew", timeProvider.GetUtcNow(), "Deck: Existing"), setActive: false);

        using var sut = new DeckWorkspaceViewModel(
            new DeckWorkspaceStateFactory(),
            new StubCardSearchIndexClient(),
            new StubDeckWorkspaceClient(),
            libraryState);

        await sut.InitializeAsync();
        await sut.StartNewDeckAsync();
        await sut.UpdateSearchQueryAsync("Sol Ring");
        await sut.SearchAsync();
        await sut.AddCardAsync("sol-ring");
        await sut.UpdateNewDeckNameAsync("Fresh Brew");
        await sut.SaveNewDeckAsync();

        sut.ImportedDecks.Should().HaveCount(2);
        sut.ImportedDecks.Should().Contain(deck => deck.Name == "Fresh Brew 001" && deck.SourceFormatId == "workspace");
        sut.ActiveWorkspaceDeckName.Should().Be("Fresh Brew 001");
        sut.ImportStatusMessage.Should().Contain("Saved 'Fresh Brew 001'");
    }

    [Fact]
    public async Task RenameActiveDeckAsync_updates_active_saved_deck_in_place()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-04-20T00:00:00Z"));
        var libraryState = CreateLibraryState(timeProvider);
        await libraryState.SaveImportedDeckAsync(CreateImportedDeck("deck-1", "Isshin Pressure", timeProvider.GetUtcNow(), "Deck: Isshin"), setActive: true);

        using var sut = new DeckWorkspaceViewModel(
            new DeckWorkspaceStateFactory(),
            new StubCardSearchIndexClient(),
            new StubDeckWorkspaceClient(),
            libraryState);

        await sut.InitializeAsync();
        await sut.OpenActiveImportedDeckAsync();
        await sut.UpdateLinkedDeckNameAsync("Isshin Pressure Reloaded");
        await sut.RenameActiveDeckAsync();

        sut.ActiveWorkspaceDeckName.Should().Be("Isshin Pressure Reloaded");
        sut.LinkedDeckName.Should().Be("Isshin Pressure Reloaded");
        sut.ImportedDecks.Should().ContainSingle(deck => deck.ImportedDeckId == "deck-1" && deck.Name == "Isshin Pressure Reloaded");
        sut.ImportedDecks[0].NormalizedDeck.DeckName.Should().Be("Isshin Pressure Reloaded");
        sut.LinkedDeckStatusHasError.Should().BeFalse();
        sut.LinkedDeckStatusMessage.Should().Be("Renamed saved deck to 'Isshin Pressure Reloaded'.");
    }

    [Fact]
    public async Task RenameActiveDeckAsync_rejects_duplicate_saved_deck_name()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-04-20T00:00:00Z"));
        var libraryState = CreateLibraryState(timeProvider);
        await libraryState.SaveImportedDeckAsync(CreateImportedDeck("deck-1", "Isshin Pressure", timeProvider.GetUtcNow(), "Deck: Isshin"), setActive: true);
        await libraryState.SaveImportedDeckAsync(CreateImportedDeck("deck-2", "Alesha Tempo", timeProvider.GetUtcNow().AddMinutes(1), "Deck: Alesha"), setActive: false);

        using var sut = new DeckWorkspaceViewModel(
            new DeckWorkspaceStateFactory(),
            new StubCardSearchIndexClient(),
            new StubDeckWorkspaceClient(),
            libraryState);

        await sut.InitializeAsync();
        await sut.OpenActiveImportedDeckAsync();
        await sut.UpdateLinkedDeckNameAsync("Alesha Tempo");
        await sut.RenameActiveDeckAsync();

        sut.ActiveWorkspaceDeckName.Should().Be("Isshin Pressure");
        sut.ImportedDecks.Should().Contain(deck => deck.ImportedDeckId == "deck-1" && deck.Name == "Isshin Pressure");
        sut.LinkedDeckStatusHasError.Should().BeTrue();
        sut.LinkedDeckStatusMessage.Should().Be("A saved deck named 'Alesha Tempo' already exists. Choose a different name.");
    }

    [Fact]
    public async Task ImportDeckAsync_prompts_for_duplicate_name_and_can_update_existing_deck()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-04-20T00:00:00Z"));
        var libraryState = CreateLibraryState(timeProvider);
        var existingDeck = CreateImportedDeck("deck-1", "Isshin Pressure", timeProvider.GetUtcNow(), "Deck: Original");
        await libraryState.SaveImportedDeckAsync(existingDeck, setActive: false);

        var importedDeck = CreateImportedDeck("deck-2", "Isshin Pressure", timeProvider.GetUtcNow().AddMinutes(5), "Deck: Updated");

        using var sut = new DeckWorkspaceViewModel(
            new DeckWorkspaceStateFactory(),
            new StubCardSearchIndexClient(),
            new StubDeckWorkspaceClient(new StubDeckImportService(CreateImportResult(importedDeck))),
            libraryState);

        await sut.InitializeAsync();
        await sut.UpdateImportDocumentTextAsync("Deck: Updated");
        await sut.ImportDeckAsync();

        sut.HasPendingDuplicateImport.Should().BeTrue();
        sut.PendingDuplicateImportName.Should().Be("Isshin Pressure");
        sut.ImportedDecks.Should().HaveCount(1);

        await sut.UpdateExistingImportedDeckAsync();

        sut.HasPendingDuplicateImport.Should().BeFalse();
        sut.ImportedDecks.Should().HaveCount(1);
        sut.ActiveImportedDeckId.Should().Be("deck-1");
        sut.ImportedDecks[0].ImportedDeckId.Should().Be("deck-1");
        sut.ImportedDecks[0].OriginalDocumentText.Should().Be("Deck: Updated");
        sut.ImportStatusMessage.Should().Contain("Updated 'Isshin Pressure'");
    }

    [Fact]
    public async Task ImportDeckAsync_can_save_duplicate_name_as_incremented_copy()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-04-20T00:00:00Z"));
        var libraryState = CreateLibraryState(timeProvider);
        await libraryState.SaveImportedDeckAsync(CreateImportedDeck("deck-1", "Isshin Pressure", timeProvider.GetUtcNow(), "Deck: Original"), setActive: false);
        await libraryState.SaveImportedDeckAsync(CreateImportedDeck("deck-2", "Isshin Pressure 001", timeProvider.GetUtcNow().AddMinutes(1), "Deck: Copy"), setActive: false);

        var importedDeck = CreateImportedDeck("deck-3", "Isshin Pressure", timeProvider.GetUtcNow().AddMinutes(2), "Deck: Latest");

        using var sut = new DeckWorkspaceViewModel(
            new DeckWorkspaceStateFactory(),
            new StubCardSearchIndexClient(),
            new StubDeckWorkspaceClient(new StubDeckImportService(CreateImportResult(importedDeck))),
            libraryState);

        await sut.InitializeAsync();
        await sut.UpdateImportDocumentTextAsync("Deck: Latest");
        await sut.ImportDeckAsync();

        sut.PendingDuplicateImportTargetName.Should().Be("Isshin Pressure 002");

        await sut.ImportDuplicateAsNewDeckAsync();

        sut.HasPendingDuplicateImport.Should().BeFalse();
        sut.ActiveImportedDeckId.Should().Be("deck-3");
        sut.ImportedDecks.Select(deck => deck.Name).Should().BeEquivalentTo(["Isshin Pressure 002", "Isshin Pressure 001", "Isshin Pressure"]);
        sut.ImportedDecks.Should().ContainSingle(deck => deck.ImportedDeckId == "deck-3" && deck.NormalizedDeck.DeckName == "Isshin Pressure 002");
        sut.ImportStatusMessage.Should().Contain("Imported 'Isshin Pressure 002' as a new saved deck.");
    }

    [Fact]
    public async Task ImportDeckAsync_surfaces_local_library_persistence_failures()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-04-20T00:00:00Z"));
        var libraryState = new ImportedDeckLibraryState(
            new ImportedDeckLibraryStore(new ThrowingLocalStorageService(), new ImportedDeckLibrarySerializer(), timeProvider),
            timeProvider);
        var importedDeck = CreateImportedDeck("deck-1", "Isshin Pressure", timeProvider.GetUtcNow(), "Deck: Latest");

        using var sut = new DeckWorkspaceViewModel(
            new DeckWorkspaceStateFactory(),
            new StubCardSearchIndexClient(),
            new StubDeckWorkspaceClient(new StubDeckImportService(CreateImportResult(importedDeck))),
            libraryState);

        await sut.InitializeAsync();
        await sut.UpdateImportDocumentTextAsync("Deck: Latest");
        await sut.ImportDeckAsync();

        sut.ImportStatusMessage.Should().Be("The imported deck library exceeds the safe local storage payload limit.");
        sut.ImportedDecks.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteImportedDeckAsync_removes_deck_and_reassigns_active_selection()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-04-20T00:00:00Z"));
        var libraryState = CreateLibraryState(timeProvider);
        var newestDeck = CreateImportedDeck("deck-2", "Alesha Tempo", timeProvider.GetUtcNow().AddMinutes(1), "Deck: Alesha");
        var activeDeck = CreateImportedDeck("deck-1", "Isshin Pressure", timeProvider.GetUtcNow(), "Deck: Isshin");

        await libraryState.SaveImportedDeckAsync(activeDeck, setActive: true);
        await libraryState.SaveImportedDeckAsync(newestDeck, setActive: false);

        using var sut = new DeckWorkspaceViewModel(
            new DeckWorkspaceStateFactory(),
            new StubCardSearchIndexClient(),
            new StubDeckWorkspaceClient(),
            libraryState);

        await sut.InitializeAsync();
        await sut.DeleteImportedDeckAsync("deck-1");

        sut.ImportedDecks.Select(deck => deck.ImportedDeckId).Should().Equal(["deck-2"]);
        sut.ActiveImportedDeckId.Should().Be("deck-2");
        sut.ImportStatusMessage.Should().Contain("Deleted 'Isshin Pressure'");
    }

    private static ImportedDeckLibraryState CreateLibraryState(FakeTimeProvider timeProvider) =>
        new(
            new ImportedDeckLibraryStore(new StubLocalStorageService(), new ImportedDeckLibrarySerializer(), timeProvider),
            timeProvider);

    private static ImportedDeckRecord CreateImportedDeck(string deckId, string name, DateTimeOffset importedAtUtc, string originalDocumentText) =>
        new(
            deckId,
            name,
            "manabox-text",
            importedAtUtc,
            null,
            originalDocumentText,
            new PortableDeckSnapshot(
                name,
                ["isshin-two-heavens-as-one"],
                null,
                [
                    new PortableDeckEntry("isshin-two-heavens-as-one", "1 Isshin, Two Heavens as One", "Isshin, Two Heavens as One", "{R}{W}{B}", "Legendary Creature", ["R", "W", "B"], 0.7m, "https://cards.example/isshin.jpg", false, Domain.Cards.CommanderEligibilityBasis.LegendaryCreature, 1, "command-zone", true, false, ParseConfidence.Exact),
                ],
                [
                    new DeckSectionState("command-zone", "Command Zone", DeckSectionRole.Commander, 0, 1),
                ],
                1,
                false),
            Array.Empty<ImportDiagnostic>(),
            Array.Empty<string>(),
            new Dictionary<string, string>());

    private static DeckImportResultContract CreateImportResult(ImportedDeckRecord importedDeck) => new()
    {
        DetectedFormatId = importedDeck.SourceFormatId,
        RequiresFormatConfirmation = false,
        CandidateFormatIds = Array.Empty<string>(),
        ImportedDeck = importedDeck.ToContract(),
        Diagnostics = Array.Empty<ImportDiagnosticContract>(),
    };

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
                SnapshotVersion = "stub",
                Results =
                [
                    new CardSearchResultContract
                    {
                        CardId = "sol-ring",
                        Name = "Sol Ring",
                        ManaCost = "{1}",
                        ManaValue = 1m,
                        TypeLine = "Artifact",
                        ColorIdentity = Array.Empty<string>(),
                        SaltScore = 0.5m,
                        ImageUri = "https://cards.example/sol-ring.jpg",
                        HasMultipleFaces = false,
                        CommanderEligibilityBasis = Domain.Cards.CommanderEligibilityBasis.Unknown,
                    },
                    new CardSearchResultContract
                    {
                        CardId = "isshin-two-heavens-as-one",
                        Name = "Isshin, Two Heavens as One",
                        ManaCost = "{R}{W}{B}",
                        ManaValue = 3m,
                        TypeLine = "Legendary Creature",
                        ColorIdentity = ["R", "W", "B"],
                        SaltScore = 0.7m,
                        ImageUri = "https://cards.example/isshin.jpg",
                        HasMultipleFaces = false,
                        CommanderEligibilityBasis = Domain.Cards.CommanderEligibilityBasis.LegendaryCreature,
                    },
                    new CardSearchResultContract
                    {
                        CardId = "shadowborn-apostle",
                        Name = "Shadowborn Apostle",
                        ManaCost = "{B}",
                        ManaValue = 1m,
                        TypeLine = "Creature — Human Cleric",
                        ColorIdentity = ["B"],
                        SaltScore = 0.2m,
                        ImageUri = "https://cards.example/shadowborn-apostle.jpg",
                        HasMultipleFaces = false,
                        AllowsMultipleCopies = true,
                        CommanderEligibilityBasis = Domain.Cards.CommanderEligibilityBasis.Unknown,
                    },
                ],
            });
        }
    }

    private class StubDeckWorkspaceClient : DeckWorkspaceClient
    {
        public StubDeckWorkspaceClient(IDeckImportService? deckImportService = null)
            : base(new HttpClient(), deckImportService ?? new StubDeckImportService(), new StubDeckExportService(), new WorkingCopyProjectionService())
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

        public override Task<DeckSuggestionsResponseContract> GetSuggestionsAsync(DeckSuggestionsRequestContract request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DeckSuggestionsResponseContract
            {
                CommanderCardId = request.Deck.CommanderCardId,
                Suggestions = Array.Empty<DeckSuggestionCardContract>(),
            });
    }

    private sealed class CountingDeckWorkspaceClient : StubDeckWorkspaceClient
    {
        public int ValidateCallCount { get; private set; }

        public int AnalyzeCallCount { get; private set; }

        public DeckSnapshotContract? LastValidationSnapshot { get; private set; }

        public override Task<DeckValidationResponseContract> ValidateAsync(DeckSnapshotContract deckSnapshot, CancellationToken cancellationToken = default)
        {
            ValidateCallCount += 1;
            LastValidationSnapshot = deckSnapshot;
            return base.ValidateAsync(deckSnapshot, cancellationToken);
        }

        public override Task<DeckAnalysisResponseContract> AnalyzeAsync(DeckSnapshotContract deckSnapshot, CancellationToken cancellationToken = default)
        {
            AnalyzeCallCount += 1;
            return base.AnalyzeAsync(deckSnapshot, cancellationToken);
        }
    }

    private sealed class SuggestionDeckWorkspaceClient : StubDeckWorkspaceClient
    {
        private static readonly CardSearchResultContract[] SuggestionCards =
        [
            CreateSuggestion("suggestion-a", "Suggestion A", 9.1m),
            CreateSuggestion("suggestion-b", "Suggestion B", 8.6m),
            CreateSuggestion("suggestion-c", "Suggestion C", 8.1m),
            CreateSuggestion("suggestion-d", "Suggestion D", 7.6m),
            CreateSuggestion("suggestion-e", "Suggestion E", 7.1m),
            CreateSuggestion("suggestion-f", "Suggestion F", 6.6m),
        ];

        public override Task<DeckSuggestionsResponseContract> GetSuggestionsAsync(DeckSuggestionsRequestContract request, CancellationToken cancellationToken = default)
        {
            var excludedCardIds = request.ExcludedCardIds
                .Concat(request.Deck.Entries.Select(static entry => entry.CardId))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var suggestions = SuggestionCards
                .Where(card => !excludedCardIds.Contains(card.CardId))
                .Take(request.Limit)
                .Select((card, index) => new DeckSuggestionCardContract
                {
                    Card = card,
                    CombinedScore = 92m - index,
                    ThemeScore = 88m - index,
                    EdhrecScore = 80m - index,
                })
                .ToArray();

            return Task.FromResult(new DeckSuggestionsResponseContract
            {
                CommanderCardId = request.Deck.CommanderCardId,
                Suggestions = suggestions,
            });
        }

        private static CardSearchResultContract CreateSuggestion(string cardId, string name, decimal eurPrice) => new()
        {
            CardId = cardId,
            Name = name,
            ManaCost = "{2}{W}",
            ManaValue = 3m,
            TypeLine = "Creature",
            ColorIdentity = ["W"],
            ImageUri = $"https://cards.example/{cardId}.jpg",
            EurPrice = eurPrice,
            CommanderEligibilityBasis = Domain.Cards.CommanderEligibilityBasis.Unknown,
        };
    }

    private sealed class StubDeckImportService : IDeckImportService
    {
        private readonly DeckImportResultContract? result;

        public StubDeckImportService(DeckImportResultContract? result = null)
        {
            this.result = result;
        }

        public Task<DeckImportResultContract> ImportAsync(DeckImportRequestContract request, CancellationToken cancellationToken = default) =>
            result is null
                ? throw new NotSupportedException()
                : Task.FromResult(result);
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

    private sealed class ThrowingLocalStorageService : ILocalStorageStringStore
    {
        public ValueTask<string?> GetItemAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult<string?>(null);
        }

        public ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }

        public ValueTask SetItemAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("The imported deck library exceeds the safe local storage payload limit.");
        }
    }
}
