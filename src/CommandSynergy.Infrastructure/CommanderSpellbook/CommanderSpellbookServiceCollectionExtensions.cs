using System.Net.Http.Headers;
using CommandSynergy.Application.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace CommandSynergy.Infrastructure.CommanderSpellbook;

/// <summary>
/// Registers outbound Commander Spellbook dependencies.
/// </summary>
public static class CommanderSpellbookServiceCollectionExtensions
{
    /// <summary>
    /// Adds the typed Commander Spellbook client with standard resilience policies.
    /// </summary>
    public static IServiceCollection AddCommanderSpellbookClient(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration[$"{CommanderSpellbookOptions.SectionName}:BaseUrl"] ?? "https://backend.commanderspellbook.com/";
        var userAgent = configuration[$"{CommanderSpellbookOptions.SectionName}:UserAgent"] ?? "CommandSynergy/0.1";

        services
            .AddHttpClient<CommanderSpellbookClient>(client =>
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
