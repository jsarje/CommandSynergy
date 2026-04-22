using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Configuration;
using CommandSynergy.Domain.Analysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommandSynergy.Infrastructure.CommanderSpellbook;

/// <summary>
/// Loads combo-analysis data from Commander Spellbook.
/// </summary>
public sealed class CommanderSpellbookClient : ICommanderSpellbookClient
{
    private readonly HttpClient httpClient;
    private readonly ILogger<CommanderSpellbookClient> logger;

    /// <summary>
    /// Creates a Commander Spellbook client.
    /// </summary>
    public CommanderSpellbookClient(HttpClient httpClient, IOptions<CommanderSpellbookOptions> options, ILogger<CommanderSpellbookClient> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;

        httpClient.BaseAddress = new Uri(options.Value.BaseUrl, UriKind.Absolute);
        httpClient.Timeout = TimeSpan.FromSeconds(10);

        if (httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(options.Value.UserAgent);
        }

        if (!httpClient.DefaultRequestHeaders.Accept.Any(static header => header.MediaType == "application/json"))
        {
            httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }
    }

    /// <inheritdoc />
    public async Task<ComboAnalysis> FindCombosAsync(IEnumerable<string> commanderNames, IEnumerable<string> mainDeckNames, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(commanderNames);
        ArgumentNullException.ThrowIfNull(mainDeckNames);

        var commanderList = NormalizeNames(commanderNames);
        var mainDeckList = NormalizeNames(mainDeckNames);

        if (commanderList.Length == 0 && mainDeckList.Length == 0)
        {
            return ComboAnalysis.Empty();
        }

        var request = new FindMyCombosRequest(
            commanderList.Select(static name => new ComboCardRequest(name, 1)).ToArray(),
            mainDeckList.Select(static name => new ComboCardRequest(name, 1)).ToArray());

        try
        {
            using var response = await httpClient.PostAsJsonAsync("find-my-combos?count=false", request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var document = await response.Content.ReadFromJsonAsync<FindMyCombosResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
            return Map(document);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Timed out while loading combo data from Commander Spellbook.");
            return ComboAnalysis.Empty();
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Failed to load combo data from Commander Spellbook.");
            return ComboAnalysis.Empty();
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Received invalid Commander Spellbook JSON.");
            return ComboAnalysis.Empty();
        }
    }

    private static ComboAnalysis Map(FindMyCombosResponse? response)
    {
        var includedCombos = response?.Results?.Included?.Select(MapCombo).ToArray() ?? [];
        var almostIncludedCombos = response?.Results?.AlmostIncluded?.Select(MapCombo).ToArray() ?? [];
        var missingOneCount = response?.Results?.AlmostIncluded?.Count(static combo => combo.MissingCards?.Length == 1) ?? 0;

        return new ComboAnalysis(includedCombos, almostIncludedCombos, missingOneCount, DateTimeOffset.UtcNow);
    }

    private static ComboResult MapCombo(ComboDocument combo) => new(
        ExtractCardNames(combo),
        ExtractProduces(combo),
        combo.Steps ?? string.Empty,
        FirstNonEmpty(combo.EasyPrerequisites, combo.Prerequisites));

    private static IReadOnlyList<string> ExtractCardNames(ComboDocument combo) => combo.Uses?
        .Select(static use => use.Card?.Name)
        .Where(static name => !string.IsNullOrWhiteSpace(name))
        .Cast<string>()
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray() ?? [];

    private static IReadOnlyList<string> ExtractProduces(ComboDocument combo) => combo.Produces?
        .Select(static produced => produced.Feature?.Name)
        .Where(static name => !string.IsNullOrWhiteSpace(name))
        .Cast<string>()
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray() ?? [];

    private static string FirstNonEmpty(params string?[] values) => values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static string[] NormalizeNames(IEnumerable<string> names) => names
        .Where(static name => !string.IsNullOrWhiteSpace(name))
        .Select(static name => name.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private sealed record FindMyCombosRequest(
        [property: JsonPropertyName("commanders")] IReadOnlyList<ComboCardRequest> Commanders,
        [property: JsonPropertyName("main")] IReadOnlyList<ComboCardRequest> Main);

    private sealed record ComboCardRequest(
        [property: JsonPropertyName("card")] string Card,
        [property: JsonPropertyName("quantity")] int Quantity);

    private sealed record FindMyCombosResponse(
        [property: JsonPropertyName("results")] FindMyCombosResults? Results);

    private sealed record FindMyCombosResults(
        [property: JsonPropertyName("included")] ComboDocument[]? Included,
        [property: JsonPropertyName("almostIncluded")] ComboDocument[]? AlmostIncluded);

    private sealed record ComboDocument(
        [property: JsonPropertyName("uses")] ComboCardUse[]? Uses,
        [property: JsonPropertyName("produces")] ComboProducedFeature[]? Produces,
        [property: JsonPropertyName("prerequisites")] string? Prerequisites,
        [property: JsonPropertyName("easyPrerequisites")] string? EasyPrerequisites,
        [property: JsonPropertyName("steps")] string? Steps,
        [property: JsonPropertyName("missingCards")] ComboCardUse[]? MissingCards);

    private sealed record ComboCardUse(
        [property: JsonPropertyName("card")] ComboCardReference? Card,
        [property: JsonPropertyName("quantity")] int Quantity);

    private sealed record ComboCardReference(
        [property: JsonPropertyName("name")] string? Name);

    private sealed record ComboProducedFeature(
        [property: JsonPropertyName("feature")] ComboFeatureReference? Feature);

    private sealed record ComboFeatureReference(
        [property: JsonPropertyName("name")] string? Name);
}
