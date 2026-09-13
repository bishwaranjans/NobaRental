using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using MudBlazor.Services;
using NobaRental.Frontend.Server.Components;
using NobaRental.Frontend.Server.Settings;
using NobaRental.Frontend.Server.Startups;
using NobaRental.Shared.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var settings = builder.Configuration.Get<AppSettings>() ?? new AppSettings();
builder.Services.AddSingleton(settings);
builder.Services.AddSingleton(TimeProvider.System);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddMudServices();
builder.Services.AddHybridCache();
builder.Services.ConfigureReverseProxy(builder);
builder.Services.ConfigureHealthChecks();
builder.Services.ConfigureApiClients(settings);

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
app.UseStaticFiles();

app.UseAntiforgery();

app.MapStaticAssets();
app.UseReverseProxy();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(NobaRental.Frontend.Client._Imports).Assembly);

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseRequestLocalization(x =>
{
    string[] supportedCultures = [CultureConstants.NorwegianCulture, CultureConstants.EnglishCulture];
    x.SetDefaultCulture(supportedCultures[0]);
    x.AddSupportedCultures(supportedCultures);
    x.AddSupportedUICultures(supportedCultures);
});

await app.RunAsync();
