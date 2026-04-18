using Bunit;
using CommandSynergy.Components.Cards;
using CommandSynergy.Components.Decks;
using FluentAssertions;

namespace CommandSynergy.WebUI.Tests.Components;

public sealed class CommanderCardTests : BunitContext
{
    [Fact]
    public void Commander_card_renders_full_artwork_image_when_image_uri_is_available()
    {
        var cut = Render<CommanderCard>(parameters => parameters
            .Add(component => component.Card, CreateSingleFaceCard() with
            {
                ImageUri = "https://cards.example/swords-to-plowshares.jpg",
                Faces = [new WorkspaceCardFaceView("Swords to Plowshares", "{W}", "Instant", "https://cards.example/swords-to-plowshares.jpg", true)],
            }));

        var image = cut.Find("img.commander-card__art-image");
        image.GetAttribute("src").Should().Be("https://cards.example/swords-to-plowshares.jpg");
        image.GetAttribute("alt").Should().Be("Swords to Plowshares artwork");
    }

    [Fact]
    public void Commander_card_renders_salt_badge_when_available()
    {
        var cut = Render<CommanderCard>(parameters => parameters
            .Add(component => component.Card, CreateSingleFaceCard() with { SaltScore = 2.7m }));

        cut.Find("[data-testid='salt-badge-swords-to-plowshares']").TextContent.Should().Contain("Salt 2.7");
    }

    [Fact]
    public void Commander_card_renders_face_toggle_for_multi_face_cards()
    {
        var cut = Render<CommanderCard>(parameters => parameters
            .Add(component => component.Card, CreateSingleFaceCard() with
            {
                CardId = "sea-gate-restoration",
                Name = "Sea Gate Restoration",
                HasMultipleFaces = true,
                Faces =
                [
                    new WorkspaceCardFaceView("Sea Gate Restoration", "{4}{U}{U}{U}", "Sorcery", null, true),
                    new WorkspaceCardFaceView("Sea Gate, Reborn", null, "Land", null, false),
                ],
            }));

        cut.Find("[data-testid='face-toggle-sea-gate-restoration']").TextContent.Should().Contain("Flip face");
        cut.Markup.Should().Contain("Sea Gate, Reborn");
    }

    private static WorkspaceCardView CreateSingleFaceCard() => new()
    {
        CardId = "swords-to-plowshares",
        Name = "Swords to Plowshares",
        ManaCost = "{W}",
        TypeLine = "Instant",
        ColorIdentity = ["W"],
        Faces = [new WorkspaceCardFaceView("Swords to Plowshares", "{W}", "Instant", null, true)],
        ImageUri = null,
        Quantity = 1,
    };
}