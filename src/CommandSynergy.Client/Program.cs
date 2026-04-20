using Blazored.LocalStorage;
using CommandSynergy.Application;
using CommandSynergy.Application.Abstractions;
using CommandSynergy.Application.Decks;
using CommandSynergy.Client.Services;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddCommandSynergyApplication(builder.Configuration);
builder.Services.AddScoped(static serviceProvider => new HttpClient
{
	BaseAddress = new Uri(serviceProvider.GetRequiredService<IWebAssemblyHostEnvironment>().BaseAddress),
});
builder.Services.AddScoped<CardSearchIndexClient>();
builder.Services.AddScoped<ICardSearchService, BrowserCardSearchService>();
builder.Services.AddScoped<DeckWorkspaceStateFactory>();
builder.Services.AddScoped<DeckWorkspaceClient>();
builder.Services.AddScoped<ImportedDeckLibrarySerializer>();
builder.Services.AddScoped<ImportedDeckLibraryStore>();
builder.Services.AddScoped<ImportedDeckLibraryState>();

await builder.Build().RunAsync();
