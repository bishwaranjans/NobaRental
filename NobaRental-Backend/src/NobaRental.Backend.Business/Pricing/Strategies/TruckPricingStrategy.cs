using NobaRental.Backend.Domain.Values;

namespace NobaRental.Backend.Business.Pricing.Strategies;

/// <summary>
/// Pricing strategy for Trucks:
/// Formula: (baseDayRental * numberOfDays * 1.5) + (baseKmPrice * numberOfKm * 1.5).
/// </summary>
public sealed class TruckPricingStrategy : ICarCategoryPricingStrategy
{
    private const decimal TruckMultiplier = 1.5m;

    public CarCategory Category => CarCategory.Truck;

    public decimal CalculatePrice(decimal baseDayRental, decimal baseKmPrice, int numberOfDays, long numberOfKm)
    {
        return (baseDayRental * numberOfDays * TruckMultiplier) + (baseKmPrice * numberOfKm * TruckMultiplier);
    }
}
