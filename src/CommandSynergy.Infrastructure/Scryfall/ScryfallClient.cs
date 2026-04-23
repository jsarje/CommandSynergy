using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace CommandSynergy.Infrastructure.Scryfall;

/// <summary>
/// Wraps outbound Scryfall requests behind a typed client with validated inputs.
/// </summary>
public sealed class ScryfallClient : IScryfallClient
{
    public const string HttpClientName = "Scryfall";
    private const int MaxAutocompleteResults = 12;

    private readonly HttpClient httpClient;
    private readonly ILogger<ScryfallClient> logger;

    /// <summary>
    /// Creates a typed client for Scryfall metadata access.
    /// </summary>
    public ScryfallClient(HttpClient httpClient, ILogger<ScryfallClient> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    /// <summary>
    /// Searches Scryfall cards using the supplied query string.
    /// </summary>
    public async Task<ScryfallSearchResponse> SearchCardsAsync(string query, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        logger.LogDebug("Searching Scryfall with query {Query}", query);

        try
        {
            var response = await httpClient.GetFromJsonAsync<ScryfallSearchResponse>($"cards/search?q={Uri.EscapeDataString(query)}", cancellationToken)
                .ConfigureAwait(false);

            return response ?? ScryfallSearchResponse.Empty;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Timed out while searching Scryfall for query {Query}", query);
            return ScryfallSearchResponse.Empty;
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Failed to search Scryfall for query {Query}", query);
            return ScryfallSearchResponse.Empty;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Received invalid JSON while searching Scryfall for query {Query}", query);
            return ScryfallSearchResponse.Empty;
        }
    }

    /// <summary>
    /// Fetches all card ids matching a Scryfall oracle tag, following pagination until exhausted.
    /// </summary>
    public async Task<IReadOnlySet<string>> FetchAllByOracleTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        logger.LogDebug("Fetching all Scryfall cards for oracle tag {Tag}", tag);

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var nextPage = $"cards/search?q={Uri.EscapeDataString($"oracletag:{tag}")}";

        while (!string.IsNullOrWhiteSpace(nextPage))
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<ScryfallSearchResponse>(nextPage, cancellationToken)
                    .ConfigureAwait(false);

                if (response is null)
                {
                    break;
                }

                foreach (var document in response.Data)
                {
                    ids.Add(document.Id);
                }

                nextPage = response.HasMore ? response.NextPage : null;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Timed out while fetching Scryfall oracle tag {Tag}", tag);
                break;
            }
            catch (HttpRequestException exception)
            {
                logger.LogWarning(exception, "Failed to fetch Scryfall oracle tag {Tag}", tag);
                break;
            }
            catch (JsonException exception)
            {
                logger.LogWarning(exception, "Received invalid JSON while fetching Scryfall oracle tag {Tag}", tag);
                break;
            }
        }

        return ids;
    }

    /// <summary>
    /// Loads card-name suggestions for a partial search term.
    /// </summary>
    public async Task<IReadOnlyList<string>> AutocompleteCardNamesAsync(string query, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        logger.LogDebug("Autocompleting Scryfall card names for query {Query}", query);

        try
        {
            var response = await httpClient.GetFromJsonAsync<ScryfallAutocompleteResponse>($"cards/autocomplete?q={Uri.EscapeDataString(query)}", cancellationToken)
                .ConfigureAwait(false);

            return response?.Data.Take(MaxAutocompleteResults).ToArray() ?? Array.Empty<string>();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Timed out while autocompleting Scryfall card names for query {Query}", query);
            return Array.Empty<string>();
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Failed to autocomplete Scryfall card names for query {Query}", query);
            return Array.Empty<string>();
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Received invalid JSON while autocompleting Scryfall card names for query {Query}", query);
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Requests an exact card match from Scryfall by card name.
    /// </summary>
    public async Task<ScryfallCardDocument?> GetNamedCardAsync(string exactName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exactName);

        logger.LogDebug("Loading Scryfall card {CardName}", exactName);

        try
        {
            return await httpClient.GetFromJsonAsync<ScryfallCardDocument>($"cards/named?exact={Uri.EscapeDataString(exactName)}", cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Timed out while loading Scryfall card {CardName}", exactName);
            return null;
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Failed to load Scryfall card {CardName}", exactName);
            return null;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Received invalid JSON while loading Scryfall card {CardName}", exactName);
            return null;
        }
    }

    /// <summary>
    /// Loads a card document by Scryfall card identifier.
    /// </summary>
    public async Task<ScryfallCardDocument?> GetCardByIdAsync(string cardId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cardId);

        logger.LogDebug("Loading Scryfall card by id {CardId}", cardId);

        try
        {
            return await httpClient.GetFromJsonAsync<ScryfallCardDocument>($"cards/{Uri.EscapeDataString(cardId)}", cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Timed out while loading Scryfall card id {CardId}", cardId);
            return null;
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Failed to load Scryfall card id {CardId}", cardId);
            return null;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Received invalid JSON while loading Scryfall card id {CardId}", cardId);
            return null;
        }
    }

    /// <summary>
    /// Loads a card document using a Scryfall id when present, otherwise falls back to exact-name lookup.
    /// </summary>
    public Task<ScryfallCardDocument?> GetCardAsync(string cardIdOrName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cardIdOrName);

        return Guid.TryParse(cardIdOrName, out _)
            ? GetCardByIdAsync(cardIdOrName, cancellationToken)
            : GetNamedCardAsync(cardIdOrName, cancellationToken);
    }

    /// <summary>
    /// Downloads the current oracle-cards bulk dataset from Scryfall.
    /// </summary>
    public async Task<ScryfallBulkDownloadResult?> DownloadOracleCardsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Loading Scryfall bulk-data manifest for oracle_cards");

        try
        {
            var manifest = await httpClient.GetFromJsonAsync<ScryfallBulkDataManifest>("bulk-data", cancellationToken)
                .ConfigureAwait(false);

            var oracleCardsEntry = manifest?.Data.FirstOrDefault(entry =>
                string.Equals(entry.Type, "oracle_cards", StringComparison.OrdinalIgnoreCase));

            if (oracleCardsEntry is null || string.IsNullOrWhiteSpace(oracleCardsEntry.DownloadUri))
            {
                logger.LogWarning("Scryfall bulk-data manifest did not contain an oracle_cards entry");
                return null;
            }

            await using var stream = await httpClient.GetStreamAsync(oracleCardsEntry.DownloadUri, cancellationToken).ConfigureAwait(false);
            var documents = await JsonSerializer.DeserializeAsync<ScryfallCardDocument[]>(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new ScryfallBulkDownloadResult(
                oracleCardsEntry.Type,
                oracleCardsEntry.DownloadUri,
                oracleCardsEntry.UpdatedAt,
                documents ?? Array.Empty<ScryfallCardDocument>());
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Timed out while downloading Scryfall oracle_cards bulk data");
            return null;
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Failed to download Scryfall oracle_cards bulk data");
            return null;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Received invalid JSON while downloading Scryfall oracle_cards bulk data");
            return null;
        }
    }
}

