using NobaRental.Backend.Business.Pricing;
using NobaRental.Backend.Domain.Exceptions;

namespace NobaRental.Backend.Business.Test.Pricing;

public class RentalDurationCalculatorTests
{
    [Fact]
    public void CalculateBilledDays_DurationLessThan24Hours_ReturnsOneDayMinimum()
    {
        // Arrange
        var pickup = new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);
        var @return = pickup.AddHours(4); // 4 hours

        // Act
        var billedDays = RentalDurationCalculator.CalculateBilledDays(pickup, @return);

        // Assert
        Assert.Equal(1, billedDays);
    }

    [Fact]
    public void CalculateBilledDays_DurationExactly24Hours_ReturnsOneDay()
    {
        // Arrange
        var pickup = new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);
        var @return = pickup.AddHours(24);

        // Act
        var billedDays = RentalDurationCalculator.CalculateBilledDays(pickup, @return);

        // Assert
        Assert.Equal(1, billedDays);
    }

    [Fact]
    public void CalculateBilledDays_DurationOver24Hours_RoundsUpToTwoDays()
    {
        // Arrange
        var pickup = new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);
        var @return = pickup.AddHours(24).AddMinutes(1); // 24h 1m

        // Act
        var billedDays = RentalDurationCalculator.CalculateBilledDays(pickup, @return);

        // Assert
        Assert.Equal(2, billedDays);
    }

    [Fact]
    public void CalculateBilledDays_ReturnBeforePickup_ThrowsRentalValidationException()
    {
        // Arrange
        var pickup = new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);
        var @return = pickup.AddHours(-1);

        // Act & Assert
        Assert.Throws<RentalValidationException>(() =>
            RentalDurationCalculator.CalculateBilledDays(pickup, @return));
    }

    [Fact]
    public void CalculateKilometers_ReturnMeterGreaterThanPickup_ReturnsDifference()
    {
        // Arrange
        const long pickupMeter = 10000;
        const long returnMeter = 10350;
        const long expectedKm = 350;

        // Act
        var km = RentalDurationCalculator.CalculateKilometers(pickupMeter, returnMeter);

        // Assert
        Assert.Equal(expectedKm, km);
    }

    [Fact]
    public void CalculateKilometers_ReturnMeterLowerThanPickup_ThrowsRentalValidationException()
    {
        // Arrange
        const long pickupMeter = 10000;
        const long returnMeter = 9950;

        // Act & Assert
        Assert.Throws<RentalValidationException>(() =>
            RentalDurationCalculator.CalculateKilometers(pickupMeter, returnMeter));
    }
}
