using Microsoft.EntityFrameworkCore;
using NobaRental.Backend.Data.Entities;
using NobaRental.Backend.Data.Entities.Values;

namespace NobaRental.Backend.Data.Test;

public sealed class SoftDeleteTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private sealed class TestTimeProvider(DateTimeOffset initialUtcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = initialUtcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void SetUtcNow(DateTimeOffset utcNow) => _utcNow = utcNow;
    }

    [Fact]
    public async Task SaveChangesAsync_WhenCarDeleted_ConvertsToSoftDeleteAndAppliesTimestamp()
    {
        // Arrange
        var fakeTime = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new TestTimeProvider(fakeTime);

        var options = new DbContextOptionsBuilder<NobaRentalDbContext>()
            .UseInMemoryDatabase(databaseName: $"SoftDelete_Car_{Guid.NewGuid()}")
            .Options;

        await using var ctx = new NobaRentalDbContext(options, timeProvider);

        var car = new CarEntity
        {
            RegistrationNumber = "SD10001",
            CategoryCode = "SMALL",
            CurrentMeterReadingKm = 1000,
            Status = CarStatusValue.Available,
            CurrentStationCode = "OSL",
        };
        ctx.Cars.Add(car);
        await ctx.SaveChangesAsync(Token);

        // Act
        ctx.Cars.Remove(car);
        await ctx.SaveChangesAsync(Token);

        // Assert - query with IgnoreQueryFilters to see persisted state
        var deletedCar = await ctx.Cars.IgnoreQueryFilters().SingleOrDefaultAsync(c => c.RegistrationNumber == "SD10001", Token);
        Assert.NotNull(deletedCar);
        Assert.True(deletedCar.IsDeleted);
        Assert.Equal(fakeTime, deletedCar.DeletedAt);
    }

    [Fact]
    public async Task Query_WhenSoftDeleted_GlobalQueryFilterExcludesSoftDeletedRecords()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<NobaRentalDbContext>()
            .UseInMemoryDatabase(databaseName: $"QueryFilter_{Guid.NewGuid()}")
            .Options;

        await using var ctx = new NobaRentalDbContext(options);

        var activeCar = new CarEntity
        {
            RegistrationNumber = "ACT001",
            CategoryCode = "SMALL",
            CurrentMeterReadingKm = 500,
            Status = CarStatusValue.Available,
            CurrentStationCode = "OSL",
        };
        var deleteTime = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);
        var deletedCar = new CarEntity
        {
            RegistrationNumber = "DEL001",
            CategoryCode = "COMBI",
            CurrentMeterReadingKm = 800,
            Status = CarStatusValue.Decommissioned,
            CurrentStationCode = "OSL",
            IsDeleted = true,
            DeletedAt = deleteTime,
        };

        ctx.Cars.AddRange(activeCar, deletedCar);
        await ctx.SaveChangesAsync(Token);

        // Act - standard query with global filter
        var cars = await ctx.Cars.ToListAsync(Token);

        // Assert
        Assert.Contains(cars, c => string.Equals(c.RegistrationNumber, "ACT001", StringComparison.Ordinal));
        Assert.DoesNotContain(cars, c => string.Equals(c.RegistrationNumber, "DEL001", StringComparison.Ordinal));

        // Act - audit query ignoring filters
        var allCars = await ctx.Cars.IgnoreQueryFilters().ToListAsync(Token);
        Assert.Contains(allCars, c => string.Equals(c.RegistrationNumber, "DEL001", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SaveChangesAsync_WhenStationDeleted_AppliesSoftDelete()
    {
        // Arrange
        var fakeTime = new DateTimeOffset(2026, 9, 15, 14, 0, 0, TimeSpan.Zero);
        var timeProvider = new TestTimeProvider(fakeTime);

        var options = new DbContextOptionsBuilder<NobaRentalDbContext>()
            .UseInMemoryDatabase(databaseName: $"Station_SoftDelete_{Guid.NewGuid()}")
            .Options;

        await using var ctx = new NobaRentalDbContext(options, timeProvider);

        var station = new StationEntity
        {
            Code = "TEST-ST",
            Name = "Test Station",
            City = "Oslo",
            IsActive = true,
        };
        ctx.Stations.Add(station);
        await ctx.SaveChangesAsync(Token);

        // Act
        ctx.Stations.Remove(station);
        await ctx.SaveChangesAsync(Token);

        // Assert
        var activeStations = await ctx.Stations.ToListAsync(Token);
        Assert.DoesNotContain(activeStations, s => string.Equals(s.Code, "TEST-ST", StringComparison.Ordinal));

        var auditStation = await ctx.Stations.IgnoreQueryFilters().SingleOrDefaultAsync(s => s.Code == "TEST-ST", Token);
        Assert.NotNull(auditStation);
        Assert.True(auditStation.IsDeleted);
        Assert.Equal(fakeTime, auditStation.DeletedAt);
    }

    [Fact]
    public async Task SaveChangesAsync_SetsAuditTimestamps_OnCreateAndModify()
    {
        // Arrange
        var createTime = new DateTimeOffset(2026, 9, 15, 10, 0, 0, TimeSpan.Zero);
        var modifyTime = new DateTimeOffset(2026, 9, 15, 15, 30, 0, TimeSpan.Zero);
        var timeProvider = new TestTimeProvider(createTime);

        var options = new DbContextOptionsBuilder<NobaRentalDbContext>()
            .UseInMemoryDatabase(databaseName: $"Audit_{Guid.NewGuid()}")
            .Options;

        await using var ctx = new NobaRentalDbContext(options, timeProvider);

        var station = new StationEntity
        {
            Code = "AUDIT-1",
            Name = "Audit Station",
            City = "Bergen",
        };
        ctx.Stations.Add(station);
        await ctx.SaveChangesAsync(Token);

        Assert.Equal(createTime, station.CreatedAt);
        Assert.Null(station.ModifiedAt);

        // Advance time and modify
        timeProvider.SetUtcNow(modifyTime);
        station.Name = "Updated Station Name";
        await ctx.SaveChangesAsync(Token);

        Assert.Equal(createTime, station.CreatedAt);
        Assert.Equal(modifyTime, station.ModifiedAt);
    }
}
