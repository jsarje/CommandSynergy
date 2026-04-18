using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Analysis;
using CommandSynergy.Application.Configuration;
using CommandSynergy.Application.Cards;
using CommandSynergy.Application.Decks;
using CommandSynergy.Domain.Analysis;
using CommandSynergy.Domain.Rules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommandSynergy.Application;

/// <summary>
/// Registers application-layer dependencies and options.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds application-layer services and validated options.
    /// </summary>
    public static IServiceCollection AddCommandSynergyApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<CardMetadataOptions>()
            .Bind(configuration.GetSection(CardMetadataOptions.SectionName))
            .ValidateDataAnnotations();

        services
            .AddOptions<BracketOptions>()
            .Bind(configuration.GetSection(BracketOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddSingleton<CommanderRules>();
        services.AddSingleton<BracketEngine>();
        services.AddSingleton<AnalysisExplanationBuilder>();
        services.AddScoped<CardSearchService>();
        services.AddScoped<DeckValidationService>();
        services.AddScoped<BracketCalculationService>();
        services.AddScoped<SynergyScoringService>();
        services.AddScoped<DeckAnalysisService>();

        return services;
    }
}