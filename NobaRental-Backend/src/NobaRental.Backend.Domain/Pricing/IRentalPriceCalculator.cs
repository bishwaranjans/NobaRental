namespace NobaRental.Backend.Domain.Pricing;

public interface IRentalPriceCalculator
{
    decimal CalculatePrice(
        decimal dayMultiplier,
        decimal kmMultiplier,
        decimal baseDayRental,
        decimal baseKmPrice,
        int numberOfDays,
        long numberOfKm);
}
