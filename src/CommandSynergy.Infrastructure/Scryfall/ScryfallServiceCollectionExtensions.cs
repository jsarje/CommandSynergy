using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using System.Net.Http.Headers;

namespace CommandSynergy.Infrastructure.Scryfall;

/// <summary>
/// Registers outbound Scryfall dependencies.
/// </summary>
public static class ScryfallServiceCollectionExtensions
{
    private const string ScryfallBaseUrlConfigurationKey = "Scryfall:BaseUrl";
    private const string ScryfallUserAgentConfigurationKey = "Scryfall:UserAgent";

    /// <summary>
    /// Adds the typed Scryfall client with standard resilience policies.
    /// </summary>
    public static IServiceCollection AddScryfallClient(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration[ScryfallBaseUrlConfigurationKey] ?? "https://api.scryfall.com/";
        var userAgent = configuration[ScryfallUserAgentConfigurationKey] ?? "CommandSynergy/0.1";

        services
            .AddHttpClient<IScryfallClient, ScryfallClient>(client =>
            {
                client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddStandardResilienceHandler();

        return services;
    }
}
