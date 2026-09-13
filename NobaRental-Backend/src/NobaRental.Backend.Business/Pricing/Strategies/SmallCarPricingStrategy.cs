using NobaRental.Backend.Domain.Values;

namespace NobaRental.Backend.Business.Pricing.Strategies;

/// <summary>
/// Pricing strategy for Small Cars: Cost depends solely on day rate (km driven is free/included).
/// Formula: baseDayRental * numberOfDays.
/// </summary>
public sealed class SmallCarPricingStrategy : ICarCategoryPricingStrategy
{
    public CarCategory Category => CarCategory.SmallCar;

    public decimal CalculatePrice(decimal baseDayRental, decimal baseKmPrice, int numberOfDays, long numberOfKm)
    {
        return baseDayRental * numberOfDays;
    }
}
