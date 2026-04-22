using System.Net;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using CommandSynergy.Application.Configuration;
using CommandSynergy.Infrastructure.CommanderSpellbook;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace CommandSynergy.Infrastructure.Tests.CommanderSpellbook;

public sealed class CommanderSpellbookClientTests
{
    [Fact]
    public async Task FindCombosAsync_returns_included_combos_for_valid_payload()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "https://backend.commanderspellbook.com/find-my-combos?count=false")
            .Respond("application/json", """
                {
                  "results": {
                    "included": [
                      {
                        "uses": [
                          { "card": { "name": "Sol Ring" }, "quantity": 1 },
                          { "card": { "name": "Basalt Monolith" }, "quantity": 1 }
                        ],
                        "produces": [
                          { "feature": { "name": "Infinite Colorless Mana" } }
                        ],
                        "prerequisites": "Basalt Monolith can tap.",
                        "easyPrerequisites": "Basalt Monolith is untapped.",
                        "steps": "Tap and untap repeatedly."
                      }
                    ],
                    "almostIncluded": []
                  }
                }
                """);
        var sut = CreateClient(mockHttp);

        var result = await sut.FindCombosAsync(["Kinnan, Bonder Prodigy"], ["Sol Ring", "Basalt Monolith"]);

        result.IncludedCombos.Should().ContainSingle();
        result.IncludedCombos[0].CardNames.Should().Equal("Sol Ring", "Basalt Monolith");
        result.IncludedCombos[0].Produces.Should().Equal("Infinite Colorless Mana");
        result.IncludedCombos[0].Prerequisites.Should().Be("Basalt Monolith is untapped.");
        result.IncludedCombos[0].Steps.Should().Be("Tap and untap repeatedly.");
    }

    [Fact]
    public async Task FindCombosAsync_sends_json_content_with_content_type_header()
    {
        var handler = new CapturingHttpMessageHandler();
        var sut = CreateClient(handler);

        _ = await sut.FindCombosAsync(["Kinnan, Bonder Prodigy"], ["Sol Ring"]);

        handler.Request.Should().NotBeNull();
        handler.Request!.RequestUri.Should().Be("https://backend.commanderspellbook.com/find-my-combos?count=false");
        handler.Request!.Content.Should().NotBeNull();
        handler.Request.Content!.Headers.ContentType.Should().NotBeNull();
        handler.Request.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var json = await handler.Request.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("commanders")[0].GetProperty("card").GetString().Should().Be("Kinnan, Bonder Prodigy");
        document.RootElement.GetProperty("commanders")[0].GetProperty("quantity").GetInt32().Should().Be(1);
        document.RootElement.GetProperty("main")[0].GetProperty("card").GetString().Should().Be("Sol Ring");
        document.RootElement.GetProperty("main")[0].GetProperty("quantity").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task FindCombosAsync_returns_almost_included_combos_and_missing_one_count()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "https://backend.commanderspellbook.com/find-my-combos?count=false")
            .Respond("application/json", """
                {
                  "results": {
                    "included": [],
                    "almostIncluded": [
                      {
                        "uses": [
                          { "card": { "name": "Dramatic Reversal" }, "quantity": 1 },
                          { "card": { "name": "Isochron Scepter" }, "quantity": 1 }
                        ],
                        "produces": [
                          { "feature": { "name": "Infinite Mana" } }
                        ],
                        "missingCards": [
                          { "card": { "name": "Sol Ring" }, "quantity": 1 }
                        ],
                        "prerequisites": "Nonland mana rocks on battlefield.",
                        "steps": "Imprint and loop."
                      }
                    ]
                  }
                }
                """);
        var sut = CreateClient(mockHttp);

        var result = await sut.FindCombosAsync(["Urza, Lord High Artificer"], ["Isochron Scepter", "Dramatic Reversal"]);

        result.AlmostIncludedCombos.Should().ContainSingle();
        result.AlmostIncludedCombos[0].CardNames.Should().Equal("Dramatic Reversal", "Isochron Scepter");
        result.AlmostIncludedCombos[0].Produces.Should().Equal("Infinite Mana");
        result.MissingOneCount.Should().Be(1);
    }

    [Fact]
    public async Task FindCombosAsync_returns_empty_result_for_empty_payload()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "https://backend.commanderspellbook.com/find-my-combos?count=false")
            .Respond("application/json", """
                {
                  "results": {
                    "included": [],
                    "almostIncluded": []
                  }
                }
                """);
        var sut = CreateClient(mockHttp);

        var result = await sut.FindCombosAsync(["Kinnan, Bonder Prodigy"], ["Sol Ring"]);

        result.IncludedCombos.Should().BeEmpty();
        result.AlmostIncludedCombos.Should().BeEmpty();
        result.MissingOneCount.Should().Be(0);
    }

    [Fact]
    public async Task FindCombosAsync_returns_empty_result_for_invalid_json()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "https://backend.commanderspellbook.com/find-my-combos?count=false")
            .Respond("application/json", "not-json");
        var sut = CreateClient(mockHttp);

        var result = await sut.FindCombosAsync(["Kinnan, Bonder Prodigy"], ["Sol Ring"]);

        result.IncludedCombos.Should().BeEmpty();
        result.AlmostIncludedCombos.Should().BeEmpty();
        result.MissingOneCount.Should().Be(0);
    }

    [Fact]
    public async Task FindCombosAsync_returns_empty_result_for_unsuccessful_http_status()
    {
        var handler = new StatusCodeHttpMessageHandler(HttpStatusCode.BadGateway);
        var sut = CreateClient(handler);

        var result = await sut.FindCombosAsync(["Kinnan, Bonder Prodigy"], ["Sol Ring"]);

        result.IncludedCombos.Should().BeEmpty();
        result.AlmostIncludedCombos.Should().BeEmpty();
        result.MissingOneCount.Should().Be(0);
    }

    [Fact]
    public async Task FindCombosAsync_returns_empty_result_for_timeout()
    {
        var handler = new TimeoutHttpMessageHandler();
        var sut = CreateClient(handler);

        var result = await sut.FindCombosAsync(["Kinnan, Bonder Prodigy"], ["Sol Ring"]);

        result.IncludedCombos.Should().BeEmpty();
        result.AlmostIncludedCombos.Should().BeEmpty();
        result.MissingOneCount.Should().Be(0);
    }

    [Fact]
    public async Task FindCombosAsync_uses_cached_result_for_repeated_requests()
    {
        var handler = new CountingHttpMessageHandler();
        var sut = CreateClient(handler);

        var first = await sut.FindCombosAsync(["Kinnan, Bonder Prodigy"], ["Sol Ring"]);
        var second = await sut.FindCombosAsync(["Kinnan, Bonder Prodigy"], ["Sol Ring"]);

        first.IncludedCombos.Should().BeEmpty();
        second.IncludedCombos.Should().BeEmpty();
        handler.CallCount.Should().Be(1);
    }

    private static CommanderSpellbookClient CreateClient(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("https://backend.commanderspellbook.com/", UriKind.Absolute),
            },
            new TestDistributedCache(),
            Options.Create(new CommanderSpellbookOptions()),
            NullLogger<CommanderSpellbookClient>.Instance);

    private sealed class TestDistributedCache : IDistributedCache
    {
        private readonly ConcurrentDictionary<string, byte[]> cache = new(StringComparer.Ordinal);

        public byte[]? Get(string key) => cache.TryGetValue(key, out var value) ? value.ToArray() : null;

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

        public void Remove(string key) => cache.TryRemove(key, out _);

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => cache[key] = value.ToArray();

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }
    }

    private sealed class StatusCodeHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode));
    }

    private sealed class TimeoutHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(new OperationCanceledException("timeout"));
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"results\":{\"included\":[],\"almostIncluded\":[]}}", Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class CountingHttpMessageHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"results\":{\"included\":[],\"almostIncluded\":[]}}", Encoding.UTF8, "application/json"),
            });
        }
    }
}
