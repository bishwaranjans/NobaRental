using NobaRental.Backend.Business.Pricing;
using NobaRental.Backend.Domain.Pricing;

namespace NobaRental.Backend.Business.Test.Pricing;

public class RentalPriceCalculatorTests
{
    private readonly IRentalPriceCalculator _calculator = new RentalPriceCalculator();

    [Fact]
    public void CalculatePrice_SmallCar_PriceDependsOnlyOnDays()
    {
        // Small car: DayMultiplier = 1.0, KmMultiplier = 0.0
        const decimal dayMultiplier = 1.0m;
        const decimal kmMultiplier = 0.0m;
        const decimal baseDayRental = 500m;
        const decimal baseKmPrice = 10m;
        const int numberOfDays = 3;
        const long numberOfKm = 250;

        // Small car formula: baseDayRental * numberOfDays = 500 * 3 = 1500
        const decimal expectedTotal = 1500m;

        var price = _calculator.CalculatePrice(
            dayMultiplier,
            kmMultiplier,
            baseDayRental,
            baseKmPrice,
            numberOfDays,
            numberOfKm);

        Assert.Equal(expectedTotal, price);
    }

    [Fact]
    public void CalculatePrice_Combi_AppliesCombiFormula()
    {
        // Combi: DayMultiplier = 1.3, KmMultiplier = 1.0
        const decimal dayMultiplier = 1.3m;
        const decimal kmMultiplier = 1.0m;
        const decimal baseDayRental = 600m;
        const decimal baseKmPrice = 5m;
        const int numberOfDays = 2;
        const long numberOfKm = 100;

        // Combi formula: (baseDayRental * numberOfDays * 1.3) + (baseKmPrice * numberOfKm * 1.0)
        // 600 * 2 * 1.3 = 1560; 5 * 100 = 500 => 2060
        const decimal expectedTotal = 2060m;

        var price = _calculator.CalculatePrice(
            dayMultiplier,
            kmMultiplier,
            baseDayRental,
            baseKmPrice,
            numberOfDays,
            numberOfKm);

        Assert.Equal(expectedTotal, price);
    }

    [Fact]
    public void CalculatePrice_Truck_AppliesTruckFormula()
    {
        // Truck: DayMultiplier = 1.5, KmMultiplier = 1.5
        const decimal dayMultiplier = 1.5m;
        const decimal kmMultiplier = 1.5m;
        const decimal baseDayRental = 800m;
        const decimal baseKmPrice = 8m;
        const int numberOfDays = 2;
        const long numberOfKm = 100;

        // Truck formula: (baseDayRental * numberOfDays * 1.5) + (baseKmPrice * numberOfKm * 1.5)
        // 800 * 2 * 1.5 = 2400; 8 * 100 * 1.5 = 1200 => 3600
        const decimal expectedTotal = 3600m;

        var price = _calculator.CalculatePrice(
            dayMultiplier,
            kmMultiplier,
            baseDayRental,
            baseKmPrice,
            numberOfDays,
            numberOfKm);

        Assert.Equal(expectedTotal, price);
    }

    [Fact]
    public void CalculatePrice_CombiWithZeroKm_ReturnsOnlyDayRentalCost()
    {
        const decimal dayMultiplier = 1.3m;
        const decimal kmMultiplier = 1.0m;
        const decimal baseDayRental = 400m;
        const decimal baseKmPrice = 5m;
        const int numberOfDays = 1;
        const long numberOfKm = 0;

        const decimal expectedTotal = 520m;

        var price = _calculator.CalculatePrice(
            dayMultiplier,
            kmMultiplier,
            baseDayRental,
            baseKmPrice,
            numberOfDays,
            numberOfKm);

        Assert.Equal(expectedTotal, price);
    }

    [Fact]
    public void CalculatePrice_CustomCategory_DynamicallyAppliesCustomMultipliers()
    {
        // Demonstrates extensibility without code changes: e.g. Luxury SUV
        const decimal dayMultiplier = 2.0m;
        const decimal kmMultiplier = 2.5m;
        const decimal baseDayRental = 1000m;
        const decimal baseKmPrice = 10m;
        const int numberOfDays = 3;
        const long numberOfKm = 100;

        // (1000 * 3 * 2.0) + (10 * 100 * 2.5) = 6000 + 2500 = 8500
        const decimal expectedTotal = 8500m;

        var price = _calculator.CalculatePrice(
            dayMultiplier,
            kmMultiplier,
            baseDayRental,
            baseKmPrice,
            numberOfDays,
            numberOfKm);

        Assert.Equal(expectedTotal, price);
    }

    [Fact]
    public void CalculatePrice_NegativeDays_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.CalculatePrice(1.0m, 0.0m, 500m, 10m, -1, 100));
    }

    [Fact]
    public void CalculatePrice_ZeroDays_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.CalculatePrice(1.0m, 0.0m, 500m, 10m, 0, 100));
    }

    [Fact]
    public void CalculatePrice_NegativeBaseDayRental_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.CalculatePrice(1.0m, 0.0m, -100m, 10m, 1, 100));
    }

    [Fact]
    public void CalculatePrice_NegativeBaseKmPrice_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.CalculatePrice(1.3m, 1.0m, 500m, -1m, 1, 100));
    }

    [Fact]
    public void CalculatePrice_NegativeKm_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.CalculatePrice(1.3m, 1.0m, 500m, 10m, 1, -10));
    }

    [Fact]
    public void CalculatePrice_NegativeDayMultiplier_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.CalculatePrice(-1.0m, 1.0m, 500m, 10m, 1, 10));
    }

    [Fact]
    public void CalculatePrice_NegativeKmMultiplier_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.CalculatePrice(1.0m, -1.0m, 500m, 10m, 1, 10));
    }
}
