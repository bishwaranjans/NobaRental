using System.Net;
using System.Net.Http.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace NobaRental.Backend.WebApi.Client.Test.Tests;

public sealed class RateLimitingTests : WebApplicationFactory<Program>
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.Configure<RateLimiterOptions>(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: "test-partition",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 2,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
            });
        });
    }

    [Fact]
    public async Task RateLimiter_ExceedingLimit_Returns429TooManyRequests()
    {
        using var client = CreateClient();

        using var res1 = await client.GetAsync("/api/v1/status", Token);
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);

        using var res2 = await client.GetAsync("/api/v1/status", Token);
        Assert.Equal(HttpStatusCode.OK, res2.StatusCode);

        using var res3 = await client.GetAsync("/api/v1/status", Token);
        Assert.Equal((HttpStatusCode)429, res3.StatusCode);

        var problem = await res3.Content.ReadFromJsonAsync<ProblemDetails>(Token);
        Assert.NotNull(problem);
        Assert.Equal(429, problem.Status);
        Assert.Equal("Too Many Requests", problem.Title);
    }
}
