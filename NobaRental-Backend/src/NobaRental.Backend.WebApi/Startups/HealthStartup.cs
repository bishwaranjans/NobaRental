using Microsoft.Extensions.Diagnostics.HealthChecks;
using NobaRental.Backend.Data;

namespace NobaRental.Backend.WebApi.Startups;

internal static class HealthStartup
{
    public static void ConfigureHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<NobaRentalDbContext>("sql:NobaRental", failureStatus: HealthStatus.Degraded);
    }
}