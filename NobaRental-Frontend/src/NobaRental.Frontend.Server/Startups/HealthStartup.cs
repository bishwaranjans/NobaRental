using NobaRental.Backend.WebApi.Client.HealthCheck;

namespace NobaRental.Frontend.Server.Startups;

internal static class HealthStartup
{
    public static void ConfigureHealthChecks(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddNobaRentalBackend();
    }
}