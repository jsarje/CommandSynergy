using CommandSynergy.Application;
using CommandSynergy.Application.Configuration;
using CommandSynergy.Infrastructure;
using CommandSynergy.Infrastructure.CardMetadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddCommandSynergyApplication(builder.Configuration)
    .AddCommandSynergyInfrastructure(builder.Configuration);
builder.Services.PostConfigure<CardMetadataOptions>(options =>
{
    if (!Path.IsPathRooted(options.SnapshotDirectory))
    {
        options.SnapshotDirectory = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, options.SnapshotDirectory));
    }
});

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("CommandSynergy.Ingestion");

try
{
    var importer = host.Services.GetRequiredService<CardMetadataBulkImportService>();
    var result = await importer.ImportOracleCardsAsync().ConfigureAwait(false);

    logger.LogInformation(
        "Imported {CardCount} cards from {SourceUri} at {ImportedAtUtc}",
        result.CardCount,
        result.SourceUri,
        result.ImportedAtUtc);

    return 0;
}
catch (Exception exception)
{
    logger.LogError(exception, "Bulk card metadata import failed");
    return 1;
}