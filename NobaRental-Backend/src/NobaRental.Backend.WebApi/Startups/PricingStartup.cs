using NobaRental.Backend.Business.Pricing;
using NobaRental.Backend.Domain.Pricing;

namespace NobaRental.Backend.WebApi.Startups;

public static class PricingStartup
{
    public static IServiceCollection ConfigurePricing(this IServiceCollection services)
    {
        services.AddSingleton<IRentalPriceCalculator, RentalPriceCalculator>();
        return services;
    }
}
