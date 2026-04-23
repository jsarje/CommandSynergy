using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using static Microsoft.Playwright.Assertions;

namespace CommandSynergy.Playwright.Tests.Tests;

public sealed class HomePageTests : PageTest, IClassFixture<PlaywrightWebApplicationFactory>
{
    private readonly PlaywrightWebApplicationFactory factory;

    public HomePageTests(PlaywrightWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        IgnoreHTTPSErrors = true,
    };

    [Fact]
    public async Task Home_page_loads_the_workspace_shell()
    {
        await Page.GotoAsync(factory.RootUri.ToString());

        await Expect(Page.GetByRole(AriaRole.Banner)).ToContainTextAsync("Synergy Sphere");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Find cards" })).ToHaveTextAsync("Find cards");
        await Expect(Page.GetByPlaceholder("Search commanders, staples, or role players"))
            .ToHaveValueAsync(string.Empty);
        await Expect(Page.GetByTestId("search-results"))
            .ToContainTextAsync("Search for a commander or a card name to start building the list.");
    }

    [Fact]
    public async Task Home_page_opens_the_deck_library_drawer()
    {
        await Page.GotoAsync(factory.RootUri.ToString());
        await Page.GetByRole(AriaRole.Button, new() { Name = "Open Deck Library" }).ClickAsync();

        var drawer = Page.GetByRole(AriaRole.Dialog, new() { Name = "Deck library, import, and export" });

        await Expect(drawer).ToContainTextAsync("Saved working copies");
        await Expect(drawer).ToContainTextAsync("No saved decks yet.");
        await Expect(drawer.GetByRole(AriaRole.Button, new() { Name = "Close menu" })).ToHaveTextAsync("Close");
    }
}
