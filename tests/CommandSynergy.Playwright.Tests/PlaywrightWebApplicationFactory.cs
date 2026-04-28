using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CommandSynergy.Playwright.Tests;

public sealed class PlaywrightWebApplicationFactory : WebApplicationFactory<Program>
{
    private IHost? kestrelHost;
    private Uri? rootUri;

    public Uri RootUri
    {
        get
        {
            if (rootUri is null)
            {
                using var _ = CreateDefaultClient();
            }

            return rootUri
                ?? throw new InvalidOperationException("The Playwright test host has not been started.");
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var testHost = builder.Build();

        builder.ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseKestrel();
            webHostBuilder.UseUrls("https://127.0.0.1:0", "http://127.0.0.1:0");
        });

        kestrelHost = builder.Build();
        kestrelHost.Start();

        var server = kestrelHost.Services.GetRequiredService<IServer>();
        var serverAddresses = server.Features.Get<IServerAddressesFeature>();
        rootUri = serverAddresses?.Addresses
            .Select(static address => new Uri(address))
            .FirstOrDefault(static address => address.Scheme == Uri.UriSchemeHttps);

        ClientOptions.BaseAddress = rootUri
            ?? throw new InvalidOperationException("Unable to determine the Playwright test host address.");

        testHost.Start();
        return testHost;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        kestrelHost?.Dispose();
        kestrelHost = null;
        rootUri = null;
    }
}
