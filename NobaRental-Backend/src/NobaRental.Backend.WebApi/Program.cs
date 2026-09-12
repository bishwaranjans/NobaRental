using FluentValidation;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using NobaRental.Backend.Business;
using NobaRental.Backend.Business.Api;
using NobaRental.Backend.Data;
using NobaRental.Backend.Data.Helpers;
using NobaRental.Backend.Domain;
using NobaRental.Backend.WebApi.Client.Models;
using NobaRental.Backend.WebApi.Settings;
using NobaRental.Backend.WebApi.Startups;
using NobaRental.Shared.ServiceDefaults;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var settings = builder.Configuration.Get<AppSettings>();
ArgumentNullException.ThrowIfNull(settings);

var services = builder.Services;
services.AddSingleton(settings);
services.AddSingleton(TimeProvider.System);
builder.AddServiceDefaults();

services.AddDbContextPool<NobaRentalDbContext>((sp, options) => options
    .UseAzureSql(settings.ConnectionStrings.NobaRental, x => x.WithDefaultOptions()));

services.AddScoped<IRentalBookingApi, RentalBookingApi>();
services.AddScoped<ICarFleetApi, CarFleetApi>();
services.AddScoped<IStationApi, StationApi>();

services.AddScoped<IValidator<NobaRental.Backend.WebApi.Client.Models.Request.RegisterPickupRequest>, NobaRental.Backend.WebApi.Validators.RegisterPickupRequestValidator>();
services.AddScoped<IValidator<NobaRental.Backend.WebApi.Client.Models.Request.RegisterReturnRequest>, NobaRental.Backend.WebApi.Validators.RegisterReturnRequestValidator>();
services.AddScoped<IValidator<NobaRental.Backend.WebApi.Client.Models.Request.RegisterCarRequest>, NobaRental.Backend.WebApi.Validators.RegisterCarRequestValidator>();
services.AddScoped<IValidator<NobaRental.Backend.WebApi.Client.Models.Request.CreateStationRequest>, NobaRental.Backend.WebApi.Validators.CreateStationRequestValidator>();
services.AddScoped<IValidator<NobaRental.Backend.WebApi.Client.Models.Request.UpdateStationRequest>, NobaRental.Backend.WebApi.Validators.UpdateStationRequestValidator>();
services.AddFluentValidationAutoValidation();

services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
services.AddLocalization();
services.AddEndpointsApiExplorer();
services.AddHttpContextAccessor();
services.AddSwaggerGen(options => options.CustomSchemaIds(x => x.ToString()));

services.ConfigureHealthChecks();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
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