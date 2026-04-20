using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Application.Abstractions;

public interface IDeckImportService
{
    Task<DeckImportResultContract> ImportAsync(DeckImportRequestContract request, CancellationToken cancellationToken = default);
}