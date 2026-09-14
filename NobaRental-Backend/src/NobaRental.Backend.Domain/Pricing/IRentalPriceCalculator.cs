using NobaRental.Backend.Domain.Values;

namespace NobaRental.Backend.Domain.Pricing;

/// <summary>
/// Service contract for calculating vehicle rental prices based on category strategies.
/// </summary>
public interface IRentalPriceCalculator
{
    decimal CalculatePrice(
        CarCategory category,
        decimal baseDayRental,
        decimal baseKmPrice,
        int numberOfDays,
        long numberOfKm);
}
