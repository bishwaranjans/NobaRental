using Microsoft.EntityFrameworkCore;
using NobaRental.Backend.Business.Api;
using NobaRental.Backend.Data;
using NobaRental.Backend.Data.Entities;
using NobaRental.Backend.Data.Entities.Values;
using NobaRental.Backend.Domain.Exceptions;
using NobaRental.Backend.Domain.Values;

namespace NobaRental.Backend.Business.Test.Api;

public sealed class CarFleetApiTests : IDisposable
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private readonly NobaRentalDbContext _ctx;
    private readonly CarFleetApi _api;

    public CarFleetApiTests()
    {
        var options = new DbContextOptionsBuilder<NobaRentalDbContext>()
            .UseInMemoryDatabase(databaseName: $"NobaRental_Fleet_{Guid.NewGuid()}")
            .Options;

        _ctx = new NobaRentalDbContext(options);
        _ctx.Stations.AddRange(
            new StationEntity { Code = "OSL", Name = "Oslo Airport", City = "Oslo", IsActive = true },
            new StationEntity { Code = "BGO", Name = "Bergen Airport", City = "Bergen", IsActive = true });
        _ctx.SaveChanges();

        _api = new CarFleetApi(_ctx);
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Fact]
    public async Task RegisterCar_Success_AddsCarWithAvailableStatusAndStation()
    {
        var car = await _api.RegisterCar("bt12345", CarCategory.SmallCar, 1500, "OSL", 500m, cancellationToken: Token);

        Assert.NotNull(car);
        Assert.Equal("BT12345", car.RegistrationNumber);
        Assert.Equal(CarCategory.SmallCar, car.Category);
        Assert.Equal(1500, car.CurrentMeterReadingKm);
        Assert.Equal("OSL", car.CurrentStationCode);
        Assert.Equal(CarStatus.Available, car.Status);
        Assert.Equal(500m, car.BaseDayRental);
        Assert.Equal(0m, car.BaseKmPrice);

        var inDb = await _ctx.Cars.SingleOrDefaultAsync(c => c.RegistrationNumber == "BT12345", Token);
        Assert.NotNull(inDb);
        Assert.Equal(CarStatusValue.Available, inDb.Status);
        Assert.Equal(500m, inDb.BaseDayRental);
        Assert.Equal(0m, inDb.BaseKmPrice);
    }

    [Fact]
    public async Task RegisterCar_SmallCar_EnforcesZeroKmPriceEvenWhenSpecified()
    {
        var car = await _api.RegisterCar("EV99999", CarCategory.SmallCar, 1000, "OSL", 450m, baseKmPrice: 15m, Token);

        Assert.Equal(0m, car.BaseKmPrice);

        var inDb = await _ctx.Cars.SingleAsync(c => c.RegistrationNumber == "EV99999", Token);
        Assert.Equal(0m, inDb.BaseKmPrice);
    }

    [Fact]
    public async Task RegisterCar_CombiAndTruck_PersistsCustomRates()
    {
        var combi = await _api.RegisterCar("CB55555", CarCategory.Combi, 2000, "OSL", 750m, baseKmPrice: 3.5m, Token);
        Assert.Equal(750m, combi.BaseDayRental);
        Assert.Equal(3.5m, combi.BaseKmPrice);

        var truck = await _api.RegisterCar("TR77777", CarCategory.Truck, 5000, "BGO", 1300m, baseKmPrice: 5.0m, Token);
        Assert.Equal(1300m, truck.BaseDayRental);
        Assert.Equal(5.0m, truck.BaseKmPrice);
    }

    [Fact]
    public async Task RegisterCar_ZeroOrNegativeDayRate_ThrowsArgumentOutOfRangeException()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _api.RegisterCar("BT00000", CarCategory.SmallCar, 1000, "OSL", 0m, cancellationToken: Token));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _api.RegisterCar("BT00001", CarCategory.SmallCar, 1000, "OSL", -100m, cancellationToken: Token));
    }

    [Fact]
    public async Task RegisterCar_DuplicatePlate_ThrowsInvalidRentalOperationException()
    {
        await _api.RegisterCar("BT12345", CarCategory.SmallCar, 1500, "OSL", 500m, cancellationToken: Token);

        var ex = await Assert.ThrowsAsync<InvalidRentalOperationException>(() =>
            _api.RegisterCar("bt12345", CarCategory.Combi, 2000, "OSL", 600m, cancellationToken: Token));

        Assert.Contains("already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterCar_NegativeMeterReading_ThrowsArgumentOutOfRangeException()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _api.RegisterCar("BT99999", CarCategory.Truck, -10, "OSL", 1000m, cancellationToken: Token));
    }

    [Fact]
    public async Task GetAllCars_ReturnsAllVehiclesSortedByPlate()
    {
        await _api.RegisterCar("ZZ99999", CarCategory.Truck, 50000, "OSL", 1200m, cancellationToken: Token);
        await _api.RegisterCar("AA11111", CarCategory.SmallCar, 1000, "OSL", 500m, cancellationToken: Token);

        var all = await _api.GetAllCars(Token);

        Assert.Equal(2, all.Count);
        Assert.Equal("AA11111", all.First().RegistrationNumber);
        Assert.Equal("ZZ99999", all.Last().RegistrationNumber);
    }

    [Fact]
    public async Task GetCars_ServerPaginationAndFiltering_ReturnsPagedResult()
    {
        await _api.RegisterCar("CAR001", CarCategory.SmallCar, 1000, "OSL", 500m, cancellationToken: Token);
        await _api.RegisterCar("CAR002", CarCategory.Combi, 2000, "BGO", 600m, cancellationToken: Token);
        await _api.RegisterCar("CAR003", CarCategory.Truck, 3000, "OSL", 1000m, cancellationToken: Token);

        var pagedOsl = await _api.GetCars(pageNumber: 1, pageSize: 10, stationCode: "OSL", cancellationToken: Token);
        Assert.Equal(2, pagedOsl.TotalCount);
        Assert.Equal(2, pagedOsl.Items.Count);

        var pagedSearch = await _api.GetCars(pageNumber: 1, pageSize: 1, searchTerm: "CAR00", cancellationToken: Token);
        Assert.Equal(3, pagedSearch.TotalCount);
        Assert.Single(pagedSearch.Items);
    }

    [Fact]
    public async Task DeleteCar_Available_DecommissionsAndSoftDeletesCar()
    {
        await _api.RegisterCar("DEL999", CarCategory.SmallCar, 5000, "OSL", 500m, cancellationToken: Token);

        await _api.DeleteCar("DEL999", Token);

        var active = await _api.GetCarByRegistrationNumber("DEL999", Token);
        Assert.Null(active);

        var audit = await _ctx.Cars.IgnoreQueryFilters().SingleOrDefaultAsync(c => c.RegistrationNumber == "DEL999", Token);
        Assert.NotNull(audit);
        Assert.True(audit.IsDeleted);
        Assert.Equal(CarStatusValue.Decommissioned, audit.Status);
    }

    [Fact]
    public async Task DeleteCar_Rented_ThrowsInvalidRentalOperationException()
    {
        var car = await _api.RegisterCar("RENT11", CarCategory.SmallCar, 5000, "OSL", 500m, cancellationToken: Token);
        var dbCar = await _ctx.Cars.SingleAsync(c => c.RegistrationNumber == car.RegistrationNumber, Token);
        dbCar.Status = CarStatusValue.Rented;
        await _ctx.SaveChangesAsync(Token);

        var ex = await Assert.ThrowsAsync<InvalidRentalOperationException>(() =>
            _api.DeleteCar("RENT11", Token));

        Assert.Contains("currently rented", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAvailableCars_FiltersOnlyAvailable()
    {
        var available = await _api.RegisterCar("AV11111", CarCategory.SmallCar, 1000, "OSL", 500m, cancellationToken: Token);
        var rentedEntity = new CarEntity
        {
            RegistrationNumber = "RN22222",
            Category = CarCategoryValue.Combi,
            CurrentMeterReadingKm = 2000,
            CurrentStationCode = "OSL",
            BaseDayRental = 700m,
            BaseKmPrice = 2m,
            Status = CarStatusValue.Rented,
        };
        _ctx.Cars.Add(rentedEntity);
        await _ctx.SaveChangesAsync(Token);

        var availableList = await _api.GetAvailableCars(stationCode: null, category: null, cancellationToken: Token);

        var single = Assert.Single(availableList);
        Assert.Equal(available.RegistrationNumber, single.RegistrationNumber);
    }

    [Fact]
    public async Task GetAvailableCars_WithCategoryAndStation_FiltersCorrectly()
    {
        await _api.RegisterCar("SM11111", CarCategory.SmallCar, 1000, "OSL", 500m, cancellationToken: Token);
        await _api.RegisterCar("CB22222", CarCategory.Combi, 2000, "OSL", 700m, cancellationToken: Token);
        await _api.RegisterCar("CB33333", CarCategory.Combi, 3000, "BGO", 700m, cancellationToken: Token);

        var oslCombi = await _api.GetAvailableCars(stationCode: "OSL", category: CarCategory.Combi, cancellationToken: Token);

        var single = Assert.Single(oslCombi);
        Assert.Equal("CB22222", single.RegistrationNumber);
        Assert.Equal(CarCategory.Combi, single.Category);
        Assert.Equal("OSL", single.CurrentStationCode);
    }

    [Fact]
    public async Task GetCarByRegistrationNumber_Found_ReturnsCar()
    {
        await _api.RegisterCar("EX12345", CarCategory.Combi, 8000, "OSL", 750m, cancellationToken: Token);

        var found = await _api.GetCarByRegistrationNumber("ex12345", Token);

        Assert.NotNull(found);
        Assert.Equal("EX12345", found.RegistrationNumber);
        Assert.Equal(8000, found.CurrentMeterReadingKm);
    }

    [Fact]
    public async Task GetCarByRegistrationNumber_NotFound_ReturnsNull()
    {
        var result = await _api.GetCarByRegistrationNumber("NOPE", Token);
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateCarTariff_WhenCarExists_UpdatesRatesSuccessfully()
    {
        await _api.RegisterCar("TF11111", CarCategory.Combi, 2000, "OSL", 600m, baseKmPrice: 2m, cancellationToken: Token);

        var updated = await _api.UpdateCarTariff("TF11111", baseDayRental: 850m, baseKmPrice: 3.5m, cancellationToken: Token);

        Assert.Equal(850m, updated.BaseDayRental);
        Assert.Equal(3.5m, updated.BaseKmPrice);

        var inDb = await _ctx.Cars.SingleAsync(c => c.RegistrationNumber == "TF11111", Token);
        Assert.Equal(850m, inDb.BaseDayRental);
        Assert.Equal(3.5m, inDb.BaseKmPrice);
    }

    [Fact]
    public async Task UpdateCarTariff_WhenSmallCar_ForcesBaseKmPriceToZero()
    {
        await _api.RegisterCar("SM99999", CarCategory.SmallCar, 1000, "OSL", 400m, cancellationToken: Token);

        var updated = await _api.UpdateCarTariff("sm99999", baseDayRental: 550m, baseKmPrice: 10m, cancellationToken: Token);

        Assert.Equal(550m, updated.BaseDayRental);
        Assert.Equal(0m, updated.BaseKmPrice);

        var inDb = await _ctx.Cars.SingleAsync(c => c.RegistrationNumber == "SM99999", Token);
        Assert.Equal(0m, inDb.BaseKmPrice);
    }

    [Fact]
    public async Task UpdateCarTariff_WhenCarIsRented_UpdatesRatesSuccessfullyForFutureRentals()
    {
        await _api.RegisterCar("RN33333", CarCategory.Truck, 5000, "OSL", 1000m, baseKmPrice: 5m, cancellationToken: Token);
        var inDb = await _ctx.Cars.SingleAsync(c => c.RegistrationNumber == "RN33333", Token);
        inDb.Status = CarStatusValue.Rented;
        await _ctx.SaveChangesAsync(Token);

        var updated = await _api.UpdateCarTariff("RN33333", baseDayRental: 1200m, baseKmPrice: 6m, cancellationToken: Token);

        Assert.Equal(1200m, updated.BaseDayRental);
        Assert.Equal(6m, updated.BaseKmPrice);
        Assert.Equal(CarStatus.Rented, updated.Status);
    }

    [Fact]
    public async Task UpdateCarTariff_WhenZeroOrNegativeDayRate_ThrowsArgumentOutOfRangeException()
    {
        await _api.RegisterCar("TF22222", CarCategory.Combi, 2000, "OSL", 600m, baseKmPrice: 2m, cancellationToken: Token);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _api.UpdateCarTariff("TF22222", baseDayRental: 0m, cancellationToken: Token));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _api.UpdateCarTariff("TF22222", baseDayRental: -50m, cancellationToken: Token));
    }

    [Fact]
    public async Task UpdateCarTariff_WhenNegativeKmPrice_ThrowsArgumentOutOfRangeException()
    {
        await _api.RegisterCar("TF33333", CarCategory.Combi, 2000, "OSL", 600m, baseKmPrice: 2m, cancellationToken: Token);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _api.UpdateCarTariff("TF33333", baseDayRental: 600m, baseKmPrice: -1m, cancellationToken: Token));
    }

    [Fact]
    public async Task UpdateCarTariff_WhenCarNotFound_ThrowsInvalidRentalOperationException()
    {
        await Assert.ThrowsAsync<InvalidRentalOperationException>(() =>
            _api.UpdateCarTariff("NOTFOUND", baseDayRental: 600m, cancellationToken: Token));
    }
}
