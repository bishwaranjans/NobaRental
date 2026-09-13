using Microsoft.EntityFrameworkCore;
using NobaRental.Backend.Business.Api;
using NobaRental.Backend.Business.Pricing;
using NobaRental.Backend.Business.Pricing.Strategies;
using NobaRental.Backend.Data;
using NobaRental.Backend.Data.Entities;
using NobaRental.Backend.Domain.Exceptions;
using NobaRental.Backend.Domain.Values;

namespace NobaRental.Backend.Business.Test.Api;

public sealed class EndToEndLifecycleTests : IDisposable
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private readonly NobaRentalDbContext _ctx;
    private readonly StationApi _stationApi;
    private readonly CarFleetApi _carApi;
    private readonly RentalBookingApi _rentalApi;

    public EndToEndLifecycleTests()
    {
        var options = new DbContextOptionsBuilder<NobaRentalDbContext>()
            .UseInMemoryDatabase(databaseName: $"NobaRental_E2E_{Guid.NewGuid()}")
            .Options;

        _ctx = new NobaRentalDbContext(options);

        // Seed initial primary stations
        _ctx.Stations.AddRange(
            new StationEntity { Code = "OSL", Name = "Oslo Airport Gardermoen", City = "Oslo", IsActive = true },
            new StationEntity { Code = "BGO", Name = "Bergen Airport Flesland", City = "Bergen", IsActive = true });
        _ctx.SaveChanges();

        _stationApi = new StationApi(_ctx);
        _carApi = new CarFleetApi(_ctx);
        var calculator = new RentalPriceCalculator([
            new SmallCarPricingStrategy(),
            new CombiPricingStrategy(),
            new TruckPricingStrategy()
        ]);
        _rentalApi = new RentalBookingApi(_ctx, calculator);
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Fact]
    public async Task Station_CompleteLifecycle_CreateUpdateFilterAndDuplicateValidation()
    {
        // 1. Create Station
        var tos = await _stationApi.CreateStation("TOS", "Tromsø Airport", "Tromsø", Token);
        Assert.Equal("TOS", tos.Code);
        Assert.True(tos.IsActive);

        // 2. Read Station
        var fetched = await _stationApi.GetStationByCode("TOS", Token);
        Assert.NotNull(fetched);
        Assert.Equal("Tromsø", fetched.City);

        // 3. Deactivate & Verify Filter
        await _stationApi.UpdateStation("TOS", "Tromsø Airport", "Tromsø", isActive: false, cancellationToken: Token);
        var activeOnly = await _stationApi.GetAllStations(includeInactive: false, Token);
        Assert.DoesNotContain(activeOnly, s => string.Equals(s.Code, "TOS", StringComparison.OrdinalIgnoreCase));

        var allStations = await _stationApi.GetAllStations(includeInactive: true, Token);
        Assert.Contains(allStations, s => string.Equals(s.Code, "TOS", StringComparison.OrdinalIgnoreCase));

        // 4. Duplicate Code Validation
        await Assert.ThrowsAsync<InvalidRentalOperationException>(() =>
            _stationApi.CreateStation("tos", "Duplicate Tromsø", "Tromsø", Token));
    }

    [Fact]
    public async Task CarFleet_CompleteLifecycle_RegisterInventoryAvailabilityAndSoftDelete()
    {
        // 1. Register vehicle at OSL
        var car = await _carApi.RegisterCar("EV88888", CarCategory.Combi, 12000, "OSL", 600m, 3m, Token);
        Assert.Equal("EV88888", car.RegistrationNumber);
        Assert.Equal(CarStatus.Available, car.Status);
        Assert.Equal("OSL", car.CurrentStationCode);

        // 2. Verify Available at OSL but not BGO
        var availableAtOsl = await _carApi.GetAvailableCars("OSL", cancellationToken: Token);
        Assert.Contains(availableAtOsl, c => string.Equals(c.RegistrationNumber, "EV88888", StringComparison.OrdinalIgnoreCase));

        var availableAtBgo = await _carApi.GetAvailableCars("BGO", cancellationToken: Token);
        Assert.DoesNotContain(availableAtBgo, c => string.Equals(c.RegistrationNumber, "EV88888", StringComparison.OrdinalIgnoreCase));

        // 3. Decommission (Soft-Delete)
        await _carApi.DeleteCar("EV88888", Token);
        var deleted = await _carApi.GetCarByRegistrationNumber("EV88888", Token);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task RentalBooking_CompleteLifecycle_EstimatePickupOneWayReturnAndRelocation()
    {
        // 1. Register car at OSL with 600 NOK/day and 5 NOK/km tariff
        await _carApi.RegisterCar("EV77777", CarCategory.Combi, 10000, "OSL", 600m, 5m, Token);
        var pickupTime = new DateTimeOffset(2026, 9, 10, 10, 0, 0, TimeSpan.Zero);
        var returnTime = pickupTime.AddDays(2);

        // 2. Pickup (rates are resolved authoritatively from EV77777)
        var booking = await _rentalApi.RegisterPickup(
            "EV77777", "12345678901", CarCategory.Combi, "OSL", pickupTime, 10000, cancellationToken: Token);
        Assert.Equal(RentalStatus.Active, booking.Status);

        // 3. Verify Car status is Rented
        var carDuringRental = await _carApi.GetCarByRegistrationNumber("EV77777", Token);
        Assert.NotNull(carDuringRental);
        Assert.Equal(CarStatus.Rented, carDuringRental.Status);

        // 4. Pre-return estimate: (600 * 2 * 1.3) + (5 * 250) = 1560 + 1250 = 2810 NOK
        var estimate = await _rentalApi.EstimatePrice(booking.BookingNumber, returnTime, 10250, Token);
        Assert.Equal(2810m, estimate.EstimatedPrice);
        Assert.Equal(2, estimate.CalculatedDays);
        Assert.Equal(250, estimate.CalculatedKm);

        // 5. Return at BGO (One-Way Relocation)
        var returned = await _rentalApi.RegisterReturn(booking.BookingNumber, "BGO", returnTime, 10250, cancellationToken: Token);
        Assert.Equal(RentalStatus.Completed, returned.Status);
        Assert.Equal(2810m, returned.TotalPrice);
        Assert.Equal("BGO", returned.ReturnStationCode);

        // 6. Verify vehicle relocated to BGO with updated odometer
        var carAfterReturn = await _carApi.GetCarByRegistrationNumber("EV77777", Token);
        Assert.NotNull(carAfterReturn);
        Assert.Equal(CarStatus.Available, carAfterReturn.Status);
        Assert.Equal("BGO", carAfterReturn.CurrentStationCode);
        Assert.Equal(10250, carAfterReturn.CurrentMeterReadingKm);
    }
}
