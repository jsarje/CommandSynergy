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
        var fixturePath = Path.Combine(ContentRootPath, "Analysis", "Fixtures", fixtureName);
        using var stream = File.OpenRead(fixturePath);
        var snapshot = JsonSerializer.Deserialize<DeckSnapshotContract>(stream, deckSnapshotJsonOptions);
        return snapshot ?? throw new InvalidOperationException($"Deck fixture '{fixtureName}' did not deserialize into {nameof(DeckSnapshotContract)}.");
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
