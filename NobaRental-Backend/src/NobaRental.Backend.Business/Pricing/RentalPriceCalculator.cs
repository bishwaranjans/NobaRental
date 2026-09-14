using NobaRental.Backend.Domain.Exceptions;
using NobaRental.Backend.Domain.Values;
using System.Collections.Frozen;
using NobaRental.Backend.Domain.Pricing;

namespace NobaRental.Backend.Business.Pricing;

/// <summary>
/// Strategy-driven rental price calculator adhering to Open/Closed Principle (OCP).
/// Category-specific pricing logic is resolved from registered <see cref="ICarCategoryPricingStrategy"/> implementations.
/// </summary>
public sealed class RentalPriceCalculator : IRentalPriceCalculator
{
    private readonly FrozenDictionary<CarCategory, ICarCategoryPricingStrategy> _strategies;

    public RentalPriceCalculator(IEnumerable<ICarCategoryPricingStrategy> strategies)
    {
        ArgumentNullException.ThrowIfNull(strategies);
        _strategies = strategies.ToFrozenDictionary(s => s.Category);
    }

    /// <summary>
    /// Calculates the total rental price by delegating to the resolved category strategy.
    /// </summary>
    public decimal CalculatePrice(
        CarCategory category,
        decimal baseDayRental,
        decimal baseKmPrice,
        int numberOfDays,
        long numberOfKm)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(baseDayRental);
        ArgumentOutOfRangeException.ThrowIfNegative(baseKmPrice);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numberOfDays);
        ArgumentOutOfRangeException.ThrowIfNegative(numberOfKm);

        if (!_strategies.TryGetValue(category, out var strategy))
        {
            throw new InvalidRentalOperationException($"Unsupported car category: '{category}'.");
        }

        var price = strategy.CalculatePrice(baseDayRental, baseKmPrice, numberOfDays, numberOfKm);
        return Math.Round(price, 2, MidpointRounding.AwayFromZero);
    }
}