/// <summary>
/// Represents a downloaded Scryfall bulk dataset.
/// </summary>
public sealed record ScryfallBulkDownloadResult(string Type, string DownloadUri, DateTimeOffset? UpdatedAt, IReadOnlyList<ScryfallCardDocument> Cards);

/// <summary>
/// Represents the Scryfall bulk-data manifest.
/// </summary>
public sealed record ScryfallBulkDataManifest
{
    [JsonPropertyName("data")]
    public IReadOnlyList<ScryfallBulkDataItem> Data { get; init; } = Array.Empty<ScryfallBulkDataItem>();
}

/// <summary>
/// Represents a single Scryfall bulk-data manifest entry.
/// </summary>
public sealed record ScryfallBulkDataItem
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("download_uri")]
    public string? DownloadUri { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Represents the subset of Scryfall search metadata required by the application.
/// </summary>
public sealed record ScryfallSearchResponse
{
    public static readonly ScryfallSearchResponse Empty = new()
    {
        Data = Array.Empty<ScryfallCardDocument>(),
    };

    [JsonPropertyName("data")]
    public IReadOnlyList<ScryfallCardDocument> Data { get; init; } = Array.Empty<ScryfallCardDocument>();

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("next_page")]
    public string? NextPage { get; init; }
}

/// <summary>
/// Represents a compact Scryfall card payload used by the metadata adapter.
/// </summary>
public sealed record ScryfallCardDocument
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("oracle_id")]
    public string? OracleId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("oracle_text")]
    public string? OracleText { get; init; }

    [JsonPropertyName("keywords")]
    public IReadOnlyList<string> Keywords { get; init; } = Array.Empty<string>();

    [JsonPropertyName("type_line")]
    public string? TypeLine { get; init; }

    [JsonPropertyName("mana_cost")]
    public string? ManaCost { get; init; }

    [JsonPropertyName("cmc")]
    public decimal ManaValue { get; init; }

    [JsonPropertyName("color_identity")]
    public IReadOnlyList<string> ColorIdentity { get; init; } = Array.Empty<string>();

    [JsonPropertyName("image_uris")]
    public ScryfallImageUris? ImageUris { get; init; }

    [JsonPropertyName("card_faces")]
    public IReadOnlyList<ScryfallCardFaceDocument> CardFaces { get; init; } = Array.Empty<ScryfallCardFaceDocument>();

    [JsonPropertyName("game_changer")]
    public bool IsGameChanger { get; init; }

    [JsonPropertyName("legalities")]
    public ScryfallLegalities? Legalities { get; init; }

    public string? ImageUri => ImageUris?.Normal ?? CardFaces.FirstOrDefault()?.ImageUri;

    public string? CommanderLegality => Legalities?.Commander;
}

/// <summary>
/// Represents Scryfall image URIs.
/// </summary>
public sealed record ScryfallImageUris
{
    [JsonPropertyName("normal")]
    public string? Normal { get; init; }
}

/// <summary>
/// Represents a Scryfall multi-face card-face payload.
/// </summary>
public sealed record ScryfallCardFaceDocument
{
    [JsonPropertyName("face_id")]
    public string? FaceId { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("mana_cost")]
    public string? ManaCost { get; init; }

    [JsonPropertyName("type_line")]
    public string? TypeLine { get; init; }

    [JsonPropertyName("oracle_text")]
    public string? OracleText { get; init; }

    [JsonPropertyName("image_uris")]
    public ScryfallImageUris? ImageUris { get; init; }

    public string? ImageUri => ImageUris?.Normal;
}

/// <summary>
/// Represents Scryfall legality metadata.
/// </summary>
public sealed record ScryfallLegalities
{
    [JsonPropertyName("commander")]
    public string? Commander { get; init; }
}

/// <summary>
/// Represents Scryfall autocomplete suggestions.
/// </summary>
public sealed record ScryfallAutocompleteResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<string> Data { get; init; } = Array.Empty<string>();
}
