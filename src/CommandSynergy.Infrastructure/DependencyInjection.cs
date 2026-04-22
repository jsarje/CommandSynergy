using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Analysis;
using CommandSynergy.Application.Cards;
using CommandSynergy.Application.Decks;
using CommandSynergy.Infrastructure.Analysis;
using CommandSynergy.Infrastructure.CardMetadata;
using CommandSynergy.Infrastructure.CommanderSpellbook;
using CommandSynergy.Infrastructure.Edhrec;
using CommandSynergy.Infrastructure.Observability;
using CommandSynergy.Infrastructure.Scryfall;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommandSynergy.Infrastructure;

/// <summary>
/// Registers infrastructure-layer integrations and adapters.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure-layer services, typed clients, and metadata adapters.
    /// </summary>
    public static IServiceCollection AddCommandSynergyInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDistributedMemoryCache();
        services.AddScryfallClient(configuration);
        services.AddEdhrecClient(configuration);
        services.AddCommanderSpellbookClient(configuration);
        services.AddSingleton<ParquetCardMetadataStore>();
        services.AddSingleton<SearchIndexSnapshotBuilder>();
        services.AddSingleton<ScryfallCardMapper>();
        services.AddSingleton<CardMetadataBulkImportService>();
        services.AddSingleton<DeckAnalysisCache>();
        services.AddSingleton<AnalysisTelemetry>();
        services.AddScoped<CardSearchService>();
        services.AddScoped<DeckValidationService>();
        services.AddScoped<BracketCalculationService>();
        services.AddScoped<SynergyScoringService>();
        services.AddScoped<DeckAnalysisService>();
        services.AddScoped<ICommanderSpellbookClient, CommanderSpellbookClient>();
        services.AddScoped<IEdhrecClient, EdhrecClient>();
        services.AddScoped<ICardCatalogGateway, CardMetadataQueryService>();
        services.AddScoped<ICardSearchService>(serviceProvider => new CardSearchLoggingDecorator(
            serviceProvider.GetRequiredService<CardSearchService>(),
            serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CardSearchLoggingDecorator>>()));
        services.AddScoped<IDeckValidationService>(serviceProvider => new DeckValidationLoggingDecorator(
            serviceProvider.GetRequiredService<DeckValidationService>(),
            serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DeckValidationLoggingDecorator>>()));
        services.AddScoped<IDeckAnalysisService>(serviceProvider => new CachedDeckAnalysisService(
            serviceProvider.GetRequiredService<DeckAnalysisService>(),
            serviceProvider.GetRequiredService<DeckAnalysisCache>(),
            serviceProvider.GetRequiredService<ICardCatalogGateway>(),
            serviceProvider.GetRequiredService<AnalysisTelemetry>()));

        return services;
    }
}