using NobaRental.Backend.Domain.Exceptions;

namespace NobaRental.Backend.Business.Pricing;

public static class RentalDurationCalculator
{
    public static int CalculateBilledDays(DateTimeOffset pickup, DateTimeOffset @return)
    {
        if (@return < pickup)
            throw new RentalValidationException("Return date and time cannot be earlier than pickup date and time.");

        var elapsedDays = (@return - pickup).TotalDays;
        return Math.Max(1, (int)Math.Ceiling(elapsedDays));
    }

    public static long CalculateKilometers(long pickupMeterKm, long returnMeterKm)
    {
        if (returnMeterKm < pickupMeterKm)
            throw new RentalValidationException($"Return meter reading ({returnMeterKm} km) cannot be lower than pickup meter reading ({pickupMeterKm} km).");

        return returnMeterKm - pickupMeterKm;
    }
}
