using NobaRental.Backend.Business.Pricing;
using NobaRental.Backend.Business.Pricing.Strategies;
using NobaRental.Backend.Domain.Values;

namespace NobaRental.Backend.Business.Test.Pricing;

public class RentalPriceCalculatorTests
{
    private readonly IRentalPriceCalculator _calculator = new RentalPriceCalculator([
        new SmallCarPricingStrategy(),
        new CombiPricingStrategy(),
        new TruckPricingStrategy()
    ]);

    [Fact]
    public void CalculatePrice_SmallCar_PriceDependsOnlyOnDays()
    {
        // Arrange
        const decimal baseDayRental = 500m;
        const decimal baseKmPrice = 10m;
        const int numberOfDays = 3;
        const long numberOfKm = 250;

        // Small car formula: baseDayRental * numberOfDays
        const decimal expectedTotal = 1500m; // 500 * 3 = 1500

        // Act
        var price = _calculator.CalculatePrice(
            CarCategory.SmallCar,
            baseDayRental,
            baseKmPrice,
            numberOfDays,
            numberOfKm);

        // Assert
        Assert.Equal(expectedTotal, price);
    }

    [Fact]
    public void CalculatePrice_Combi_AppliesCombiFormula()
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
        const decimal expectedTotal = 2060m;

        // Act
        var price = _calculator.CalculatePrice(
            CarCategory.Combi,
            baseDayRental,
            baseKmPrice,
            numberOfDays,
            numberOfKm);

        // Assert
        Assert.Equal(expectedTotal, price);
    }

    [Fact]
    public void CalculatePrice_Truck_AppliesTruckFormula()
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
        const decimal expectedTotal = 3600m;

        // Act
        var price = _calculator.CalculatePrice(
            CarCategory.Truck,
            baseDayRental,
            baseKmPrice,
            numberOfDays,
            numberOfKm);

        // Assert
        Assert.Equal(expectedTotal, price);
    }

    [Fact]
    public void CalculatePrice_CombiWithZeroKm_ReturnsOnlyDayRentalCost()
    {
        // Arrange
        const decimal baseDayRental = 400m;
        const decimal baseKmPrice = 5m;
        const int numberOfDays = 1;
        const long numberOfKm = 0;

        // 400 * 1 * 1.3 = 520
        const decimal expectedTotal = 520m;

        // Act
        var price = _calculator.CalculatePrice(
            CarCategory.Combi,
            baseDayRental,
            baseKmPrice,
            numberOfDays,
            numberOfKm);

        // Assert
        Assert.Equal(expectedTotal, price);
    }

    [Fact]
    public void CalculatePrice_NegativeDays_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.CalculatePrice(CarCategory.SmallCar, 500m, 10m, -1, 100));
    }

    [Fact]
    public void CalculatePrice_ZeroDays_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.CalculatePrice(CarCategory.SmallCar, 500m, 10m, 0, 100));
    }

    [Fact]
    public void CalculatePrice_NegativeBaseDayRental_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.CalculatePrice(CarCategory.SmallCar, -100m, 10m, 1, 100));
    }

    [Fact]
    public void CalculatePrice_NegativeBaseKmPrice_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.CalculatePrice(CarCategory.Combi, 500m, -1m, 1, 100));
    }

    [Fact]
    public void CalculatePrice_NegativeKm_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.CalculatePrice(CarCategory.Combi, 500m, 10m, 1, -10));
    }

    [Fact]
    public void CalculatePrice_UnsupportedCategory_ThrowsInvalidRentalOperationException()
    {
        // Custom calculator without Truck strategy
        var calculator = new RentalPriceCalculator([new SmallCarPricingStrategy()]);

        Assert.Throws<Domain.Exceptions.InvalidRentalOperationException>(() =>
            calculator.CalculatePrice(CarCategory.Truck, 800m, 5m, 2, 50));
    }

    [Fact]
    public void SmallCarPricingStrategy_CalculatesCorrectly()
    {
        var strategy = new SmallCarPricingStrategy();
        Assert.Equal(CarCategory.SmallCar, strategy.Category);
        var price = strategy.CalculatePrice(500m, 10m, 3, 200);
        Assert.Equal(1500m, price);
    }

    [Fact]
    public void CombiPricingStrategy_CalculatesCorrectly()
    {
        var strategy = new CombiPricingStrategy();
        Assert.Equal(CarCategory.Combi, strategy.Category);
        var price = strategy.CalculatePrice(600m, 5m, 2, 100);
        Assert.Equal(2060m, price);
    }

    [Fact]
    public void TruckPricingStrategy_CalculatesCorrectly()
    {
        var strategy = new TruckPricingStrategy();
        Assert.Equal(CarCategory.Truck, strategy.Category);
        var price = strategy.CalculatePrice(800m, 8m, 2, 100);
        Assert.Equal(3600m, price);
    }

    [Fact]
    public void RentalPriceCalculator_Extensibility_AllowsNewCategoryStrategyWithoutModifyingCalculator()
    {
        // Demonstrate Open/Closed Principle: A new strategy is plugged in without modifying RentalPriceCalculator
        var customStrategy = new MockLuxuryPricingStrategy();
        var calculator = new RentalPriceCalculator([
            new SmallCarPricingStrategy(),
            customStrategy
        ]);

        var price = calculator.CalculatePrice((CarCategory)999, 1000m, 20m, 2, 50);

        // Luxury custom formula: (1000 * 2 * 2.0) + (20 * 50 * 2.0) = 4000 + 2000 = 6000
        Assert.Equal(6000m, price);
    }

    private sealed class MockLuxuryPricingStrategy : ICarCategoryPricingStrategy
    {
        public CarCategory Category => (CarCategory)999;

        public decimal CalculatePrice(decimal baseDayRental, decimal baseKmPrice, int numberOfDays, long numberOfKm)
        {
            return (baseDayRental * numberOfDays * 2.0m) + (baseKmPrice * numberOfKm * 2.0m);
        }
    }
}
