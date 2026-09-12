using NobaRental.Backend.Business.Pricing;
using NobaRental.Backend.Domain.Values;

namespace NobaRental.Backend.Business.Test.Pricing;

public class RentalPriceCalculatorTests
{
    [Fact]
    public void Calculate_SmallCar_PriceDependsOnlyOnDays()
    {
        // Arrange
        const decimal baseDayRental = 500m;
        const decimal baseKmPrice = 10m;
        const int numberOfDays = 3;
        const long numberOfKm = 250;

        // Small car formula: baseDayRental * numberOfDays
        const decimal expectedTotal = 1500m; // 500 * 3 = 1500

        // Act
        var result = RentalPriceCalculator.Calculate(
            CarCategory.SmallCar,
            baseDayRental,
            baseKmPrice,
            numberOfDays,
            numberOfKm);

        // Assert
        Assert.Equal(expectedTotal, result.TotalPrice);
        Assert.Equal(expectedTotal, result.DayRentalCost);
        Assert.Equal(0m, result.KmRentalCost);
        Assert.Equal("NOK", result.Currency);
        Assert.Equal(numberOfDays, result.NumberOfDays);
        Assert.Equal(numberOfKm, result.NumberOfKm);
    }

    [Fact]
    public void Calculate_Combi_AppliesCombiMultipliers()
    {
        // Arrange
        const decimal baseDayRental = 600m;
        const decimal baseKmPrice = 5m;
        const int numberOfDays = 2;
        const long numberOfKm = 100;

        // Combi formula: (baseDayRental * numberOfDays * 1.3) + (baseKmPrice * numberOfKm)
        // Day cost: 600 * 2 * 1.3 = 1560
        // Km cost: 5 * 100 = 500
        // Total: 2060
        const decimal expectedDayCost = 1560m;
        const decimal expectedKmCost = 500m;
        const decimal expectedTotal = 2060m;

        // Act
        var result = RentalPriceCalculator.Calculate(
            CarCategory.Combi,
            baseDayRental,
            baseKmPrice,
            numberOfDays,
            numberOfKm);

        // Assert
        Assert.Equal(expectedDayCost, result.DayRentalCost);
        Assert.Equal(expectedKmCost, result.KmRentalCost);
        Assert.Equal(expectedTotal, result.TotalPrice);
        Assert.Equal("NOK", result.Currency);
    }

    [Fact]
    public void Calculate_Truck_AppliesTruckMultipliers()
    {
        // Arrange
        const decimal baseDayRental = 800m;
        const decimal baseKmPrice = 8m;
        const int numberOfDays = 2;
        const long numberOfKm = 100;

        // Truck formula: (baseDayRental * numberOfDays * 1.5) + (baseKmPrice * numberOfKm * 1.5)
        // Day cost: 800 * 2 * 1.5 = 2400
        // Km cost: 8 * 100 * 1.5 = 1200
        // Total: 3600
        const decimal expectedDayCost = 2400m;
        const decimal expectedKmCost = 1200m;
        const decimal expectedTotal = 3600m;

        // Act
        var result = RentalPriceCalculator.Calculate(
            CarCategory.Truck,
            baseDayRental,
            baseKmPrice,
            numberOfDays,
            numberOfKm);

        // Assert
        Assert.Equal(expectedDayCost, result.DayRentalCost);
        Assert.Equal(expectedKmCost, result.KmRentalCost);
        Assert.Equal(expectedTotal, result.TotalPrice);
        Assert.Equal("NOK", result.Currency);
    }

    [Fact]
    public void Calculate_CombiWithZeroKm_ReturnsOnlyDayRentalCost()
    {
        // Arrange
        const decimal baseDayRental = 400m;
        const decimal baseKmPrice = 5m;
        const int numberOfDays = 1;
        const long numberOfKm = 0;

        // 400 * 1 * 1.3 = 520
        const decimal expectedTotal = 520m;

        // Act
        var result = RentalPriceCalculator.Calculate(
            CarCategory.Combi,
            baseDayRental,
            baseKmPrice,
            numberOfDays,
            numberOfKm);

        // Assert
        Assert.Equal(expectedTotal, result.TotalPrice);
        Assert.Equal(0m, result.KmRentalCost);
    }

    [Fact]
    public void Calculate_NegativeDays_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RentalPriceCalculator.Calculate(CarCategory.SmallCar, 500m, 10m, -1, 100));
    }

    [Fact]
    public void Calculate_ZeroDays_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RentalPriceCalculator.Calculate(CarCategory.SmallCar, 500m, 10m, 0, 100));
    }

    [Fact]
    public void Calculate_NegativeBaseDayRental_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RentalPriceCalculator.Calculate(CarCategory.SmallCar, -100m, 10m, 1, 100));
    }
}
