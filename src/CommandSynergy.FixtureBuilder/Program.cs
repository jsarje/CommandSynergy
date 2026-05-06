using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CommandSynergy.Application;
using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var options = FixtureBuilderOptions.Parse(args);

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["CardMetadata:SnapshotDirectory"] = options.SnapshotDirectory,
    ["CardMetadata:SnapshotFileName"] = options.SnapshotFileName,
    ["CardMetadata:SearchIndexVersion"] = options.SearchIndexVersion,
});

builder.Services
    .AddCommandSynergyApplication(builder.Configuration)
    .AddCommandSynergyInfrastructure(builder.Configuration);

using var host = builder.Build();
using var scope = host.Services.CreateScope();

var cardSearchService = scope.ServiceProvider.GetRequiredService<ICardSearchService>();
var parsedLines = await LoadDeckLinesAsync(options.InputPath).ConfigureAwait(false);
var resolvedEntries = await ResolveDeckEntriesAsync(cardSearchService, parsedLines).ConfigureAwait(false);

ValidateSpecialEntries(resolvedEntries);

var commander = resolvedEntries.SingleOrDefault(static entry => entry.IsCommander);
var companion = resolvedEntries.SingleOrDefault(static entry => entry.IsCompanion);

var snapshot = new DeckSnapshotContract
{
    DeckId = options.DeckId,
    Name = options.DeckName,
    CommanderCardId = commander?.CardId,
    CompanionCardId = companion?.CardId,
    Piles = Array.Empty<PileDefinitionContract>(),
    Entries = resolvedEntries
        .Select(static entry => new DeckEntryContract
        {
            CardId = entry.CardId,
            Quantity = entry.Quantity,
            AssignedPileId = null,
            IsCommander = entry.IsCommander,
            IsCompanion = entry.IsCompanion,
        })
        .ToArray(),
};

var outputDirectory = Path.GetDirectoryName(options.OutputPath);
if (!string.IsNullOrWhiteSpace(outputDirectory))
{
    Directory.CreateDirectory(outputDirectory);
}

await using var outputStream = File.Create(options.OutputPath);
await JsonSerializer.SerializeAsync(
    outputStream,
    snapshot,
    new JsonSerializerOptions
    {
        WriteIndented = true,
    }).ConfigureAwait(false);

Console.WriteLine($"Fixture written to: {options.OutputPath}");

static async Task<IReadOnlyList<ParsedDeckLine>> LoadDeckLinesAsync(string inputPath)
{
    if (!File.Exists(inputPath))
    {
        throw new FileNotFoundException($"Input deck list was not found at '{inputPath}'.", inputPath);
    }

    var lines = await File.ReadAllLinesAsync(inputPath).ConfigureAwait(false);
    var parsedLines = lines
        .Select(ParseLine)
        .Where(static line => line is not null)
        .Cast<ParsedDeckLine>()
        .ToArray();

    if (parsedLines.Length == 0)
    {
        throw new InvalidOperationException("No deck lines were found in the input file.");
    }

    return parsedLines;
}

static ParsedDeckLine? ParseLine(string rawLine)
{
    var line = rawLine.Trim();
    if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
    {
        return null;
    }

    var match = FixtureBuilderOptions.MatchDeckLine(line);
    if (!match.Success)
    {
        throw new InvalidOperationException($"Could not parse line '{line}'. Expected format '1 Card Name' or '1 Card Name (commander)'.");
    }

    var quantity = int.Parse(match.Groups["quantity"].Value, CultureInfo.InvariantCulture);
    var name = match.Groups["name"].Value.Trim();
    var tag = match.Groups["tag"].Value.Trim();

    return new ParsedDeckLine(
        quantity,
        name,
        string.Equals(tag, "commander", StringComparison.OrdinalIgnoreCase),
        string.Equals(tag, "companion", StringComparison.OrdinalIgnoreCase));
}

static async Task<IReadOnlyList<ResolvedDeckEntry>> ResolveDeckEntriesAsync(
    ICardSearchService cardSearchService,
    IReadOnlyList<ParsedDeckLine> parsedLines)
{
    var resolvedEntries = new List<ResolvedDeckEntry>(parsedLines.Count);
    var unresolvedNames = new List<string>();

    foreach (var line in parsedLines)
    {
        var card = await ResolveCardAsync(cardSearchService, line.Name).ConfigureAwait(false);
        if (card is null)
        {
            unresolvedNames.Add(line.Name);
            continue;
        }

        resolvedEntries.Add(new ResolvedDeckEntry(
            card.CardId,
            card.Name,
            line.Quantity,
            line.IsCommander,
            line.IsCompanion));
    }

    if (unresolvedNames.Count > 0)
    {
        var unresolvedMessage = string.Join(Environment.NewLine, unresolvedNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .Select(static name => $"- {name}"));

        throw new InvalidOperationException($"Failed to resolve the following card names:{Environment.NewLine}{unresolvedMessage}");
    }

    return resolvedEntries
        .GroupBy(static entry => entry.CardId, StringComparer.OrdinalIgnoreCase)
        .Select(static group => MergeResolvedEntries(group))
        .OrderBy(static entry => entry.IsCommander ? 0 : entry.IsCompanion ? 1 : 2)
        .ThenBy(static entry => entry.Name, StringComparer.OrdinalIgnoreCase)
        .ToArray();
}

