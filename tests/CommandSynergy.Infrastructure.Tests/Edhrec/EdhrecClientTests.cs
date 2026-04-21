using System.Net;
using System.Text;
using CommandSynergy.Application.Configuration;
using CommandSynergy.Domain.Cards;
using CommandSynergy.Infrastructure.Edhrec;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CommandSynergy.Infrastructure.Tests.Edhrec;

public sealed class EdhrecClientTests
{
    [Fact]
    public async Task GetCommanderThemeInsightsAsync_returns_synergy_payload_for_valid_commander()
    {
        var sut = CreateClient(new StubHttpMessageHandler());

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

    private static EdhrecClient CreateClient(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("https://json.edhrec.com/", UriKind.Absolute),
            },
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new EdhrecOptions()),
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

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
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
                    """, Encoding.UTF8, "application/json"),
            });
    }

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