using CommandSynergy.Client.Services;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddMudServices();
builder.Services.AddScoped(static serviceProvider => new HttpClient
{
	BaseAddress = new Uri(serviceProvider.GetRequiredService<IWebAssemblyHostEnvironment>().BaseAddress),
});
builder.Services.AddScoped<CardSearchIndexClient>();
builder.Services.AddScoped<DeckWorkspaceClient>();

await builder.Build().RunAsync();
