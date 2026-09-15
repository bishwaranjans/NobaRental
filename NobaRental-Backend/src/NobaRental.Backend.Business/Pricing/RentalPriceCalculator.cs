using NobaRental.Backend.Domain.Pricing;

namespace NobaRental.Backend.Business.Pricing;

/// <summary>
/// Database/Tariff-driven rental price calculator.
/// Computes total price based on category DayMultiplier and KmMultiplier parameters.
/// </summary>
public sealed class RentalPriceCalculator : IRentalPriceCalculator
{
    public decimal CalculatePrice(
        decimal dayMultiplier,
        decimal kmMultiplier,
        decimal baseDayRental,
        decimal baseKmPrice,
        int numberOfDays,
        long numberOfKm)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dayMultiplier);
        ArgumentOutOfRangeException.ThrowIfNegative(kmMultiplier);
        ArgumentOutOfRangeException.ThrowIfNegative(baseDayRental);
        ArgumentOutOfRangeException.ThrowIfNegative(baseKmPrice);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numberOfDays);
        ArgumentOutOfRangeException.ThrowIfNegative(numberOfKm);

        var price = (baseDayRental * numberOfDays * dayMultiplier) + (baseKmPrice * numberOfKm * kmMultiplier);
        return Math.Round(price, 2, MidpointRounding.AwayFromZero);
    }
}
