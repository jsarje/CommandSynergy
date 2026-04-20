using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Contracts;

namespace CommandSynergy.Application.Decks.Portability;

public sealed class DeckImportService : IDeckImportService
{
    private const int MaxDocumentLength = 64 * 1024;

    private readonly ICardSearchService cardSearchService;
    private readonly DeckFormatDetectionService deckFormatDetectionService;
    private readonly TimeProvider timeProvider;

    public DeckImportService(
        ICardSearchService cardSearchService,
        DeckFormatDetectionService deckFormatDetectionService,
        TimeProvider timeProvider)
    {
        this.cardSearchService = cardSearchService;
        this.deckFormatDetectionService = deckFormatDetectionService;
        this.timeProvider = timeProvider;
    }

    public async Task<DeckImportResultContract> ImportAsync(DeckImportRequestContract request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedDocument = request.RawDocumentText?.Trim() ?? string.Empty;
        var diagnostics = new List<ImportDiagnostic>();

        if (string.IsNullOrWhiteSpace(normalizedDocument))
        {
            diagnostics.Add(CreateError("empty-document", "Paste a decklist before importing.", null, null, "Provide a supported plaintext decklist."));
            return CreateResult(null, false, Array.Empty<string>(), CreatePlaceholderRecord(request, diagnostics), diagnostics);
        }

        if (normalizedDocument.Length > MaxDocumentLength)
        {
            diagnostics.Add(CreateError("payload-too-large", "The imported deck document exceeds the browser-safe size limit.", null, null, "Trim comments or import a smaller plaintext file."));
            return CreateResult(null, false, Array.Empty<string>(), CreatePlaceholderRecord(request, diagnostics), diagnostics);
        }

        var detection = deckFormatDetectionService.Detect(normalizedDocument, request.HintedFormatId);
        var profile = detection.SelectedProfile;
        if (profile is null)
        {
            diagnostics.Add(CreateError("format-not-supported", "The document format is not supported.", null, null, "Choose one of the supported plaintext formats."));
            return CreateResult(null, false, Array.Empty<string>(), CreatePlaceholderRecord(request, diagnostics), diagnostics);
        }

        var parsed = profile.Parse(normalizedDocument);
        diagnostics.AddRange(parsed.Diagnostics);

        var entries = new List<PortableDeckEntry>();
        foreach (var draft in parsed.Entries)
        {
            var resolution = await ResolveCardAsync(draft.DisplayName, cancellationToken).ConfigureAwait(false);
            if (resolution.Match is null)
            {
                diagnostics.Add(new ImportDiagnostic(
                    Guid.NewGuid().ToString("N"),
                    DiagnosticSeverity.Warning,
                    "card-unresolved",
                    $"Could not resolve '{draft.DisplayName}' to a known card.",
                    draft.LineNumber,
                    draft.OriginalLine,
                    "Fix the name or keep the line as unresolved for later review."));
            }

            entries.Add(new PortableDeckEntry(
                resolution.Match?.CardId,
                draft.OriginalLine,
                resolution.Match?.Name ?? draft.DisplayName,
                draft.Quantity,
                draft.SectionId,
                draft.IsCommander,
                draft.IsCompanion,
                resolution.Confidence));
        }

        var sections = parsed.Sections
            .Select(section => new DeckSectionState(
                section.SectionId,
                section.DisplayName,
                section.Role,
                section.SortOrder,
                entries.Where(entry => string.Equals(entry.SectionId, section.SectionId, StringComparison.OrdinalIgnoreCase)).Sum(static entry => entry.Quantity)))
            .ToArray();

        var snapshot = PortableDeckSectionMapper.NormalizeImportedSnapshot(new PortableDeckSnapshot(
            parsed.DeckName ?? GuessDeckName(entries),
            entries.Where(static entry => entry.IsCommander && !string.IsNullOrWhiteSpace(entry.CardId)).Select(static entry => entry.CardId!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            entries.FirstOrDefault(static entry => entry.IsCompanion)?.CardId,
            entries,
            sections,
            entries.Sum(static entry => entry.Quantity),
            entries.Any(static entry => entry.ParseConfidence == ParseConfidence.Unresolved)));

        var importedDeck = new ImportedDeckRecord(
            Guid.NewGuid().ToString("N"),
            snapshot.DeckName,
            profile.FormatId,
            timeProvider.GetUtcNow(),
            null,
            normalizedDocument,
            snapshot,
            diagnostics,
            Array.Empty<string>(),
            new Dictionary<string, string>(parsed.SourceMetadata, StringComparer.OrdinalIgnoreCase));

        return CreateResult(
            profile.FormatId,
            detection.RequiresConfirmation,
            detection.Candidates.Select(static candidate => candidate.Profile.FormatId).ToArray(),
            importedDeck,
            diagnostics);
    }

    private DeckImportResultContract CreateResult(
        string? detectedFormatId,
        bool requiresConfirmation,
        IReadOnlyList<string> candidateFormatIds,
        ImportedDeckRecord importedDeck,
        IReadOnlyList<ImportDiagnostic> diagnostics) => new()
        {
            DetectedFormatId = detectedFormatId,
            RequiresFormatConfirmation = requiresConfirmation,
            CandidateFormatIds = candidateFormatIds,
            ImportedDeck = importedDeck.ToContract(),
            Diagnostics = diagnostics.Select(static diagnostic => diagnostic.ToContract()).ToArray(),
        };

    private ImportedDeckRecord CreatePlaceholderRecord(DeckImportRequestContract request, IReadOnlyList<ImportDiagnostic> diagnostics) => new(
        Guid.NewGuid().ToString("N"),
        request.SourceFileName ?? "Imported deck",
        request.HintedFormatId ?? "generic-plaintext",
        timeProvider.GetUtcNow(),
        null,
        request.RawDocumentText ?? string.Empty,
        new PortableDeckSnapshot(request.SourceFileName ?? "Imported deck", Array.Empty<string>(), null, Array.Empty<PortableDeckEntry>(), Array.Empty<DeckSectionState>(), 0, diagnostics.Count > 0),
        diagnostics,
        Array.Empty<string>(),
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

    private async Task<CardResolutionResult> ResolveCardAsync(string cardName, CancellationToken cancellationToken)
    {
        var response = await cardSearchService.SearchAsync(new CardSearchQueryContract
        {
            Query = cardName,
            Colors = Array.Empty<string>(),
        }, cancellationToken).ConfigureAwait(false);

        var exactMatch = response.Results.FirstOrDefault(result => string.Equals(result.Name, cardName, StringComparison.OrdinalIgnoreCase));
        if (exactMatch is not null)
        {
            return new CardResolutionResult(exactMatch, ParseConfidence.Exact);
        }

        if (response.Results.Count == 1)
        {
            return new CardResolutionResult(response.Results[0], ParseConfidence.Normalized);
        }

        return new CardResolutionResult(null, ParseConfidence.Unresolved);
    }

    private static string GuessDeckName(IReadOnlyList<PortableDeckEntry> entries) =>
        entries.FirstOrDefault(static entry => entry.IsCommander)?.DisplayName
        ?? entries.FirstOrDefault()?.DisplayName
        ?? "Imported deck";

    private static ImportDiagnostic CreateError(string code, string message, int? lineNumber, string? sourceLine, string suggestedAction) => new(
        Guid.NewGuid().ToString("N"),
        DiagnosticSeverity.Error,
        code,
        message,
        lineNumber,
        sourceLine,
        suggestedAction);

    private sealed record CardResolutionResult(CardSearchResultContract? Match, ParseConfidence Confidence);
}