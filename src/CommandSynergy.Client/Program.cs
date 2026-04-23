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
builder.Services.AddScoped<ICardSearchIndexClient, CardSearchIndexClient>();
builder.Services.AddScoped<ICardSearchService, BrowserCardSearchService>();
builder.Services.AddScoped<IDeckWorkspaceStateFactory, DeckWorkspaceStateFactory>();
builder.Services.AddScoped<IDeckWorkspaceClient, DeckWorkspaceClient>();
builder.Services.AddScoped<ILocalStorageStringStore, StreamingLocalStorageStringStore>();
builder.Services.AddScoped<IImportedDeckLibrarySerializer, ImportedDeckLibrarySerializer>();
builder.Services.AddScoped<IImportedDeckLibraryStore, ImportedDeckLibraryStore>();
builder.Services.AddScoped<IImportedDeckLibraryState, ImportedDeckLibraryState>();

await builder.Build().RunAsync();
