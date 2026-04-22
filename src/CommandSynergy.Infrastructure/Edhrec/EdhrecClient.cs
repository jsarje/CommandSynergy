using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Configuration;
using CommandSynergy.Domain.Cards;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommandSynergy.Infrastructure.Edhrec;

/// <summary>
/// Loads commander-specific EDHREC synergy data with SSRF-safe slug generation.
/// </summary>
public sealed class EdhrecClient : IEdhrecClient
{
    private static readonly Uri CanonicalBaseAddress = new("https://json.edhrec.com/", UriKind.Absolute);
    private static readonly Regex InvalidSlugCharacters = new(@"[^a-z0-9\s-]", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);
    private static readonly Regex MultiDash = new("-{2,}", RegexOptions.Compiled | RegexOptions.NonBacktracking);
    private static readonly Regex SlugAllowlist = new("^[a-z0-9][a-z0-9-]{1,80}[a-z0-9]$", RegexOptions.Compiled | RegexOptions.NonBacktracking);
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    private readonly HttpClient httpClient;
    private readonly IMemoryCache memoryCache;
    private readonly ILogger<EdhrecClient> logger;

    /// <summary>
    /// Creates an EDHREC client.
    /// </summary>
    public EdhrecClient(HttpClient httpClient, IMemoryCache memoryCache, IOptions<EdhrecOptions> options, ILogger<EdhrecClient> logger)
    {
        this.httpClient = httpClient;
        this.memoryCache = memoryCache;
        this.logger = logger;

        this.httpClient.BaseAddress = NormalizeBaseAddress(httpClient.BaseAddress ?? new Uri(options.Value.BaseUrl, UriKind.Absolute));

        if (httpClient.Timeout == System.Threading.Timeout.InfiniteTimeSpan || httpClient.Timeout == TimeSpan.FromSeconds(100))
        {
            httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        if (httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(options.Value.UserAgent);
        }
    }

    /// <inheritdoc />
    public async Task<CommanderThemeInsights> GetCommanderThemeInsightsAsync(CardProfile commanderProfile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(commanderProfile);

        var slug = BuildCommanderSlug(commanderProfile.Name);
        if (!SlugAllowlist.IsMatch(slug))
        {
            logger.LogWarning("Rejected EDHREC slug {Slug} for commander {CommanderName}", slug, commanderProfile.Name);
            return CommanderThemeInsights.Empty(slug);
        }

        if (memoryCache.TryGetValue(slug, out CommanderThemeInsights? cachedInsights) && cachedInsights is not null)
        {
            return cachedInsights;
        }

        try
        {
            var document = await GetCommanderDocumentAsync(slug, cancellationToken).ConfigureAwait(false);
            var synergyByCardId = document?.Container?.JsonDict?.CardLists
                .SelectMany(static list => list.CardViews)
                .Where(static view => !string.IsNullOrWhiteSpace(view.Id))
                .GroupBy(static view => view.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    static group => group.Key,
                    static group => group.Max(static card => card.Synergy),
                    StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            var insights = new CommanderThemeInsights(slug, synergyByCardId.Count > 0, synergyByCardId);
            memoryCache.Set(slug, insights, CacheDuration);
            return insights;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Timed out while loading EDHREC data for {CommanderName}", commanderProfile.Name);
            return CommanderThemeInsights.Empty(slug);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Failed to load EDHREC data for {CommanderName}", commanderProfile.Name);
            return CommanderThemeInsights.Empty(slug);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Received invalid EDHREC JSON for {CommanderName}", commanderProfile.Name);
            return CommanderThemeInsights.Empty(slug);
        }
    }

    internal static string BuildCommanderSlug(string commanderName)
    {
        var lowerInvariant = commanderName.ToLowerInvariant();
        var stripped = InvalidSlugCharacters.Replace(lowerInvariant, string.Empty).Trim();
        var spaced = stripped.Replace(' ', '-');
        return MultiDash.Replace(spaced, "-");
    }

    private async Task<EdhrecCommanderDocument?> GetCommanderDocumentAsync(string slug, CancellationToken cancellationToken)
    {
        HttpStatusCode? lastStatusCode = null;

        foreach (var requestUri in BuildRequestUris(slug))
        {
            using var response = await httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                lastStatusCode = response.StatusCode;
                continue;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EdhrecCommanderDocument>(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        if (lastStatusCode is HttpStatusCode.NotFound)
        {
            return null;
        }

        return null;
    }

    private IEnumerable<Uri> BuildRequestUris(string slug)
    {
        var primaryUri = new Uri(httpClient.BaseAddress ?? CanonicalBaseAddress, $"pages/commanders/{slug}.json");
        yield return primaryUri;

        var canonicalUri = new Uri(CanonicalBaseAddress, $"pages/commanders/{slug}.json");
        if (primaryUri != canonicalUri)
        {
            yield return canonicalUri;
        }
    }

    private static Uri NormalizeBaseAddress(Uri baseAddress)
    {
        var builder = new UriBuilder(baseAddress)
        {
            Path = "/",
            Query = string.Empty,
            Fragment = string.Empty,
        };

        return builder.Uri;
    }
}