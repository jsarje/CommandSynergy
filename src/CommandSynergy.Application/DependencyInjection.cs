using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Analysis;
using CommandSynergy.Application.Configuration;
using CommandSynergy.Application.Decks.Portability;
using CommandSynergy.Application.Decks.Portability.Formats;
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

        services
            .AddOptions<EdhrecOptions>()
            .Bind(configuration.GetSection(EdhrecOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddSingleton<CommanderRules>();
        services.AddSingleton<BracketEngine>();
        services.AddSingleton<AnalysisExplanationBuilder>();
        services.AddSingleton<ThemeMatchingService>();
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddScoped<DeckFormatDetectionService>();
        services.AddScoped<IDeckFormatRegistry, DeckFormatRegistry>();
        services.AddScoped<IDeckImportService, DeckImportService>();
        services.AddScoped<IDeckExportService, DeckExportService>();
        services.AddScoped<IWorkingCopyProjectionService, WorkingCopyProjectionService>();
        services.AddScoped<ThemeAnalysisService>();
        services.AddScoped<DeckFormatProfileBase, MoxfieldTextFormatProfile>();
        services.AddScoped<DeckFormatProfileBase, ManaBoxTextFormatProfile>();
        services.AddScoped<DeckFormatProfileBase, GenericPlaintextFormatProfile>();

        return services;
    }
}