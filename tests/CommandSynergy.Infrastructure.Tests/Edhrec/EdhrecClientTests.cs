using System.Net;
using System.Text;
using System.Reflection;
using CommandSynergy.Application.Configuration;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Infrastructure.Edhrec;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace CommandSynergy.Infrastructure.Tests.Edhrec;

public sealed class EdhrecClientTests
{
    [Fact]
    public async Task GetCommanderThemeInsightsAsync_returns_synergy_payload_for_valid_commander()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://json.edhrec.com/pages/commanders/krenko-mob-boss.json")
            .Respond("application/json", """
                {
                  "container": {
                    "json_dict": {
                      "cardlists": [
                        {
                          "header": "High Synergy Cards",
                          "tag": "highsynergycards",
                          "cardviews": [
                            {
                              "id": "card-a",
                              "name": "Card A",
                              "sanitized": "card-a",
                              "synergy": 0.72,
                              "inclusion": 100,
                              "num_decks": 100,
                              "potential_decks": 120,
                              "trend_zscore": 0.1
                            }
                          ]
                        }
                      ]
                    }
                  }
                }
                """);
        var sut = CreateClient(mockHttp);

        var result = await sut.GetCommanderThemeInsightsAsync(CreateCommander("Krenko, Mob Boss"));

        result.IsAvailable.Should().BeTrue();
        result.Slug.Should().Be("krenko-mob-boss");
        result.SynergyByCardId.Should().ContainKey("card-a");
    }

    [Fact]
    public async Task GetCommanderThemeInsightsAsync_returns_empty_result_when_slug_is_invalid()
    {
        var handler = new CountingHttpMessageHandler();
        var sut = CreateClient(handler);

        var result = await sut.GetCommanderThemeInsightsAsync(CreateCommander("!!!"));

        result.IsAvailable.Should().BeFalse();
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetCommanderThemeInsightsAsync_uses_cache_for_repeated_commander_requests()
    {
        var mockHttp = new MockHttpMessageHandler();
        var request = mockHttp.When("https://json.edhrec.com/pages/commanders/krenko-mob-boss.json");
        request.Respond("application/json", """
                {
                  "container": {
                    "json_dict": {
                      "cardlists": [
                        {
                          "header": "High Synergy Cards",
                          "tag": "highsynergycards",
                          "cardviews": [
                            {
                              "id": "card-a",
                              "name": "Card A",
                              "sanitized": "card-a",
                              "synergy": 0.72,
                              "inclusion": 100,
                              "num_decks": 100,
                              "potential_decks": 120,
                              "trend_zscore": 0.1
                            }
                          ]
                        }
                      ]
                    }
                  }
                }
                """);
        var sut = CreateClient(mockHttp);

        _ = await sut.GetCommanderThemeInsightsAsync(CreateCommander("Krenko, Mob Boss"));
        _ = await sut.GetCommanderThemeInsightsAsync(CreateCommander("Krenko, Mob Boss"));

        mockHttp.GetMatchCount(request).Should().Be(1);
    }

    [Fact]
    public async Task GetCommanderThemeInsightsAsync_returns_empty_result_for_invalid_json()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://json.edhrec.com/pages/commanders/krenko-mob-boss.json")
            .Respond("application/json", "not-json");
        var sut = CreateClient(mockHttp);

        var result = await sut.GetCommanderThemeInsightsAsync(CreateCommander("Krenko, Mob Boss"));

        result.IsAvailable.Should().BeFalse();
        result.SynergyByCardId.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCommanderThemeInsightsAsync_retries_against_canonical_json_host_after_not_found()
    {
        var mockHttp = new MockHttpMessageHandler();
        var alternateRequest = mockHttp.When("https://edhrec.com/pages/commanders/krenko-mob-boss.json");
        alternateRequest.Respond(HttpStatusCode.NotFound);
        var canonicalRequest = mockHttp.When("https://json.edhrec.com/pages/commanders/krenko-mob-boss.json");
        canonicalRequest.Respond("application/json", """
                {
                  "container": {
                    "json_dict": {
                      "cardlists": [
                        {
                          "header": "High Synergy Cards",
                          "tag": "highsynergycards",
                          "cardviews": [
                            {
                              "id": "card-a",
                              "name": "Card A",
                              "sanitized": "card-a",
                              "synergy": 0.72,
                              "inclusion": 100,
                              "num_decks": 100,
                              "potential_decks": 120,
                              "trend_zscore": 0.1
                            }
                          ]
                        }
                      ]
                    }
                  }
                }
                """);
        var sut = CreateClient(mockHttp, "https://edhrec.com/articles/");

        var result = await sut.GetCommanderThemeInsightsAsync(CreateCommander("Krenko, Mob Boss"));

        result.IsAvailable.Should().BeTrue();
        result.SynergyByCardId.Should().ContainKey("card-a");
        mockHttp.GetMatchCount(alternateRequest).Should().Be(1);
        mockHttp.GetMatchCount(canonicalRequest).Should().Be(1);
    }

    [Fact]
    public void Constructor_normalizes_base_address_to_origin_root()
    {
        var sut = CreateClient(new CountingHttpMessageHandler(), "https://edhrec.com/articles/");

        sut.Should().NotBeNull();
        sut.GetType();

        var httpClientField = typeof(EdhrecClient).GetField("httpClient", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("httpClient field was not found.");

        var httpClient = (HttpClient)(httpClientField.GetValue(sut)
            ?? throw new InvalidOperationException("httpClient field was null."));

        httpClient.BaseAddress.Should().Be(new Uri("https://edhrec.com/"));
    }

    [Theory]
    [InlineData("Atraxa, Praetors' Voice", "atraxa-praetors-voice")]
    [InlineData("The Ur-Dragon", "the-ur-dragon")]
    public void BuildCommanderSlug_normalizes_names(string name, string expectedSlug)
    {
      var slugBuilder = typeof(EdhrecClient).GetMethod("BuildCommanderSlug", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("BuildCommanderSlug was not found.");

      slugBuilder.Invoke(null, [name]).Should().Be(expectedSlug);
    }

    private static EdhrecClient CreateClient(HttpMessageHandler handler, string? baseUrl = null) =>
        new(
            new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl ?? "https://json.edhrec.com/", UriKind.Absolute),
            },
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new EdhrecOptions
            {
                BaseUrl = baseUrl ?? "https://json.edhrec.com/",
            }),
            NullLogger<EdhrecClient>.Instance);

    private static CardProfile CreateCommander(string name) => new()
    {
        CardId = name,
        Name = name,
        OracleId = name + "-oracle",
        ManaValue = 4,
        TypeLine = "Legendary Creature",
        FaceProfiles = [ new CardFaceProfile("0", name, null, "Legendary Creature", null, null, true) ],
    };

    private sealed class CountingHttpMessageHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount += 1;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}