using Microsoft.AspNetCore.Antiforgery;

namespace NobaRental.Frontend.Server.Startups;

internal static class ReverseProxyStartup
{
    public static void ConfigureReverseProxy(this IServiceCollection services, WebApplicationBuilder builder)
    {
        services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetRequiredSection("ReverseProxy"))
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