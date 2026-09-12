using Microsoft.EntityFrameworkCore;
using NobaRental.Backend.Business.Api;
using NobaRental.Backend.Data;
using NobaRental.Backend.Data.Entities;
using NobaRental.Backend.Data.Entities.Values;
using NobaRental.Backend.Domain.Exceptions;
using NobaRental.Backend.Domain.Values;

namespace NobaRental.Backend.Business.Test.Api;

public sealed class RentalBookingApiTests : IDisposable
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private readonly NobaRentalDbContext _ctx;
    private readonly RentalBookingApi _api;

    public RentalBookingApiTests()
    {
        var options = new DbContextOptionsBuilder<NobaRentalDbContext>()
            .UseInMemoryDatabase(databaseName: $"NobaRental_{Guid.NewGuid()}")
            .Options;

        _ctx = new NobaRentalDbContext(options);
        _ctx.Stations.AddRange(
            new StationEntity { Code = "OSL", Name = "Oslo Airport", City = "Oslo", IsActive = true },
            new StationEntity { Code = "BGO", Name = "Bergen Airport", City = "Bergen", IsActive = true });
        _ctx.SaveChanges();

        _api = new RentalBookingApi(_ctx);
    }

    private void EnsureCar(string registrationNumber, CarCategory category, long meterReading = 0, string stationCode = "OSL")
    {
        var normalized = registrationNumber.Trim().ToUpperInvariant();
        var existing = _ctx.Cars.Find(normalized);
        if (existing is null)
        {
            _ctx.Cars.Add(new CarEntity
            {
                RegistrationNumber = normalized,
                Category = (CarCategoryValue)category,
                CurrentMeterReadingKm = meterReading,
                CurrentStationCode = stationCode,
                Status = CarStatusValue.Available
            });
            _ctx.SaveChanges();
        }
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Fact]
    public async Task RegisterPickup_Success_PersistsBookingWithActiveStatus()
    {
        EnsureCar("ev12345", CarCategory.SmallCar, 15000);
        var pickupTime = new DateTimeOffset(2026, 9, 12, 9, 0, 0, TimeSpan.Zero);

        var result = await _api.RegisterPickup(
            registrationNumber: "ev12345",
            customerSsn: "12345678901",
            category: CarCategory.SmallCar,
            pickupStationCode: "OSL",
            pickupDateTime: pickupTime,
            pickupMeterReadingKm: 15000,
            baseDayRental: 500m,
            baseKmPrice: 2m,
            cancellationToken: Token);

        Assert.NotNull(result);
        Assert.True(result.BookingNumber > 0);
        Assert.Equal("EV12345", result.RegistrationNumber);
        Assert.Equal("12345678901", result.CustomerSsn);
        Assert.Equal(CarCategory.SmallCar, result.Category);
        Assert.Equal("OSL", result.PickupStationCode);
        Assert.Equal(RentalStatus.Active, result.Status);
        Assert.Equal("NOK", result.Currency);
        Assert.Equal(15000, result.PickupMeterReadingKm);
        Assert.Null(result.ReturnDateTime);
        Assert.Null(result.TotalPrice);

        var entity = await _ctx.RentalBookings.SingleOrDefaultAsync(x => x.BookingNumber == result.BookingNumber, Token);
        Assert.NotNull(entity);
        Assert.Equal(RentalStatusValue.Active, entity.Status);
    }

    [Fact]
    public async Task RegisterPickup_AutoGeneratesUniqueSequentialBookingNumbers()
    {
        EnsureCar("BT10001", CarCategory.SmallCar, 1000);
        EnsureCar("BT20002", CarCategory.Combi, 5000);
        var pickupTime = new DateTimeOffset(2026, 9, 12, 9, 0, 0, TimeSpan.Zero);

        var first = await _api.RegisterPickup(
            registrationNumber: "BT10001",
            customerSsn: "11111111111",
            category: CarCategory.SmallCar,
            pickupStationCode: "OSL",
            pickupDateTime: pickupTime,
            pickupMeterReadingKm: 1000,
            baseDayRental: 400m,
            baseKmPrice: 2m,
            cancellationToken: Token);

        var second = await _api.RegisterPickup(
            registrationNumber: "BT20002",
            customerSsn: "22222222222",
            category: CarCategory.Combi,
            pickupStationCode: "OSL",
            pickupDateTime: pickupTime,
            pickupMeterReadingKm: 5000,
            baseDayRental: 600m,
            baseKmPrice: 3m,
            cancellationToken: Token);

        Assert.True(first.BookingNumber > 0);
        Assert.True(second.BookingNumber > first.BookingNumber);
    }

    [Fact]
    public async Task RegisterPickup_CarAtDifferentStation_ThrowsInvalidRentalOperationException()
    {
        EnsureCar("BGO_CAR", CarCategory.SmallCar, 1000, stationCode: "BGO");
        var pickupTime = new DateTimeOffset(2026, 9, 12, 9, 0, 0, TimeSpan.Zero);

        var ex = await Assert.ThrowsAsync<InvalidRentalOperationException>(() =>
            _api.RegisterPickup(
                registrationNumber: "BGO_CAR",
                customerSsn: "11111111111",
                category: CarCategory.SmallCar,
                pickupStationCode: "OSL",
                pickupDateTime: pickupTime,
                pickupMeterReadingKm: 1000,
                baseDayRental: 500m,
                baseKmPrice: 2m,
                cancellationToken: Token));

        Assert.Contains("stationed at 'BGO', not at pickup station 'OSL'", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterPickup_CarAlreadyHasActiveRental_ThrowsInvalidRentalOperationException()
    {
        EnsureCar("EV99999", CarCategory.SmallCar, 10000);
        var pickupTime = new DateTimeOffset(2026, 9, 12, 9, 0, 0, TimeSpan.Zero);

        await _api.RegisterPickup(
            registrationNumber: "EV99999",
            customerSsn: "11111111111",
            category: CarCategory.SmallCar,
            pickupStationCode: "OSL",
            pickupDateTime: pickupTime,
            pickupMeterReadingKm: 10000,
            baseDayRental: 500m,
            baseKmPrice: 2m,
            cancellationToken: Token);

        var ex = await Assert.ThrowsAsync<InvalidRentalOperationException>(() =>
            _api.RegisterPickup(
                registrationNumber: "EV99999",
                customerSsn: "22222222222",
                category: CarCategory.SmallCar,
                pickupStationCode: "OSL",
                pickupDateTime: pickupTime.AddHours(1),
                pickupMeterReadingKm: 10000,
                baseDayRental: 500m,
                baseKmPrice: 2m,
                cancellationToken: Token));

        Assert.Contains("EV99999", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterPickup_SameCarCanBeRentedAfterPreviousCompleted()
    {
        EnsureCar("EV88888", CarCategory.SmallCar, 10000);
        var pickupTime = new DateTimeOffset(2026, 9, 10, 9, 0, 0, TimeSpan.Zero);
        var returnTime = pickupTime.AddDays(1);

        var firstBooking = await _api.RegisterPickup(
            registrationNumber: "EV88888",
            customerSsn: "11111111111",
            category: CarCategory.SmallCar,
            pickupStationCode: "OSL",
            pickupDateTime: pickupTime,
            pickupMeterReadingKm: 10000,
            baseDayRental: 500m,
            baseKmPrice: 2m,
            cancellationToken: Token);

        await _api.RegisterReturn(
            bookingNumber: firstBooking.BookingNumber,
            returnStationCode: "OSL",
            returnDateTime: returnTime,
            returnMeterReadingKm: 10150,
            cancellationToken: Token);

        var secondBooking = await _api.RegisterPickup(
            registrationNumber: "EV88888",
            customerSsn: "22222222222",
            category: CarCategory.SmallCar,
            pickupStationCode: "OSL",
            pickupDateTime: returnTime.AddHours(2),
            pickupMeterReadingKm: 10150,
            baseDayRental: 500m,
            baseKmPrice: 2m,
            cancellationToken: Token);

        Assert.NotNull(secondBooking);
        Assert.True(secondBooking.BookingNumber > firstBooking.BookingNumber);
        Assert.Equal(RentalStatus.Active, secondBooking.Status);
    }

    [Fact]
    public async Task RegisterReturn_SmallCar_CalculatesPriceAndCompletesBooking()
    {
        EnsureCar("EV11111", CarCategory.SmallCar, 20000);
        var pickupTime = new DateTimeOffset(2026, 9, 10, 10, 0, 0, TimeSpan.Zero);
        var returnTime = pickupTime.AddDays(3);

        var booking = await _api.RegisterPickup(
            registrationNumber: "EV11111",
            customerSsn: "12345678901",
            category: CarCategory.SmallCar,
            pickupStationCode: "OSL",
            pickupDateTime: pickupTime,
            pickupMeterReadingKm: 20000,
            baseDayRental: 480m,
            baseKmPrice: 2.5m,
            cancellationToken: Token);

        var result = await _api.RegisterReturn(
            bookingNumber: booking.BookingNumber,
            returnStationCode: "OSL",
            returnDateTime: returnTime,
            returnMeterReadingKm: 20450,
            cancellationToken: Token);

        Assert.NotNull(result);
        Assert.Equal(RentalStatus.Completed, result.Status);
        Assert.Equal(3, result.CalculatedDays);
        Assert.Equal(450, result.CalculatedKm);
        Assert.Equal(1440m, result.TotalPrice);
    }

    [Fact]
    public async Task RegisterReturn_OneWayRental_RelocatesVehicleToReturnStation()
    {
        EnsureCar("ONEWAY1", CarCategory.Combi, 30000, stationCode: "OSL");
        var pickupTime = new DateTimeOffset(2026, 9, 10, 10, 0, 0, TimeSpan.Zero);

        var booking = await _api.RegisterPickup(
            registrationNumber: "ONEWAY1",
            customerSsn: "12345678901",
            category: CarCategory.Combi,
            pickupStationCode: "OSL",
            pickupDateTime: pickupTime,
            pickupMeterReadingKm: 30000,
            baseDayRental: 600m,
            baseKmPrice: 3m,
            cancellationToken: Token);

        var result = await _api.RegisterReturn(
            bookingNumber: booking.BookingNumber,
            returnStationCode: "BGO",
            returnDateTime: pickupTime.AddDays(2),
            returnMeterReadingKm: 30500,
            cancellationToken: Token);

        Assert.Equal("BGO", result.ReturnStationCode);

        var carInDb = await _ctx.Cars.SingleAsync(c => c.RegistrationNumber == "ONEWAY1", Token);
        Assert.Equal("BGO", carInDb.CurrentStationCode);
        Assert.Equal(30500, carInDb.CurrentMeterReadingKm);
        Assert.Equal(CarStatusValue.Available, carInDb.Status);
    }

    [Fact]
    public async Task RegisterReturn_Combi_CalculatesPriceCorrectly()
    {
        EnsureCar("EV22222", CarCategory.Combi, 50000);
        var pickupTime = new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.Zero);
        var returnTime = pickupTime.AddDays(2);

        var booking = await _api.RegisterPickup(
            registrationNumber: "EV22222",
            customerSsn: "12345678901",
            category: CarCategory.Combi,
            pickupStationCode: "OSL",
            pickupDateTime: pickupTime,
            pickupMeterReadingKm: 50000,
            baseDayRental: 600m,
            baseKmPrice: 3m,
            cancellationToken: Token);

        var result = await _api.RegisterReturn(
            bookingNumber: booking.BookingNumber,
            returnStationCode: "OSL",
            returnDateTime: returnTime,
            returnMeterReadingKm: 50200,
            cancellationToken: Token);

        Assert.NotNull(result);
        Assert.Equal(RentalStatus.Completed, result.Status);
        Assert.Equal(2, result.CalculatedDays);
        Assert.Equal(200, result.CalculatedKm);
        Assert.Equal(2160m, result.TotalPrice);
    }

    [Fact]
    public async Task RegisterReturn_Truck_CalculatesPriceCorrectly()
    {
        EnsureCar("BT33333", CarCategory.Truck, 80000);
        var pickupTime = new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.Zero);
        var returnTime = pickupTime.AddDays(2);

        var booking = await _api.RegisterPickup(
            registrationNumber: "BT33333",
            customerSsn: "12345678901",
            category: CarCategory.Truck,
            pickupStationCode: "OSL",
            pickupDateTime: pickupTime,
            pickupMeterReadingKm: 80000,
            baseDayRental: 1000m,
            baseKmPrice: 4m,
            cancellationToken: Token);

        var result = await _api.RegisterReturn(
            bookingNumber: booking.BookingNumber,
            returnStationCode: "OSL",
            returnDateTime: returnTime,
            returnMeterReadingKm: 80100,
            cancellationToken: Token);

        Assert.NotNull(result);
        Assert.Equal(RentalStatus.Completed, result.Status);
        Assert.Equal(2, result.CalculatedDays);
        Assert.Equal(100, result.CalculatedKm);
        Assert.Equal(3600m, result.TotalPrice);
    }

    [Fact]
    public async Task RegisterReturn_BookingNotFound_ThrowsBookingNotFoundException()
    {
        await Assert.ThrowsAsync<BookingNotFoundException>(() =>
            _api.RegisterReturn(
                bookingNumber: 999999L,
                returnStationCode: "OSL",
                returnDateTime: new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero),
                returnMeterReadingKm: 10000,
                cancellationToken: Token));
    }

    [Fact]
    public async Task RegisterReturn_AlreadyCompleted_ThrowsInvalidRentalOperationException()
    {
        EnsureCar("EV44444", CarCategory.SmallCar, 10000);
        var pickupTime = new DateTimeOffset(2026, 9, 10, 10, 0, 0, TimeSpan.Zero);
        var returnTime = pickupTime.AddDays(1);

        var booking = await _api.RegisterPickup(
            registrationNumber: "EV44444",
            customerSsn: "12345678901",
            category: CarCategory.SmallCar,
            pickupStationCode: "OSL",
            pickupDateTime: pickupTime,
            pickupMeterReadingKm: 10000,
            baseDayRental: 500m,
            baseKmPrice: 2m,
            cancellationToken: Token);

        await _api.RegisterReturn(
            bookingNumber: booking.BookingNumber,
            returnStationCode: "OSL",
            returnDateTime: returnTime,
            returnMeterReadingKm: 10100,
            cancellationToken: Token);

        var ex = await Assert.ThrowsAsync<InvalidRentalOperationException>(() =>
            _api.RegisterReturn(
                bookingNumber: booking.BookingNumber,
                returnStationCode: "OSL",
                returnDateTime: returnTime.AddHours(2),
                returnMeterReadingKm: 10200,
                cancellationToken: Token));

        Assert.Contains("already been returned and completed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetBookingByNumber_ReturnsBooking_WhenFound()
    {
        EnsureCar("EV55555", CarCategory.Combi, 30000);
        var pickupTime = new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);
        var created = await _api.RegisterPickup(
            registrationNumber: "EV55555",
            customerSsn: "12345678901",
            category: CarCategory.Combi,
            pickupStationCode: "OSL",
            pickupDateTime: pickupTime,
            pickupMeterReadingKm: 30000,
            baseDayRental: 600m,
            baseKmPrice: 3m,
            cancellationToken: Token);

        var booking = await _api.GetBookingByNumber(created.BookingNumber, Token);

        Assert.NotNull(booking);
        Assert.Equal(created.BookingNumber, booking.BookingNumber);
        Assert.Equal("EV55555", booking.RegistrationNumber);
        Assert.Equal(CarCategory.Combi, booking.Category);
    }

    [Fact]
    public async Task GetBookingByNumber_ReturnsNull_WhenNotFound()
    {
        var result = await _api.GetBookingByNumber(999999L, Token);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetBookings_ServerPaginationAndFiltering_ReturnsPagedResult()
    {
        EnsureCar("PAG1", CarCategory.SmallCar, 1000);
        EnsureCar("PAG2", CarCategory.Combi, 2000, stationCode: "BGO");
        var time = new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

        await _api.RegisterPickup("PAG1", "12345678901", CarCategory.SmallCar, "OSL", time, 1000, 500m, 2m, Token);
        await _api.RegisterPickup("PAG2", "12345678902", CarCategory.Combi, "BGO", time.AddHours(1), 2000, 600m, 3m, Token);

        var pagedBgo = await _api.GetBookings(pageNumber: 1, pageSize: 10, stationCode: "BGO", cancellationToken: Token);
        Assert.Equal(1, pagedBgo.TotalCount);
        Assert.Single(pagedBgo.Items);

        var pagedAll = await _api.GetBookings(pageNumber: 1, pageSize: 1, cancellationToken: Token);
        Assert.Equal(2, pagedAll.TotalCount);
        Assert.Single(pagedAll.Items);
    }

    [Fact]
    public async Task GetAllBookings_ReturnsAllBookings()
    {
        EnsureCar("EV66661", CarCategory.SmallCar, 1000);
        EnsureCar("EV66662", CarCategory.Combi, 2000);
        var time = new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

        await _api.RegisterPickup("EV66661", "12345678901", CarCategory.SmallCar, "OSL", time, 1000, 500m, 2m, Token);
        await _api.RegisterPickup("EV66662", "12345678902", CarCategory.Combi, "OSL", time.AddHours(1), 2000, 600m, 3m, Token);

        var all = await _api.GetAllBookings(Token);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetActiveBookings_ReturnsOnlyActiveBookings()
    {
        EnsureCar("EV77771", CarCategory.SmallCar, 1000);
        EnsureCar("EV77772", CarCategory.Combi, 2000);
        var time = new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

        var first = await _api.RegisterPickup("EV77771", "12345678901", CarCategory.SmallCar, "OSL", time, 1000, 500m, 2m, Token);
        var second = await _api.RegisterPickup("EV77772", "12345678902", CarCategory.Combi, "OSL", time.AddHours(1), 2000, 600m, 3m, Token);

        await _api.RegisterReturn(first.BookingNumber, "OSL", time.AddDays(1), 1100, cancellationToken: Token);

        var active = await _api.GetActiveBookings(Token);
        var singleActive = Assert.Single(active);
        Assert.Equal(second.BookingNumber, singleActive.BookingNumber);
        Assert.Equal(RentalStatus.Active, singleActive.Status);
    }

    [Fact]
    public async Task RegisterPickup_CarNotRegisteredInFleet_ThrowsInvalidRentalOperationException()
    {
        var pickupTime = new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

        var ex = await Assert.ThrowsAsync<InvalidRentalOperationException>(() =>
            _api.RegisterPickup(
                registrationNumber: "UNREGISTERED",
                customerSsn: "12345678901",
                category: CarCategory.SmallCar,
                pickupStationCode: "OSL",
                pickupDateTime: pickupTime,
                pickupMeterReadingKm: 1000,
                baseDayRental: 500m,
                baseKmPrice: 2m,
                cancellationToken: Token));

        Assert.Contains("not registered", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterPickup_CarCategoryMismatch_ThrowsInvalidRentalOperationException()
    {
        EnsureCar("CAT_MISMATCH", CarCategory.Truck, 5000);
        var pickupTime = new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

        var ex = await Assert.ThrowsAsync<InvalidRentalOperationException>(() =>
            _api.RegisterPickup(
                registrationNumber: "CAT_MISMATCH",
                customerSsn: "12345678901",
                category: CarCategory.SmallCar,
                pickupStationCode: "OSL",
                pickupDateTime: pickupTime,
                pickupMeterReadingKm: 5000,
                baseDayRental: 500m,
                baseKmPrice: 2m,
                cancellationToken: Token));

        Assert.Contains("does not match", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterPickup_PickupMeterReadingLessThanCarMeterReading_ThrowsInvalidRentalOperationException()
    {
        EnsureCar("ODOMETER_CHECK", CarCategory.Combi, 25000);
        var pickupTime = new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

        var ex = await Assert.ThrowsAsync<InvalidRentalOperationException>(() =>
            _api.RegisterPickup(
                registrationNumber: "ODOMETER_CHECK",
                customerSsn: "12345678901",
                category: CarCategory.Combi,
                pickupStationCode: "OSL",
                pickupDateTime: pickupTime,
                pickupMeterReadingKm: 24000,
                baseDayRental: 600m,
                baseKmPrice: 3m,
                cancellationToken: Token));

        Assert.Contains("cannot be less than", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterPickup_And_RegisterReturn_TransitionsCarStatusAndOdometer()
    {
        EnsureCar("LIFECYCLE_01", CarCategory.SmallCar, 12000);
        var pickupTime = new DateTimeOffset(2026, 9, 10, 9, 0, 0, TimeSpan.Zero);

        var booking = await _api.RegisterPickup(
            registrationNumber: "LIFECYCLE_01",
            customerSsn: "12345678901",
            category: CarCategory.SmallCar,
            pickupStationCode: "OSL",
            pickupDateTime: pickupTime,
            pickupMeterReadingKm: 12000,
            baseDayRental: 500m,
            baseKmPrice: 2m,
            cancellationToken: Token);

        var carDuringRental = await _ctx.Cars.SingleAsync(c => c.RegistrationNumber == "LIFECYCLE_01", Token);
        Assert.Equal(CarStatusValue.Rented, carDuringRental.Status);

        await _api.RegisterReturn(booking.BookingNumber, "OSL", pickupTime.AddDays(2), 12500, cancellationToken: Token);

        var carAfterReturn = await _ctx.Cars.SingleAsync(c => c.RegistrationNumber == "LIFECYCLE_01", Token);
        Assert.Equal(CarStatusValue.Available, carAfterReturn.Status);
        Assert.Equal(12500, carAfterReturn.CurrentMeterReadingKm);
    }
}
