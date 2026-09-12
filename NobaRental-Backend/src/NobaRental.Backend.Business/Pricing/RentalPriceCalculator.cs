using NobaRental.Backend.Domain.Exceptions;
using NobaRental.Backend.Domain.Models;
using NobaRental.Backend.Domain.Values;

namespace NobaRental.Backend.Business.Pricing;

public static class RentalPriceCalculator
{
    private const decimal CombiDayMultiplier = 1.3m;
    private const decimal CombiKmMultiplier = 1.0m;
    private const decimal TruckDayMultiplier = 1.5m;
    private const decimal TruckKmMultiplier = 1.5m;
    private const string DefaultCurrency = "NOK";

    public static RentalPriceBreakdown Calculate(
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

        decimal dayCost;
        decimal kmCost;

        switch (category)
        {
            case CarCategory.SmallCar:
                // Small car: Price = baseDayRental * numberOfDays
                dayCost = Math.Round(baseDayRental * numberOfDays, 2, MidpointRounding.AwayFromZero);
                kmCost = 0m;
                break;

            case CarCategory.Combi:
                // Combi: Price = baseDayRental * numberOfDays * 1.3 + baseKmPrice * numberOfKm
                dayCost = Math.Round(baseDayRental * numberOfDays * CombiDayMultiplier, 2, MidpointRounding.AwayFromZero);
                kmCost = Math.Round(baseKmPrice * numberOfKm * CombiKmMultiplier, 2, MidpointRounding.AwayFromZero);
                break;

            case CarCategory.Truck:
                // Truck: Price = baseDayRental * numberOfDays * 1.5 + baseKmPrice * numberOfKm * 1.5
                dayCost = Math.Round(baseDayRental * numberOfDays * TruckDayMultiplier, 2, MidpointRounding.AwayFromZero);
                kmCost = Math.Round(baseKmPrice * numberOfKm * TruckKmMultiplier, 2, MidpointRounding.AwayFromZero);
                break;

            default:
                throw new InvalidRentalOperationException($"Unsupported car category: '{category}'.");
        }

        var totalPrice = dayCost + kmCost;

        return new RentalPriceBreakdown(
            BaseDayRental: baseDayRental,
            BaseKmPrice: baseKmPrice,
            NumberOfDays: numberOfDays,
            NumberOfKm: numberOfKm,
            DayRentalCost: dayCost,
            KmRentalCost: kmCost,
            TotalPrice: totalPrice,
            Currency: DefaultCurrency);
    }
}
