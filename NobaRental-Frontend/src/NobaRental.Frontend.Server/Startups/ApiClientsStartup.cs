using NobaRental.Backend.WebApi.Client;
using NobaRental.Frontend.Server.Settings;

namespace NobaRental.Frontend.Server.Startups;

internal static class ApiClientsStartup
{
    public static void ConfigureApiClients(this IServiceCollection services, AppSettings settings)
    {
        services.AddHttpContextAccessor();
        services.AddNobaRentalApiClients(settings.NobaRentalBackendApi.BaseUri);
    }
}