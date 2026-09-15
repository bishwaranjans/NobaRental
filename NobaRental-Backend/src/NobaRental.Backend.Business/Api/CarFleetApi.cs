using Microsoft.EntityFrameworkCore;
using NobaRental.Backend.Business.Mapping;
using NobaRental.Backend.Data;
using NobaRental.Backend.Data.Entities;
using NobaRental.Backend.Data.Entities.Values;
using NobaRental.Backend.Domain;
using NobaRental.Backend.Domain.Exceptions;
using NobaRental.Backend.Domain.Models;
using NobaRental.Backend.Domain.Values;

namespace NobaRental.Backend.Business.Api;

public class CarFleetApi(NobaRentalDbContext dbContext) : ICarFleetApi
{
    public async Task<Car> RegisterCar(
        string registrationNumber,
        string categoryCode,
        long initialMeterReadingKm,
        string stationCode,
        decimal baseDayRental,
        decimal baseKmPrice = 0m,
        CancellationToken cancellationToken = default)
    {
        ValidateRegisterArguments(registrationNumber, categoryCode, stationCode, baseDayRental, baseKmPrice, initialMeterReadingKm);

        var normalizedReg = registrationNumber.Trim().ToUpperInvariant();
        var normalizedCat = categoryCode.Trim().ToUpperInvariant();
        var normalizedStation = stationCode.Trim().ToUpperInvariant();

        var category = await dbContext.CarCategories
            .SingleOrDefaultAsync(c => c.Code == normalizedCat, cancellationToken);

        if (category?.IsActive != true)
        {
            throw new InvalidRentalOperationException($"Category '{normalizedCat}' does not exist or is inactive.");
        }

        if (!category.ChargesKilometers)
        {
            baseKmPrice = 0m;
        }

        var station = await dbContext.Stations
            .SingleOrDefaultAsync(s => s.Code == normalizedStation, cancellationToken);

        if (station?.IsActive != true)
        {
            throw new InvalidRentalOperationException($"Station '{normalizedStation}' does not exist or is inactive.");
        }

        var existing = await dbContext.Cars
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.RegistrationNumber == normalizedReg, cancellationToken);

        if (existing is not null)
        {
            if (existing.IsDeleted)
            {
                ResurrectDeletedCar(existing, normalizedCat, initialMeterReadingKm, normalizedStation, baseDayRental, baseKmPrice);
                await dbContext.SaveChangesAsync(cancellationToken);
                return existing.Map();
            }

            throw new InvalidRentalOperationException($"Car with registration number '{normalizedReg}' already exists.");
        }

