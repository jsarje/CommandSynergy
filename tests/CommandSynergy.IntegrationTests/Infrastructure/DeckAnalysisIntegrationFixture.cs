using System.Text.Json;
using CommandSynergy.Application;
using CommandSynergy.Application.Analysis;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CommandSynergy.IntegrationTests.Infrastructure;

public sealed class DeckAnalysisIntegrationFixture : IAsyncLifetime
{
    private static readonly JsonSerializerOptions deckSnapshotJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions fixtureJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private IHost? host;

    public string ContentRootPath { get; } = AppContext.BaseDirectory;

    public bool LiveDependenciesEnabled =>
        string.Equals(Environment.GetEnvironmentVariable("COMMANDSYNERGY_RUN_LIVE_INTEGRATION_TESTS"), "true", StringComparison.OrdinalIgnoreCase);

    public async Task InitializeAsync()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = ContentRootPath,
            DisableDefaults = true,
        });

        builder.Configuration
            .AddJsonFile(Path.Combine(ContentRootPath, "appsettings.json"), optional: false, reloadOnChange: false)
            .AddEnvironmentVariables();

        builder.Services
            .AddLogging()
            .AddCommandSynergyApplication(builder.Configuration)
            .AddCommandSynergyInfrastructure(builder.Configuration);

        host = builder.Build();
        await host.StartAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (host is null)
        {
            return;
        }

        await host.StopAsync().ConfigureAwait(false);
        host.Dispose();
    }

    public AsyncServiceScope CreateScope()
    {
        EnsureInitialized();
        return host!.Services.CreateAsyncScope();
    }

    public DeckSnapshotContract LoadDeckSnapshot(string fixtureName)
    {
        var fixture = LoadFixtureExpectations(fixtureName);
        return fixture.ToDeckSnapshot();
    }

    public DeckAnalysisFixtureContract LoadFixtureExpectations(string fixtureName)
    {
        var fixturePath = Path.Combine(ContentRootPath, "Analysis", "Fixtures", fixtureName);
        using var stream = File.OpenRead(fixturePath);
        var fixture = JsonSerializer.Deserialize<DeckAnalysisFixtureContract>(stream, fixtureJsonOptions);
        return fixture ?? throw new InvalidOperationException($"Deck fixture '{fixtureName}' did not deserialize into {nameof(DeckAnalysisFixtureContract)}.");
    }

    public async Task<DeckAnalysisResponseContract> AnalyzeAsync(string fixtureName, CancellationToken cancellationToken = default)
    {
        var snapshot = LoadDeckSnapshot(fixtureName);
        await using var scope = CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IDeckAnalysisCoreService>();
        return await service.AnalyzeAsync(snapshot, cancellationToken).ConfigureAwait(false);
    }

    private void EnsureInitialized()
    {
        if (host is null)
        {
            throw new InvalidOperationException("The integration fixture has not been initialized.");
        }
    }
}

public sealed record DeckAnalysisFixtureContract
{
    public string? DeckId { get; init; }

    public string? Name { get; init; }

    public string? CommanderCardId { get; init; }

    public string? CompanionCardId { get; init; }

    public required IReadOnlyList<DeckEntryContract> Entries { get; init; }

    public IReadOnlyList<PileDefinitionContract> Piles { get; init; } = Array.Empty<PileDefinitionContract>();

    public int? ExpectedBracketLevel { get; init; }

    public decimal? MinPower { get; init; }

    public decimal? MaxPower { get; init; }

    public decimal? MinSynergy { get; init; }

    public decimal? MaxSynergy { get; init; }

    public string? ExpectedTheme { get; init; }

    public DeckSnapshotContract ToDeckSnapshot() => new()
    {
        DeckId = DeckId,
        Name = Name,
        CommanderCardId = CommanderCardId,
        CompanionCardId = CompanionCardId,
        Entries = Entries,
        Piles = Piles,
    };
}
