using Microsoft.EntityFrameworkCore;
using NobaRental.Backend.Business.Mapping;
using NobaRental.Backend.Data;
using NobaRental.Backend.Data.Entities.Values;
using NobaRental.Backend.Domain;
using NobaRental.Backend.Domain.Exceptions;
using NobaRental.Backend.Domain.Models;

namespace NobaRental.Backend.Business.Api;

public class StationApi(NobaRentalDbContext dbContext) : IStationApi
{
    private readonly NobaRentalDbContext _dbContext = dbContext;

    public async Task<Station> CreateStation(
        string code,
        string name,
        string city,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();

        var existing = await _dbContext.Stations
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(s => s.Code == normalizedCode, cancellationToken);

        if (existing is not null)
        {
            if (existing.IsDeleted)
            {
                existing.IsDeleted = false;
                existing.DeletedAt = null;
                existing.Name = name.Trim();
                existing.City = city.Trim();
                existing.IsActive = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
                return existing.Map();
            }

            throw new InvalidRentalOperationException($"Station with code '{normalizedCode}' already exists.");
        }

        var entity = StationMap.MapToEntity(normalizedCode, name, city);
        _dbContext.Stations.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return entity.Map();
    }

    public async Task<Station> UpdateStation(
        string code,
        string name,
        string city,
        bool isActive,
        byte[]? rowVersion = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();

        var station = await _dbContext.Stations
            .SingleOrDefaultAsync(s => s.Code == normalizedCode, cancellationToken);

        if (station is null)
        {
            throw new InvalidRentalOperationException($"Station with code '{normalizedCode}' was not found.");
        }

        if (rowVersion is not null && rowVersion.Length > 0)
        {
            _dbContext.Entry(station).Property(x => x.RowVersion).OriginalValue = rowVersion;
        }

        station.Name = name.Trim();
        station.City = city.Trim();
        station.IsActive = isActive;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new RentalConcurrencyException($"Station '{normalizedCode}' was modified by another operation.", ex);
        }

        return station.Map();
    }

    public async Task DeleteStation(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();

        var station = await _dbContext.Stations
            .SingleOrDefaultAsync(s => s.Code == normalizedCode, cancellationToken);

        if (station is null)
        {
            return;
        }

        var hasVehicles = await _dbContext.Cars
            .AnyAsync(c => c.CurrentStationCode == normalizedCode, cancellationToken);

        if (hasVehicles)
        {
            throw new StationInUseException(normalizedCode, $"Cannot delete station '{normalizedCode}' because vehicles are currently stationed there.");
        }

        var hasActiveBookings = await _dbContext.RentalBookings
            .AnyAsync(b => b.Status == RentalStatusValue.Active && (b.PickupStationCode == normalizedCode || b.ReturnStationCode == normalizedCode), cancellationToken);

        if (hasActiveBookings)
        {
            throw new StationInUseException(normalizedCode, $"Cannot delete station '{normalizedCode}' because active rental bookings are linked to it.");
        }

        _dbContext.Stations.Remove(station);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Station>> GetAllStations(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Stations.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        var stations = await query
            .OrderBy(s => s.City)
            .ThenBy(s => s.Name)
            .Select(s => s.Map())
            .ToListAsync(cancellationToken);

        return stations;
    }

    public async Task<Station?> GetStationByCode(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();

        var station = await _dbContext.Stations
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Code == normalizedCode, cancellationToken);

        return station?.Map();
    }
}
