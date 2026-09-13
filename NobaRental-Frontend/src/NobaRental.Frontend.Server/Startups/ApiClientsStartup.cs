using NobaRental.Backend.WebApi.Client;
using NobaRental.Frontend.Server.Auth;
using NobaRental.Frontend.Server.Services;
using NobaRental.Frontend.Server.Settings;

namespace NobaRental.Frontend.Server.Startups;

internal static class ApiClientsStartup
{
    public static void ConfigureApiClients(this IServiceCollection services, AppSettings settings)
    {
        services.AddHttpContextAccessor();

        var authority = settings.Auth0.Authority;
        if (!string.IsNullOrWhiteSpace(authority))
        {
            var authorityUri = authority.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? new Uri(authority)
                : new Uri($"https://{authority.TrimEnd('/')}/");

            services.AddHttpClient<ITokenProvider, TokenProvider>(client =>
            {
                client.BaseAddress = authorityUri;
            });
        }
        else
        {
            services.AddHttpClient<ITokenProvider, TokenProvider>();
        }

        services.AddTransient<BearerTokenHandler>();

        services.AddNobaRentalApiClients(settings.NobaRentalBackendApi.BaseUri, builder =>
        {
            builder.AddHttpMessageHandler<BearerTokenHandler>();
        });
    }
}