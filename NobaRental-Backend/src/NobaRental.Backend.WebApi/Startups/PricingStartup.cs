using NobaRental.Backend.Business.Pricing;
using NobaRental.Backend.Business.Pricing.Strategies;
using NobaRental.Backend.Domain.Pricing;

namespace NobaRental.Backend.WebApi.Startups;

public static class PricingStartup
{
    public static IServiceCollection ConfigurePricing(this IServiceCollection services)
    {
        services.AddSingleton<ICarCategoryPricingStrategy, SmallCarPricingStrategy>();
        services.AddSingleton<ICarCategoryPricingStrategy, CombiPricingStrategy>();
        services.AddSingleton<ICarCategoryPricingStrategy, TruckPricingStrategy>();
        services.AddSingleton<IRentalPriceCalculator, RentalPriceCalculator>();

        return services;
    }
}
