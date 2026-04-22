using System.Net.Http.Headers;
using CommandSynergy.Application.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace CommandSynergy.Infrastructure.Edhrec;

/// <summary>
/// Registers outbound EDHREC dependencies.
/// </summary>
public static class EdhrecServiceCollectionExtensions
{
    /// <summary>
    /// Adds the typed EDHREC client with standard resilience policies.
    /// </summary>
    public static IServiceCollection AddEdhrecClient(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration[$"{EdhrecOptions.SectionName}:BaseUrl"] ?? "https://json.edhrec.com/";
        var userAgent = configuration[$"{EdhrecOptions.SectionName}:UserAgent"] ?? "CommandSynergy/0.1";

        services
            .AddHttpClient<EdhrecClient>(client =>
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