using Microsoft.EntityFrameworkCore;
using NobaRental.Backend.Business.Mapping;
using NobaRental.Backend.Data;
using NobaRental.Backend.Domain;
using NobaRental.Backend.Domain.Exceptions;
using NobaRental.Backend.Domain.Models;

namespace NobaRental.Backend.Business.Api;

public class CarCategoryApi(NobaRentalDbContext dbContext) : ICarCategoryApi
{
    public async Task<IReadOnlyCollection<CarCategory>> GetCategories(
        bool? activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.CarCategories.AsNoTracking();

        if (activeOnly == true)
        {
            query = query.Where(c => c.IsActive);
        }

        var entities = await query
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return entities.ConvertAll(c => c.Map());
    }

    public async Task<CarCategory?> GetCategory(string code, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var normalizedCode = code.Trim().ToUpperInvariant();

        var category = await dbContext.CarCategories
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Code == normalizedCode, cancellationToken);

        return category?.Map();
    }

    public async Task<CarCategory> CreateCategory(
        string code,
        string name,
        decimal dayMultiplier,
        decimal kmMultiplier,
        bool chargesKilometers,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(dayMultiplier);
        ArgumentOutOfRangeException.ThrowIfNegative(kmMultiplier);

        var normalizedCode = code.Trim().ToUpperInvariant();

        var existing = await dbContext.CarCategories
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(c => c.Code == normalizedCode, cancellationToken);

        if (existing is not null)
        {
            if (existing.IsDeleted)
            {
                existing.IsDeleted = false;
                existing.DeletedAt = null;
                existing.Name = name.Trim();
                existing.DayMultiplier = dayMultiplier;
                existing.KmMultiplier = kmMultiplier;
                existing.ChargesKilometers = chargesKilometers;
                existing.IsActive = true;
                await dbContext.SaveChangesAsync(cancellationToken);
                return existing.Map();
            }

            throw new DuplicateCategoryException(normalizedCode, $"Category with code '{normalizedCode}' already exists.");
        }

        var entity = CarCategoryMap.MapToEntity(normalizedCode, name, dayMultiplier, kmMultiplier, chargesKilometers);
        dbContext.CarCategories.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return entity.Map();
    }

    public async Task<CarCategory> UpdateCategory(
        string code,
        string name,
        decimal dayMultiplier,
        decimal kmMultiplier,
        bool chargesKilometers,
        bool isActive,
        byte[] rowVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(dayMultiplier);
        ArgumentOutOfRangeException.ThrowIfNegative(kmMultiplier);

        var normalizedCode = code.Trim().ToUpperInvariant();

        var category = await dbContext.CarCategories
            .SingleOrDefaultAsync(c => c.Code == normalizedCode, cancellationToken)
            ?? throw new CategoryNotFoundException(normalizedCode, $"Category with code '{normalizedCode}' was not found.");

        if (rowVersion.Length > 0)
        {
            dbContext.Entry(category).Property(x => x.RowVersion).OriginalValue = rowVersion;
        }

        category.Name = name.Trim();
        category.DayMultiplier = dayMultiplier;
        category.KmMultiplier = kmMultiplier;
        category.ChargesKilometers = chargesKilometers;
        category.IsActive = isActive;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new RentalConcurrencyException($"Category '{normalizedCode}' was modified by another operation.", ex);
        }

        return category.Map();
    }

    public async Task DeleteCategory(string code, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var normalizedCode = code.Trim().ToUpperInvariant();

        var category = await dbContext.CarCategories
            .SingleOrDefaultAsync(c => c.Code == normalizedCode, cancellationToken);

        if (category is null)
        {
            return;
        }

        var vehicleCount = await dbContext.Cars
            .CountAsync(c => c.CategoryCode == normalizedCode, cancellationToken);

        var bookingCount = await dbContext.RentalBookings
            .CountAsync(b => b.CategoryCode == normalizedCode, cancellationToken);

        var totalInUse = vehicleCount + bookingCount;
        if (totalInUse > 0)
        {
            throw new CategoryInUseException(normalizedCode, totalInUse);
        }

        dbContext.CarCategories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
