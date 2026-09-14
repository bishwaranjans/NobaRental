using NobaRental.Backend.Domain.Values;

namespace NobaRental.Backend.Domain.Pricing;

/// <summary>
/// Strategy contract for calculating rental price for a specific vehicle category.
/// Implementations encapsulate category-specific pricing rules adhering to the Open/Closed Principle.
/// </summary>
public interface ICarCategoryPricingStrategy
{
    CarCategory Category { get; }

    decimal CalculatePrice(decimal baseDayRental, decimal baseKmPrice, int numberOfDays, long numberOfKm);
}
