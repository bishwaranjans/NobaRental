using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RedisRateLimiting;
using StackExchange.Redis;
using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace NobaRental.Backend.WebApi.Startups;

public static class RateLimitingStartup
{
    public static IServiceCollection ConfigureRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = OnRateLimitRejected;
            options.GlobalLimiter = CreateGlobalLimiter();
        });

        return services;
    }

    private static PartitionedRateLimiter<HttpContext> CreateGlobalLimiter() =>
        PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            var redis = httpContext.RequestServices.GetService<IConnectionMultiplexer>();
            var clientId = ResolveClientId(httpContext);

            if (!string.IsNullOrWhiteSpace(clientId))
            {
                return CreateClientLimiter($"rl:client:{clientId}", redis);
            }

            var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return CreateAnonymousLimiter($"rl:ip:{remoteIp}", redis);
        });

    private static string? ResolveClientId(HttpContext httpContext) =>
        httpContext.User.FindFirst("client_id")?.Value
        ?? httpContext.User.FindFirst("azp")?.Value
        ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private static RateLimitPartition<string> CreateClientLimiter(string partitionKey, IConnectionMultiplexer? redis)
    {
        if (redis is not null)
        {
            return RedisRateLimitPartition.GetTokenBucketRateLimiter(
                partitionKey,
                _ => new RedisTokenBucketRateLimiterOptions
                {
                    ConnectionMultiplexerFactory = () => redis,
                    TokenLimit = 100,
                    TokensPerPeriod = 100,
                    ReplenishmentPeriod = TimeSpan.FromMinutes(1)
                });
        }

        return RateLimitPartition.GetTokenBucketLimiter(
            partitionKey,
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 100,
                TokensPerPeriod = 100,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    }

    private static RateLimitPartition<string> CreateAnonymousLimiter(string partitionKey, IConnectionMultiplexer? redis)
    {
        if (redis is not null)
        {
            return RedisRateLimitPartition.GetSlidingWindowRateLimiter(
                partitionKey,
                _ => new RedisSlidingWindowRateLimiterOptions
                {
                    ConnectionMultiplexerFactory = () => redis,
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(1)
                });
        }

        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 3,
                QueueLimit = 0
            });
    }

    private static async ValueTask OnRateLimitRejected(OnRejectedContext context, CancellationToken cancellationToken)
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Detail = "API rate limit exceeded. Please retry after the duration specified in the Retry-After header.",
            Instance = context.HttpContext.Request.Path
        };

        await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: cancellationToken);
    }
}
