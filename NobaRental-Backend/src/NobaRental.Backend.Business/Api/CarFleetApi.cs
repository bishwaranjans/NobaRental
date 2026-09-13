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
        CarCategory category,
        long initialMeterReadingKm,
        string stationCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registrationNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(stationCode);

        if (initialMeterReadingKm < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialMeterReadingKm), "Initial meter reading cannot be negative.");
        }

        var normalizedReg = registrationNumber.Trim().ToUpperInvariant();
        var normalizedStation = stationCode.Trim().ToUpperInvariant();

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
                existing.IsDeleted = false;
                existing.DeletedAt = null;
                existing.Category = category.ToEntity();
                existing.CurrentMeterReadingKm = initialMeterReadingKm;
                existing.CurrentStationCode = normalizedStation;
                existing.Status = CarStatusValue.Available;

                await dbContext.SaveChangesAsync(cancellationToken);

                return existing.Map();
            }

            throw new InvalidRentalOperationException($"Car with registration number '{normalizedReg}' already exists.");
        }

        var entity = CarMap.MapToEntity(normalizedReg, category, initialMeterReadingKm, normalizedStation, status: CarStatus.Available);
        await dbContext.Cars.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return entity.Map();
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

    public async Task<IReadOnlyCollection<Car>> GetAllCars(CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.Cars
            .AsNoTracking()
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
        var query = dbContext.Cars.AsNoTracking();

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
        CarCategory? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Cars
            .AsNoTracking()
            .Where(c => c.Status == CarStatusValue.Available);

        if (!string.IsNullOrWhiteSpace(stationCode))
        {
            var normalizedStation = stationCode.Trim().ToUpperInvariant();
            query = query.Where(c => c.CurrentStationCode == normalizedStation);
        }

        if (category.HasValue)
        {
            var catVal = category.Value.ToEntity();
            query = query.Where(c => c.Category == catVal);
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
            .FirstOrDefaultAsync(c => c.RegistrationNumber == normalizedReg, cancellationToken);

        return entity?.Map();
    }

    private static IQueryable<CarEntity> ApplySorting(IQueryable<CarEntity> query, string? sortBy, bool sortDescending) =>
        sortBy?.ToLowerInvariant() switch
        {
            "category" => sortDescending ? query.OrderByDescending(c => c.Category) : query.OrderBy(c => c.Category),
            "meter" or "odometer" => sortDescending ? query.OrderByDescending(c => c.CurrentMeterReadingKm) : query.OrderBy(c => c.CurrentMeterReadingKm),
            "status" => sortDescending ? query.OrderByDescending(c => c.Status) : query.OrderBy(c => c.Status),
            "station" => sortDescending ? query.OrderByDescending(c => c.CurrentStationCode) : query.OrderBy(c => c.CurrentStationCode),
            _ => sortDescending ? query.OrderByDescending(c => c.RegistrationNumber) : query.OrderBy(c => c.RegistrationNumber),
        };
}
