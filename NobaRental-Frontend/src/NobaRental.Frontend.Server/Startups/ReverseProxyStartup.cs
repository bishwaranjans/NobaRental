using Microsoft.AspNetCore.Antiforgery;
using NobaRental.Frontend.Server.Services;
using System.Net.Http.Headers;
using Yarp.ReverseProxy.Transforms;

namespace NobaRental.Frontend.Server.Startups;

internal static class ReverseProxyStartup
{
    public static void ConfigureReverseProxy(this IServiceCollection services, WebApplicationBuilder builder)
    {
        services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetRequiredSection("ReverseProxy"))
            .AddTransforms(builderContext =>
            {
                builderContext.AddRequestTransform(async requestContext =>
                {
                    var tokenProvider = requestContext.HttpContext.RequestServices.GetRequiredService<ITokenProvider>();
                    var token = await tokenProvider.GetAccessToken(requestContext.CancellationToken);
                    if (!string.IsNullOrEmpty(token))
                    {
                        requestContext.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }
                });
            })
            .AddServiceDiscoveryDestinationResolver();
    }

    public static void UseReverseProxy(this WebApplication app)
    {
        app.MapReverseProxy(proxyPipeline => proxyPipeline.Use(ValidateCsrf));
    }

    private static async Task ValidateCsrf(HttpContext context, RequestDelegate next)
    {
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
            if (!await antiforgery.IsRequestValidAsync(context))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Antiforgery token invalid", context.RequestAborted);
                return;
            }
        }

        await next(context);
    }
}