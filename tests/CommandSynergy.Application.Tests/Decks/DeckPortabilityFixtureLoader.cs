namespace CommandSynergy.Application.Tests.Decks;

public static class DeckPortabilityFixtureLoader
{
    public static string Load(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Decks", "Fixtures", fileName);
        return File.ReadAllText(path);
    }
}