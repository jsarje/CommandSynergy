using CommandSynergy.Domain.Decks;
using FluentAssertions;

namespace CommandSynergy.Domain.Tests.Decks;

public sealed class DeckAggregateTests
{
    [Fact]
    public void SetCommander_replaces_the_previous_commander_flag()
    {
        var deck = new Deck();
        deck.UpsertEntry("a", 1, isCommander: true);
        deck.UpsertEntry("b", 1);

        deck.SetCommander("b");

        deck.Entries.Should().ContainSingle(entry => entry.IsCommander && entry.CardId == "b");
        deck.Entries.Should().ContainSingle(entry => !entry.IsCommander && entry.CardId == "a");
    }

    [Fact]
    public void UpsertEntry_updates_the_existing_quantity()
    {
        var deck = new Deck();
        deck.UpsertEntry("sol-ring", 1);

        deck.UpsertEntry("sol-ring", 2);

        deck.Entries.Should().ContainSingle();
        deck.Entries[0].Quantity.Should().Be(2);
    }

    [Fact]
    public void AssignPile_requires_the_pile_to_exist()
    {
        var deck = new Deck();
        deck.UpsertEntry("sol-ring", 1);

        var act = () => deck.AssignPile("sol-ring", "ramp");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddPile_allows_entries_to_be_assigned_after_creation()
    {
        var deck = new Deck();
        deck.AddPile("ramp", "Ramp", 1);
        deck.UpsertEntry("sol-ring", 1);

        deck.AssignPile("sol-ring", "ramp");

        deck.Entries[0].AssignedPileId.Should().Be("ramp");
    }
}