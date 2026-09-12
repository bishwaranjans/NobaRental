using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;

namespace NobaRental.Backend.WebApi.Client.HealthCheck;

public class NobaRentalHealthCheck(IWeatherForecastClient client) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken token = default)
    {
        using var response = await client.Status(token);

        var statusCode = response.ResponseMessage.StatusCode;
        if (statusCode != HttpStatusCode.OK)
            return HealthCheckResult.Unhealthy("NobaRental backend returned status code " + statusCode);

        return response.GetContent() switch
        {
            null => HealthCheckResult.Unhealthy("NobaRental backend returned a null response"),
            _ => HealthCheckResult.Healthy()
        };
    }
}