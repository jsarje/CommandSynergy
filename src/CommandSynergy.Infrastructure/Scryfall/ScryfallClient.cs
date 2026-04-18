using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace CommandSynergy.Infrastructure.Scryfall;

/// <summary>
/// Wraps outbound Scryfall requests behind a typed client with validated inputs.
/// </summary>
public sealed class ScryfallClient
{
    public const string HttpClientName = "Scryfall";

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

        var response = await httpClient.GetFromJsonAsync<ScryfallSearchResponse>($"cards/search?q={Uri.EscapeDataString(query)}", cancellationToken)
            .ConfigureAwait(false);

        return response ?? ScryfallSearchResponse.Empty;
    }

    /// <summary>
    /// Requests an exact card match from Scryfall by card name.
    /// </summary>
    public async Task<ScryfallCardDocument?> GetNamedCardAsync(string exactName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exactName);

        logger.LogDebug("Loading Scryfall card {CardName}", exactName);

        return await httpClient.GetFromJsonAsync<ScryfallCardDocument>($"cards/named?exact={Uri.EscapeDataString(exactName)}", cancellationToken)
            .ConfigureAwait(false);
    }
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