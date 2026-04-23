using CommandSynergy.Application;
using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Configuration;
using CommandSynergy.Infrastructure;
using CommandSynergy.Infrastructure.Analysis;
using CommandSynergy.Infrastructure.CardMetadata;
using CommandSynergy.Infrastructure.Scryfall;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CommandSynergy.Infrastructure.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddCommandSynergyServices_registers_expected_foundation_services()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CardMetadata:SnapshotDirectory"] = "Data/CardMetadata",
                ["CardMetadata:SnapshotFileName"] = "cards.parquet",
                ["CardMetadata:SearchIndexVersion"] = "v-test",
                ["Bracket:MinimumBracketLevel"] = "1",
                ["Bracket:MaximumBracketLevel"] = "5",
                ["Scryfall:BaseUrl"] = "https://api.scryfall.com/",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        services
            .AddCommandSynergyApplication(configuration)
            .AddCommandSynergyInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IScryfallClient>().Should().NotBeNull();
        provider.GetRequiredService<IParquetCardMetadataStore>().Should().NotBeNull();
        provider.GetRequiredService<ICardMetadataBulkImportService>().Should().NotBeNull();
        provider.GetRequiredService<ISearchIndexSnapshotBuilder>().Should().NotBeNull();
        provider.GetRequiredService<IDeckAnalysisCache>().Should().NotBeNull();
        provider.GetRequiredService<IAnalysisTelemetry>().Should().NotBeNull();
        provider.GetRequiredService<IDeckAnalysisService>().Should().NotBeNull();
        provider.GetRequiredService<IHttpClientFactory>().CreateClient().Should().NotBeNull();

        var metadataOptions = provider.GetRequiredService<IOptions<CardMetadataOptions>>().Value;
        metadataOptions.SearchIndexVersion.Should().Be("v-test");
    }
}
