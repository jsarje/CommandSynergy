using Blazored.LocalStorage;
using CommandSynergy.Application;
using CommandSynergy.Application.Decks;
using CommandSynergy.Components;
using CommandSynergy.Components.Decks;
using CommandSynergy.Client.Services;
using CommandSynergy.Endpoints;
using CommandSynergy.Infrastructure;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services
    .AddCommandSynergyApplication(builder.Configuration)
    .AddCommandSynergyInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped(static serviceProvider =>
{
    // Use IHttpContextAccessor to derive the base URI so this works for both
    // Blazor Server components and plain HTTP API requests, where NavigationManager
    // (RemoteNavigationManager) may not yet be initialized.
    var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
    var request = httpContextAccessor.HttpContext?.Request;
    var baseUri = request is not null
        ? $"{request.Scheme}://{request.Host}/"
        : "http://localhost/";
    return new HttpClient { BaseAddress = new Uri(baseUri) };
});
builder.Services.AddScoped<DeckWorkspaceStateFactory>();
builder.Services.AddScoped<CardSearchIndexClient>();
builder.Services.AddScoped<DeckWorkspaceClient>();
builder.Services.AddScoped<DeckWorkspaceViewModel>();
builder.Services.AddScoped<ILocalStorageStringStore, StreamingLocalStorageStringStore>();
builder.Services.AddScoped<ImportedDeckLibrarySerializer>();
builder.Services.AddScoped<ImportedDeckLibraryStore>();
builder.Services.AddScoped<ImportedDeckLibraryState>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(CommandSynergy.Client._Imports).Assembly);
app.MapCardSearchEndpoints();
app.MapDeckValidationEndpoints();
app.MapDeckAnalysisEndpoints();

app.Run();

public partial class Program;
