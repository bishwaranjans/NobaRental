using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using NobaRental.Backend.Business.Api;
using NobaRental.Backend.Data;
using NobaRental.Backend.Data.Helpers;
using NobaRental.Backend.Domain;
using NobaRental.Backend.WebApi.Client.Models;
using NobaRental.Backend.WebApi.Settings;
using NobaRental.Backend.WebApi.Startups;
using NobaRental.Shared.ServiceDefaults;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var settings = builder.Configuration.Get<AppSettings>();
ArgumentNullException.ThrowIfNull(settings);

var services = builder.Services;
services.AddSingleton(settings);
services.AddSingleton(TimeProvider.System);
builder.AddServiceDefaults();

if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("redis")))
{
    builder.AddRedisClient("redis");
}

services.AddDbContextPool<NobaRentalDbContext>((sp, options) => options
    .UseAzureSql(settings.ConnectionStrings.NobaRental, x => x.WithDefaultOptions()));

services.AddScoped<IRentalBookingApi, RentalBookingApi>();
services.AddScoped<ICarFleetApi, CarFleetApi>();
services.AddScoped<IStationApi, StationApi>();
services.AddScoped<ICarCategoryApi, CarCategoryApi>();
services.ConfigureValidation();
services.ConfigurePricing();

services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
services.AddLocalization();
services.AddEndpointsApiExplorer();
services.AddProblemDetails();
services.AddExceptionHandler<NobaRental.Backend.WebApi.Middleware.GlobalExceptionHandler>();
services.ConfigureAuthentication(settings.Auth);
services.ConfigureRateLimiting();
services.ConfigureHealthChecks();
services.ConfigureSwagger();

var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseRequestLocalization(x =>
{
    string[] supportedCultures = [CultureConstants.EnglishCulture, CultureConstants.NorwegianCulture];
    x.SetDefaultCulture(supportedCultures[0]);
    x.AddSupportedCultures(supportedCultures);
    x.AddSupportedUICultures(supportedCultures);
});

app.MapControllers();

app.Map("/api/v1/status", () => new StatusResponse(TimeProvider.System.GetUtcNow().UtcDateTime));

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

await app.RunAsync();