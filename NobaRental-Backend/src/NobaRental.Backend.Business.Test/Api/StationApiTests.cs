using Microsoft.EntityFrameworkCore;
using NobaRental.Backend.Business.Api;
using NobaRental.Backend.Data;
using NobaRental.Backend.Data.Entities;
using NobaRental.Backend.Data.Entities.Values;
using NobaRental.Backend.Domain.Exceptions;

namespace NobaRental.Backend.Business.Test.Api;

public sealed class StationApiTests : IDisposable
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private readonly NobaRentalDbContext _ctx;
    private readonly StationApi _api;

    public StationApiTests()
    {
        var options = new DbContextOptionsBuilder<NobaRentalDbContext>()
            .UseInMemoryDatabase(databaseName: $"NobaRental_Stations_{Guid.NewGuid()}")
            .Options;

        _ctx = new NobaRentalDbContext(options);
        _api = new StationApi(_ctx);
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Fact]
    public async Task CreateStation_Success_AddsStation()
    {
        var station = await _api.CreateStation("krs", "Kristiansand Airport Kjevik", "Kristiansand", Token);

        Assert.NotNull(station);
        Assert.Equal("KRS", station.Code);
        Assert.Equal("Kristiansand Airport Kjevik", station.Name);
        Assert.Equal("Kristiansand", station.City);
        Assert.True(station.IsActive);
        Assert.False(station.IsDeleted);

        var inDb = await _ctx.Stations.SingleOrDefaultAsync(s => s.Code == "KRS", Token);
        Assert.NotNull(inDb);
        Assert.Equal("KRS", inDb.Code);
    }

    [Fact]
    public async Task CreateStation_DuplicateActive_ThrowsInvalidRentalOperationException()
    {
        await _api.CreateStation("HAU", "Haugesund Airport", "Haugesund", Token);

        var ex = await Assert.ThrowsAsync<InvalidRentalOperationException>(() =>
            _api.CreateStation("hau", "Haugesund Karmøy", "Haugesund", Token));

        Assert.Contains("already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateStation_ReactivatesSoftDeletedStation()
    {
        await _api.CreateStation("MOL", "Molde Airport", "Molde", Token);
        await _api.DeleteStation("MOL", Token);

        var reactivated = await _api.CreateStation("MOL", "Molde Airport Årø", "Molde", Token);
        Assert.NotNull(reactivated);
        Assert.Equal("Molde Airport Årø", reactivated.Name);
        Assert.True(reactivated.IsActive);
        Assert.False(reactivated.IsDeleted);
    }

    [Fact]
    public async Task UpdateStation_Success_ModifiesMetadata()
    {
        await _api.CreateStation("AES", "Ålesund Vigra", "Ålesund", Token);

        var updated = await _api.UpdateStation("AES", "Ålesund Airport Vigra", "Ålesund", isActive: false, cancellationToken: Token);

        Assert.Equal("Ålesund Airport Vigra", updated.Name);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task UpdateStation_NotFound_ThrowsInvalidRentalOperationException()
    {
        await Assert.ThrowsAsync<InvalidRentalOperationException>(() =>
            _api.UpdateStation("MISSING", "Name", "City", true, cancellationToken: Token));
    }

    [Fact]
    public async Task DeleteStation_Success_SoftDeletes()
    {
        await _api.CreateStation("TOS", "Tromsø Airport", "Tromsø", Token);

        await _api.DeleteStation("TOS", Token);

        var active = await _ctx.Stations.SingleOrDefaultAsync(s => s.Code == "TOS", Token);
        Assert.Null(active);

        var audit = await _ctx.Stations.IgnoreQueryFilters().SingleOrDefaultAsync(s => s.Code == "TOS", Token);
        Assert.NotNull(audit);
        Assert.True(audit.IsDeleted);
    }

    [Fact]
    public async Task DeleteStation_WhenCarsStationed_ThrowsStationInUseException()
    {
        await _api.CreateStation("BOO", "Bodø Airport", "Bodø", Token);
        _ctx.Cars.Add(new CarEntity
        {
            RegistrationNumber = "BO12345",
            Category = CarCategoryValue.SmallCar,
            CurrentMeterReadingKm = 1000,
            Status = CarStatusValue.Available,
            CurrentStationCode = "BOO"
        });
        await _ctx.SaveChangesAsync(Token);

        var ex = await Assert.ThrowsAsync<StationInUseException>(() =>
            _api.DeleteStation("BOO", Token));

        Assert.Equal("BOO", ex.StationCode);
        Assert.Contains("vehicles are currently stationed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteStation_WhenActiveBookingsExist_ThrowsStationInUseException()
    {
        await _api.CreateStation("EVE", "Harstad/Narvik Airport", "Evenes", Token);
        _ctx.RentalBookings.Add(new RentalBookingEntity
        {
            BookingNumber = 9999,
            RegistrationNumber = "EV99999",
            CustomerSsn = "12345678901",
            Category = CarCategoryValue.SmallCar,
            PickupStationCode = "EVE",
            PickupDateTime = new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero),
            PickupMeterReadingKm = 500,
            BaseDayRental = 500m,
            BaseKmPrice = 2m,
            Currency = "NOK",
            Status = RentalStatusValue.Active
        });
        await _ctx.SaveChangesAsync(Token);

        var ex = await Assert.ThrowsAsync<StationInUseException>(() =>
            _api.DeleteStation("EVE", Token));

        Assert.Equal("EVE", ex.StationCode);
        Assert.Contains("active rental bookings", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAllStations_FiltersInactiveByDefault()
    {
        await _api.CreateStation("ACT1", "Active Station", "City A", Token);
        var st2 = await _api.CreateStation("INA1", "Inactive Station", "City B", Token);
        await _api.UpdateStation("INA1", st2.Name, st2.City, isActive: false, cancellationToken: Token);

        var activeOnly = await _api.GetAllStations(includeInactive: false, Token);
        Assert.Contains(activeOnly, s => string.Equals(s.Code, "ACT1", StringComparison.Ordinal));
        Assert.DoesNotContain(activeOnly, s => string.Equals(s.Code, "INA1", StringComparison.Ordinal));

        var all = await _api.GetAllStations(includeInactive: true, Token);
        Assert.Contains(all, s => string.Equals(s.Code, "ACT1", StringComparison.Ordinal));
        Assert.Contains(all, s => string.Equals(s.Code, "INA1", StringComparison.Ordinal));
    }
}
