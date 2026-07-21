using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Decks.Portability;
using CommandSynergy.Application.Decks.Portability.Formats;
using FluentAssertions;

namespace CommandSynergy.Application.Tests.Decks;

public sealed class DeckFormatDetectionServiceTests
{    

    [Fact]
    public void Detect_requires_confirmation_when_multiple_profiles_are_equally_strong()
    {
        var sut = CreateSut(new TestProfile("one", 3), new TestProfile("two", 3));

        var result = sut.Detect("ambiguous");

        result.RequiresConfirmation.Should().BeTrue();
        result.Candidates.Should().HaveCount(2);
    }

    private static DeckFormatDetectionService CreateSut(params DeckFormatProfileBase[] profiles) =>
        new(new StubRegistry(profiles));

    private sealed class StubRegistry : IDeckFormatRegistry
    {
        private readonly IReadOnlyList<DeckFormatProfileBase> profiles;

        public StubRegistry(IReadOnlyList<DeckFormatProfileBase> profiles)
        {
            this.profiles = profiles;
        }

        public DeckFormatProfileBase? GetById(string formatId) => profiles.FirstOrDefault(profile => profile.FormatId == formatId);

        public IReadOnlyList<DeckFormatProfileBase> GetSupportedProfiles() => profiles;
    }

    private sealed class TestProfile : DeckFormatProfileBase
    {
        private readonly int score;

        public TestProfile(string formatId, int score)
        {
            FormatId = formatId;
            DisplayName = formatId;
            this.score = score;
        }

        public override string FormatId { get; }

        public override string DisplayName { get; }

        public override int Detect(string documentText) => score;

        public override FormatParseResult Parse(string documentText) => new(null, Array.Empty<FormatDeckEntryDraft>(), Array.Empty<DeckSectionDraft>(), Array.Empty<ImportDiagnostic>(), new Dictionary<string, string>());

        public override string Render(PortableDeckSnapshot snapshot, IReadOnlyList<string> warnings) => string.Empty;
    }
}