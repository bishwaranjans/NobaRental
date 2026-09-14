using NobaRental.Backend.Domain.Pricing;
using NobaRental.Backend.Domain.Values;

namespace NobaRental.Backend.Business.Pricing.Strategies;

/// <summary>
/// Pricing strategy for Combi vehicles:
/// Formula: (baseDayRental * numberOfDays * 1.3) + (baseKmPrice * numberOfKm).
/// </summary>
public sealed class CombiPricingStrategy : ICarCategoryPricingStrategy
{
    private const decimal DayRateMultiplier = 1.3m;

    public CarCategory Category => CarCategory.Combi;

    public decimal CalculatePrice(decimal baseDayRental, decimal baseKmPrice, int numberOfDays, long numberOfKm)
    {
        return (baseDayRental * numberOfDays * DayRateMultiplier) + (baseKmPrice * numberOfKm);
    }
}
