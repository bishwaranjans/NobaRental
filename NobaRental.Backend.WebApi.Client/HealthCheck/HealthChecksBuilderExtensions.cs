using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NobaRental.Backend.WebApi.Client.HealthCheck;

public static class HealthChecksBuilderExtensions
{
    public static IHealthChecksBuilder AddNobaRentalBackend(
       this IHealthChecksBuilder builder,
       string name = "api:Noba Rental Backend API",
       IWeatherForecastClient? client = null,
       HealthStatus? failureStatus = null,
       IEnumerable<string>? tags = null)
    {
        return builder.Add(new HealthCheckRegistration(
            name: name,
            factory: sp => new NobaRentalHealthCheck(client ?? sp.GetRequiredService<IWeatherForecastClient>()),
            failureStatus: failureStatus,
            tags: tags));
    }
}