static ResolvedDeckEntry MergeResolvedEntries(IGrouping<string, ResolvedDeckEntry> group)
{
    var entries = group.ToArray();
    var hasCommander = entries.Any(static entry => entry.IsCommander);
    var hasCompanion = entries.Any(static entry => entry.IsCompanion);

    if (hasCommander && entries.Any(static entry => !entry.IsCommander))
    {
        throw new InvalidOperationException($"Card '{entries[0].Name}' appears as both commander and non-commander.");
    }

    if (hasCompanion && entries.Any(static entry => !entry.IsCompanion))
    {
        throw new InvalidOperationException($"Card '{entries[0].Name}' appears as both companion and non-companion.");
    }

    if (hasCommander && hasCompanion)
    {
        throw new InvalidOperationException($"Card '{entries[0].Name}' cannot be both commander and companion.");
    }

    return new ResolvedDeckEntry(
        entries[0].CardId,
        entries[0].Name,
        entries.Sum(static entry => entry.Quantity),
        hasCommander,
        hasCompanion);
}

static async Task<CardSearchResultContract?> ResolveCardAsync(ICardSearchService cardSearchService, string cardName)
{
    var response = await cardSearchService.SearchAsync(new CardSearchQueryContract
    {
        Query = cardName,
        Colors = Array.Empty<string>(),
    }).ConfigureAwait(false);

    var exactMatch = response.Results.FirstOrDefault(result =>
        string.Equals(result.Name, cardName, StringComparison.OrdinalIgnoreCase));

    if (exactMatch is not null)
    {
        return exactMatch;
    }

    return response.Results.Count == 1
        ? response.Results[0]
        : null;
}

static void ValidateSpecialEntries(IReadOnlyList<ResolvedDeckEntry> entries)
{
    if (entries.Count(static entry => entry.IsCommander) > 1)
    {
        throw new InvalidOperationException("Only one commander entry can be generated per fixture.");
    }

    if (entries.Count(static entry => entry.IsCompanion) > 1)
    {
        throw new InvalidOperationException("Only one companion entry can be generated per fixture.");
    }
}

sealed record ParsedDeckLine(int Quantity, string Name, bool IsCommander, bool IsCompanion);

sealed record ResolvedDeckEntry(string CardId, string Name, int Quantity, bool IsCommander, bool IsCompanion);

sealed partial record FixtureBuilderOptions(
    string InputPath,
    string OutputPath,
    string DeckName,
    string DeckId,
    string SnapshotDirectory,
    string SnapshotFileName,
    string SearchIndexVersion)
{
    public static FixtureBuilderOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i++)
        {
            var argument = args[i];
            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Missing value for argument '{argument}'.");
            }

            values[argument[2..]] = args[++i];
        }

        var inputPath = GetRequired(values, "input");
        var snapshotDirectory = GetRequired(values, "snapshot-directory");
        var deckName = values.GetValueOrDefault("deck-name") ?? Path.GetFileNameWithoutExtension(inputPath);
        var deckId = values.GetValueOrDefault("deck-id") ?? Slugify(deckName);
        var outputPath = values.GetValueOrDefault("output") ?? Path.Combine(Environment.CurrentDirectory, $"{deckId}.json");
        var snapshotFileName = values.GetValueOrDefault("snapshot-file-name") ?? "cards.parquet";
        var searchIndexVersion = values.GetValueOrDefault("search-index-version") ?? "fixture-builder";

        return new FixtureBuilderOptions(
            Path.GetFullPath(inputPath),
            Path.GetFullPath(outputPath),
            deckName,
            deckId,
            Path.GetFullPath(snapshotDirectory),
            snapshotFileName,
            searchIndexVersion);
    }

    private static string GetRequired(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"Required argument '--{key}' was not provided.");

    public static Match MatchDeckLine(string line) => DeckLineRegex().Match(line);

    private static string Slugify(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var slug = Regex.Replace(builder.ToString().ToLowerInvariant(), "[^a-z0-9]+", "-");
        slug = Regex.Replace(slug, "-+", "-");
        return slug.Trim('-');
    }


    [GeneratedRegex("^(?<quantity>\\d+)\\s+(?<name>.+?)(?:\\s+\\((?<tag>commander|companion)\\))?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DeckLineRegex();

}