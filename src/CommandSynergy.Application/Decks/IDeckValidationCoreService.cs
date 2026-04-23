using CommandSynergy.Application.Abstractions;

namespace CommandSynergy.Application.Decks;

/// <summary>
/// Represents the core deck-validation implementation before decorators are applied.
/// </summary>
public interface IDeckValidationCoreService : IDeckValidationService;