        var entity = CarMap.MapToEntity(
            normalizedReg,
            normalizedCat,
            initialMeterReadingKm,
            normalizedStation,
            baseDayRental,
            baseKmPrice,
            status: CarStatus.Available);
        dbContext.Cars.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return entity.Map();
    }

    private static void ValidateRegisterArguments(string reg, string categoryCode, string station, decimal dayPrice, decimal kmPrice, long meter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reg);
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(station);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dayPrice);
        ArgumentOutOfRangeException.ThrowIfNegative(kmPrice);
        if (meter < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(meter), "Initial meter reading cannot be negative.");
        }
    }

    private static void ResurrectDeletedCar(CarEntity existing, string categoryCode, long meter, string station, decimal dayPrice, decimal kmPrice)
    {
        existing.IsDeleted = false;
        existing.DeletedAt = null;
        existing.CategoryCode = categoryCode;
        existing.CurrentMeterReadingKm = meter;
        existing.CurrentStationCode = station;
        existing.BaseDayRental = dayPrice;
        existing.BaseKmPrice = kmPrice;
        existing.Status = CarStatusValue.Available;
    }

    public async Task DeleteCar(string registrationNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registrationNumber);
        var normalizedReg = registrationNumber.Trim().ToUpperInvariant();

        var car = await dbContext.Cars
            .SingleOrDefaultAsync(c => c.RegistrationNumber == normalizedReg, cancellationToken);

        if (car is null)
        {
            return;
        }

        if (car.Status == CarStatusValue.Rented)
        {
            throw new InvalidRentalOperationException("Cannot decommission or delete a car that is currently rented.");
        }

        car.Status = CarStatusValue.Decommissioned;
        dbContext.Cars.Remove(car);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Car> UpdateCarTariff(
        string registrationNumber,
        decimal baseDayRental,
        decimal baseKmPrice = 0m,
        byte[]? rowVersion = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registrationNumber);
        if (baseDayRental <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseDayRental), "Base day rental must be greater than zero.");
        }

        if (baseKmPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseKmPrice), "Base kilometer price cannot be negative.");
        }

        var normalizedReg = registrationNumber.Trim().ToUpperInvariant();
        var car = await dbContext.Cars
            .Include(c => c.Category)
            .SingleOrDefaultAsync(c => c.RegistrationNumber == normalizedReg, cancellationToken)
            ?? throw new InvalidRentalOperationException($"Car '{normalizedReg}' is not registered in the fleet.");

        if (car.Category?.ChargesKilometers == false)
        {
            baseKmPrice = 0m;
        }

        if (rowVersion?.Length > 0)
        {
            dbContext.Entry(car).Property(x => x.RowVersion).OriginalValue = rowVersion;
        }

        car.BaseDayRental = baseDayRental;
        car.BaseKmPrice = baseKmPrice;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new RentalConcurrencyException($"Car '{normalizedReg}' was modified by another operation.", ex);
        }

        return car.Map();
    }

    public async Task<IReadOnlyCollection<Car>> GetAllCars(CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.Cars
            .AsNoTracking()
            .Include(c => c.Category)
            .OrderBy(c => c.RegistrationNumber)
            .ToListAsync(cancellationToken);

        return entities.ConvertAll(e => e.Map());
    }

    public async Task<PagedResult<Car>> GetCars(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? stationCode = null,
        CarStatus? status = null,
        string? sortBy = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Cars
            .AsNoTracking()
            .Include(c => c.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToUpperInvariant();
            query = query.Where(c => c.RegistrationNumber.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(stationCode))
        {
            var station = stationCode.Trim().ToUpperInvariant();
            query = query.Where(c => c.CurrentStationCode == station);
        }

        if (status.HasValue)
        {
            var statusVal = status.Value.ToEntity();
            query = query.Where(c => c.Status == statusVal);
        }

        query = ApplySorting(query, sortBy, sortDescending);

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, pageNumber);
        var size = Math.Max(1, pageSize);

        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(c => c.Map())
            .ToListAsync(cancellationToken);

        return new PagedResult<Car>(items, totalCount, page, size);
    }

    public async Task<IReadOnlyCollection<Car>> GetAvailableCars(
        string? stationCode = null,
        string? categoryCode = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Cars
            .AsNoTracking()
            .Include(c => c.Category)
            .Where(c => c.Status == CarStatusValue.Available);

        if (!string.IsNullOrWhiteSpace(stationCode))
        {
            var normalizedStation = stationCode.Trim().ToUpperInvariant();
            query = query.Where(c => c.CurrentStationCode == normalizedStation);
        }

        if (!string.IsNullOrWhiteSpace(categoryCode))
        {
            var normalizedCat = categoryCode.Trim().ToUpperInvariant();
            query = query.Where(c => c.CategoryCode == normalizedCat);
        }

        var entities = await query
            .OrderBy(c => c.RegistrationNumber)
            .ToListAsync(cancellationToken);

        return entities.ConvertAll(e => e.Map());
    }

    public async Task<Car?> GetCarByRegistrationNumber(string registrationNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registrationNumber);

        var normalizedReg = registrationNumber.Trim().ToUpperInvariant();

        var entity = await dbContext.Cars
            .AsNoTracking()
            .Include(c => c.Category)
            .FirstOrDefaultAsync(c => c.RegistrationNumber == normalizedReg, cancellationToken);

        return entity?.Map();
    }

    private static IQueryable<CarEntity> ApplySorting(IQueryable<CarEntity> query, string? sortBy, bool sortDescending) =>
        sortBy?.ToLowerInvariant() switch
        {
            "category" => sortDescending ? query.OrderByDescending(c => c.CategoryCode) : query.OrderBy(c => c.CategoryCode),
            "meter" or "odometer" => sortDescending ? query.OrderByDescending(c => c.CurrentMeterReadingKm) : query.OrderBy(c => c.CurrentMeterReadingKm),
            "status" => sortDescending ? query.OrderByDescending(c => c.Status) : query.OrderBy(c => c.Status),
            "station" => sortDescending ? query.OrderByDescending(c => c.CurrentStationCode) : query.OrderBy(c => c.CurrentStationCode),
            _ => sortDescending ? query.OrderByDescending(c => c.RegistrationNumber) : query.OrderBy(c => c.RegistrationNumber),
        };
}
