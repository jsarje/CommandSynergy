using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace CommandSynergy.PlaywrightTests;

public sealed class WorkspaceBrowserTests
{
    [PlaywrightFact]
    [Trait("Category", "Playwright")]
    public Task HomePage_LoadsInteractiveWorkspace() =>
        WithPageAsync(async page =>
        {
            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Synergy Sphere" })).ToBeVisibleAsync();
            await Expect(page.GetByTestId("workspace-empty")).ToContainTextAsync("Start with a commander");
            await Expect(page.GetByTestId("recommendations-disabled")).ToBeVisibleAsync();
        });

    [PlaywrightFact]
    [Trait("Category", "Playwright")]
    public Task Playtest_OpensAndReturnsToWorkspace() =>
        WithPageAsync(async page =>
        {
            await page.GetByTestId("open-playtest").ClickAsync();
            await Expect(page.GetByTestId("playtest-overlay")).ToBeVisibleAsync();
            await Expect(page.GetByTestId("playtest-empty-state")).ToContainTextAsync("Add cards to the mainboard");

            await page.GetByRole(AriaRole.Button, new() { Name = "Back to deck workspace" }).ClickAsync();
            await Expect(page.GetByTestId("playtest-overlay")).ToHaveCountAsync(0);
        });

    [PlaywrightFact]
    [Trait("Category", "Playwright")]
    public Task DeckLibrary_OpensAndCloses() =>
        WithPageAsync(async page =>
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Open Deck Library" }).ClickAsync();
            await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();
            await Expect(page.GetByTestId("workspace-library-summary")).ToContainTextAsync("Saved decks");
            await Expect(page.GetByTestId("imported-deck-library")).ToContainTextAsync("No saved decks yet");

            await page.GetByRole(AriaRole.Button, new() { Name = "Close menu" }).ClickAsync();
            await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
        });

    private static async Task WithPageAsync(Func<IPage, Task> test)
    {
        var baseUrl = Environment.GetEnvironmentVariable("COMMANDSYNERGY_BASE_URL");
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException("Set COMMANDSYNERGY_BASE_URL to the running CommandSynergy web app URL.");
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUri.ToString());
        await test(page);
    }
}

public sealed class PlaywrightFactAttribute : FactAttribute
{
    public PlaywrightFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COMMANDSYNERGY_BASE_URL")))
        {
            Skip = "Set COMMANDSYNERGY_BASE_URL to a running CommandSynergy app to run browser tests.";
        }
    }
}
