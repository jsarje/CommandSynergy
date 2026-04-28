using System;
using System.IO;

namespace CommandSynergy.Playwright.Tests;

public static class PlaywrightTestHelpers
{
    public static void SkipIfBrowsersMissing()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var playwrightDir = Path.Combine(localAppData, "ms-playwright", "chromium_headless_shell-1217");

        if (!Directory.Exists(playwrightDir))
        {
            throw Xunit.Sdk.SkipException.ForSkip(
                "Playwright browsers are not installed in this environment. Run `pwsh bin/Debug/netX/playwright.ps1 install` to install browsers, or run tests with browsers available.");
        }
    }
}
