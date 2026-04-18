using CommandSynergy.Application;
using CommandSynergy.Application.Decks;
using CommandSynergy.Components;
using CommandSynergy.Components.Decks;
using CommandSynergy.Client.Services;
using CommandSynergy.Endpoints;
using CommandSynergy.Infrastructure;
using Microsoft.AspNetCore.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services
    .AddCommandSynergyApplication(builder.Configuration)
    .AddCommandSynergyInfrastructure(builder.Configuration);
builder.Services.AddMudServices();
builder.Services.AddScoped(static serviceProvider => new HttpClient
{
    BaseAddress = new Uri(serviceProvider.GetRequiredService<NavigationManager>().BaseUri),
});
builder.Services.AddScoped<DeckWorkspaceStateFactory>();
builder.Services.AddScoped<CardSearchIndexClient>();
builder.Services.AddScoped<DeckWorkspaceClient>();
builder.Services.AddScoped<DeckWorkspaceViewModel>();

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
