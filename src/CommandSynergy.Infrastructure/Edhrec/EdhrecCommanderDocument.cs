using System.Text.Json.Serialization;

namespace CommandSynergy.Infrastructure.Edhrec;

/// <summary>
/// Represents the EDHREC commander page JSON document.
/// </summary>
public sealed record EdhrecCommanderDocument
{
    [JsonPropertyName("container")]
    public EdhrecContainer? Container { get; init; }
}

/// <summary>
/// Represents the EDHREC container object.
/// </summary>
public sealed record EdhrecContainer
{
    [JsonPropertyName("json_dict")]
    public EdhrecJsonDict? JsonDict { get; init; }
}

/// <summary>
/// Represents the EDHREC JSON dictionary payload.
/// </summary>
public sealed record EdhrecJsonDict
{
    [JsonPropertyName("cardlists")]
    public IReadOnlyList<EdhrecCardList> CardLists { get; init; } = Array.Empty<EdhrecCardList>();
}

/// <summary>
/// Represents a card list in the EDHREC commander payload.
/// </summary>
public sealed record EdhrecCardList
{
    [JsonPropertyName("header")]
    public string Header { get; init; } = string.Empty;

    [JsonPropertyName("tag")]
    public string Tag { get; init; } = string.Empty;

    [JsonPropertyName("cardviews")]
    public IReadOnlyList<EdhrecCardView> CardViews { get; init; } = Array.Empty<EdhrecCardView>();
}

/// <summary>
/// Represents a single EDHREC card view record.
/// </summary>
public sealed record EdhrecCardView
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("sanitized")]
    public string Sanitized { get; init; } = string.Empty;

    [JsonPropertyName("synergy")]
    public decimal Synergy { get; init; }

    [JsonPropertyName("inclusion")]
    public int Inclusion { get; init; }

    [JsonPropertyName("num_decks")]
    public int NumDecks { get; init; }

    [JsonPropertyName("potential_decks")]
    public int PotentialDecks { get; init; }

    [JsonPropertyName("trend_zscore")]
    public decimal TrendZscore { get; init; }
}