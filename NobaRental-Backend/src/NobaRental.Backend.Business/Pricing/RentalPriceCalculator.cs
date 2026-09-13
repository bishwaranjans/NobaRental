using NobaRental.Backend.Domain.Exceptions;
using NobaRental.Backend.Domain.Values;

namespace NobaRental.Backend.Business.Pricing;

public static class RentalPriceCalculator
{
    public static decimal CalculatePrice(
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

        var price = category switch
        {
            CarCategory.SmallCar => baseDayRental * numberOfDays,
            CarCategory.Combi => (baseDayRental * numberOfDays * 1.3m) + (baseKmPrice * numberOfKm),
            CarCategory.Truck => (baseDayRental * numberOfDays * 1.5m) + (baseKmPrice * numberOfKm * 1.5m),
            _ => throw new InvalidRentalOperationException($"Unsupported car category: '{category}'.")
        };

        return Math.Round(price, 2, MidpointRounding.AwayFromZero);
    }
}
