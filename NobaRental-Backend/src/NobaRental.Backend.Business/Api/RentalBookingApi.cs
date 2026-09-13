using Microsoft.EntityFrameworkCore;
using NobaRental.Backend.Business.Mapping;
using NobaRental.Backend.Business.Pricing;
using NobaRental.Backend.Data;
using NobaRental.Backend.Data.Entities;
using NobaRental.Backend.Data.Entities.Values;
using NobaRental.Backend.Domain;
using NobaRental.Backend.Domain.Exceptions;
using NobaRental.Backend.Domain.Models;
using NobaRental.Backend.Domain.Values;

namespace NobaRental.Backend.Business.Api;

public class RentalBookingApi(
    NobaRentalDbContext ctx,
    IRentalPriceCalculator priceCalculator) : IRentalBookingApi
{
    public async Task<RentalBooking> RegisterPickup(
        string registrationNumber,
        string customerSsn,
        CarCategory category,
        string pickupStationCode,
        DateTimeOffset pickupDateTime,
        long pickupMeterReadingKm,
        CancellationToken cancellationToken = default)
    {
        ValidatePickupArguments(registrationNumber, customerSsn, pickupStationCode, pickupMeterReadingKm);

        var normalizedReg = registrationNumber.Trim().ToUpperInvariant();
        var normalizedStation = pickupStationCode.Trim().ToUpperInvariant();

        var station = await ctx.Stations.SingleOrDefaultAsync(s => s.Code == normalizedStation, cancellationToken);
        if (station?.IsActive != true)
        {
            throw new InvalidRentalOperationException($"Pickup station '{normalizedStation}' does not exist or is inactive.");
        }

        var car = await ctx.Cars.FirstOrDefaultAsync(x => x.RegistrationNumber == normalizedReg, cancellationToken);
        ValidateCarForPickup(car, normalizedReg, category, normalizedStation, pickupMeterReadingKm);

        car!.Status = CarStatusValue.Rented;

        var entity = RentalBookingMap.MapToEntity(
            registrationNumber: normalizedReg,
            customerSsn: customerSsn.Trim(),
            category: category,
            pickupStationCode: normalizedStation,
            pickupDateTime: pickupDateTime,
            pickupMeterReadingKm: pickupMeterReadingKm,
            baseDayRental: car.BaseDayRental,
            baseKmPrice: car.BaseKmPrice);

        await ctx.RentalBookings.AddAsync(entity, cancellationToken);

        try
        {
            await ctx.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new RentalConcurrencyException($"Car '{normalizedReg}' was rented or modified by another concurrent operation.", ex);
        }
        catch (DbUpdateException ex) when (IsActiveRentalConflict(ex))
        {
            throw new RentalConcurrencyException($"Car '{normalizedReg}' could not be rented due to a concurrent update or active rental conflict.", ex);
        }

        return entity.Map();
    }

    public async Task<RentalBooking> RegisterReturn(
        long bookingNumber,
        string returnStationCode,
        DateTimeOffset returnDateTime,
        long returnMeterReadingKm,
        byte[]? rowVersion = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bookingNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(returnStationCode);

        var normalizedStation = returnStationCode.Trim().ToUpperInvariant();
        var station = await ctx.Stations.SingleOrDefaultAsync(s => s.Code == normalizedStation, cancellationToken);
        if (station?.IsActive != true)
        {
            throw new InvalidRentalOperationException($"Return station '{normalizedStation}' does not exist or is inactive.");
        }

        var entity = await ctx.RentalBookings.SingleOrDefaultAsync(x => x.BookingNumber == bookingNumber, cancellationToken);
        if (entity is null)
            throw new BookingNotFoundException(bookingNumber);

        if (entity.Status == RentalStatusValue.Completed)
            throw new InvalidRentalOperationException($"Booking '{bookingNumber}' has already been returned and completed.");

        if (rowVersion?.Length > 0)
        {
            ctx.Entry(entity).Property(x => x.RowVersion).OriginalValue = rowVersion;
        }

        CalculateReturnValues(entity, normalizedStation, returnDateTime, returnMeterReadingKm);

        var car = await ctx.Cars.FirstOrDefaultAsync(x => x.RegistrationNumber == entity.RegistrationNumber, cancellationToken);
        if (car is not null)
        {
            car.CurrentMeterReadingKm = returnMeterReadingKm;
            car.CurrentStationCode = normalizedStation;
            car.Status = CarStatusValue.Available;
        }

        try
        {
            await ctx.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new RentalConcurrencyException($"Booking '{bookingNumber}' was modified by another operation.", ex);
        }

        return entity.Map();
    }

    public async Task<RentalBooking?> GetBookingByNumber(long bookingNumber, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bookingNumber);

        var entity = await ctx.RentalBookings
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.BookingNumber == bookingNumber, cancellationToken);

        return entity?.Map();
    }

    public async Task<IReadOnlyCollection<RentalBooking>> GetAllBookings(CancellationToken cancellationToken = default)
    {
        var entities = await ctx.RentalBookings
            .AsNoTracking()
            .OrderByDescending(x => x.PickupDateTime)
            .ToListAsync(cancellationToken);

        return entities.ConvertAll(x => x.Map());
    }

    public async Task<PagedResult<RentalBooking>> GetBookings(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? stationCode = null,
        RentalStatus? status = null,
        string? sortBy = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var query = ctx.RentalBookings.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToUpperInvariant();
            query = query.Where(b => b.RegistrationNumber.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(stationCode))
        {
            var station = stationCode.Trim().ToUpperInvariant();
            query = query.Where(b => b.PickupStationCode == station || b.ReturnStationCode == station);
        }

        if (status.HasValue)
        {
            var statusVal = status.Value.ToEntity();
            query = query.Where(b => b.Status == statusVal);
        }

        query = ApplyBookingSorting(query, sortBy, sortDescending);

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, pageNumber);
        var size = Math.Max(1, pageSize);

        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(b => b.Map())
            .ToListAsync(cancellationToken);

        return new PagedResult<RentalBooking>(items, totalCount, page, size);
    }

    public async Task<IReadOnlyCollection<RentalBooking>> GetActiveBookings(CancellationToken cancellationToken = default)
    {
        var entities = await ctx.RentalBookings
            .AsNoTracking()
            .Where(x => x.Status == RentalStatusValue.Active)
            .OrderByDescending(x => x.PickupDateTime)
            .ToListAsync(cancellationToken);

        return entities.ConvertAll(x => x.Map());
    }

    public async Task<RentalPriceEstimate> EstimatePrice(
        long bookingNumber,
        DateTimeOffset returnDateTime,
        long returnMeterReadingKm,
        CancellationToken cancellationToken = default)
    {
        var entity = await ctx.RentalBookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BookingNumber == bookingNumber, cancellationToken)
            ?? throw new BookingNotFoundException(bookingNumber);

        var billedDays = RentalDurationCalculator.CalculateBilledDays(entity.PickupDateTime, returnDateTime);
        var drivenKm = RentalDurationCalculator.CalculateKilometers(entity.PickupMeterReadingKm, returnMeterReadingKm);

        var price = priceCalculator.CalculatePrice(
            category: entity.Category.ToDomain(),
            baseDayRental: entity.BaseDayRental,
            baseKmPrice: entity.BaseKmPrice,
            numberOfDays: billedDays,
            numberOfKm: drivenKm);

        return new RentalPriceEstimate(
            BookingNumber: bookingNumber,
            CalculatedDays: billedDays,
            CalculatedKm: drivenKm,
            EstimatedPrice: price,
            Currency: entity.Currency);
    }

    private static void ValidatePickupArguments(string reg, string ssn, string station, long meter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reg);
        ArgumentException.ThrowIfNullOrWhiteSpace(ssn);
        ArgumentException.ThrowIfNullOrWhiteSpace(station);
        ArgumentOutOfRangeException.ThrowIfNegative(meter);
    }

    private static void ValidateCarForPickup(CarEntity? car, string reg, CarCategory category, string station, long meter)
    {
        if (car is null)
            throw new InvalidRentalOperationException($"Car '{reg}' is not registered in the fleet.");

        if (car.Status != CarStatusValue.Available)
            throw new InvalidRentalOperationException($"Car '{reg}' is not available for rental (current status: {car.Status}).");

        if (car.Category != category.ToEntity())
            throw new InvalidRentalOperationException($"Car '{reg}' category '{car.Category}' does not match requested category '{category}'.");

        if (!string.Equals(car.CurrentStationCode, station, StringComparison.OrdinalIgnoreCase))
            throw new InvalidRentalOperationException($"Car '{reg}' is stationed at '{car.CurrentStationCode}', not at pickup station '{station}'.");

        if (meter < car.CurrentMeterReadingKm)
            throw new InvalidRentalOperationException($"Pickup meter reading ({meter} km) cannot be less than car's current meter reading ({car.CurrentMeterReadingKm} km).");
    }

    private void CalculateReturnValues(RentalBookingEntity entity, string returnStation, DateTimeOffset returnDate, long returnMeter)
    {
        var billedDays = RentalDurationCalculator.CalculateBilledDays(entity.PickupDateTime, returnDate);
        var drivenKm = RentalDurationCalculator.CalculateKilometers(entity.PickupMeterReadingKm, returnMeter);

        var totalPrice = priceCalculator.CalculatePrice(
            category: entity.Category.ToDomain(),
            baseDayRental: entity.BaseDayRental,
            baseKmPrice: entity.BaseKmPrice,
            numberOfDays: billedDays,
            numberOfKm: drivenKm);

        entity.ReturnStationCode = returnStation;
        entity.ReturnDateTime = returnDate;
        entity.ReturnMeterReadingKm = returnMeter;
        entity.TotalPrice = totalPrice;
        entity.Status = RentalStatusValue.Completed;
    }

    private static IQueryable<RentalBookingEntity> ApplyBookingSorting(IQueryable<RentalBookingEntity> query, string? sortBy, bool sortDescending) =>
        sortBy?.ToLowerInvariant() switch
        {
            "bookingnumber" => sortDescending ? query.OrderByDescending(b => b.BookingNumber) : query.OrderBy(b => b.BookingNumber),
            "car" or "registrationnumber" => sortDescending ? query.OrderByDescending(b => b.RegistrationNumber) : query.OrderBy(b => b.RegistrationNumber),
            "status" => sortDescending ? query.OrderByDescending(b => b.Status) : query.OrderBy(b => b.Status),
            "totalprice" => sortDescending ? query.OrderByDescending(b => b.TotalPrice) : query.OrderBy(b => b.TotalPrice),
            _ => sortDescending ? query.OrderByDescending(b => b.PickupDateTime) : query.OrderBy(b => b.PickupDateTime),
        };

    private static bool IsActiveRentalConflict(DbUpdateException exception) =>
        exception.InnerException is Microsoft.Data.SqlClient.SqlException sqlException &&
        sqlException.Errors
            .Cast<Microsoft.Data.SqlClient.SqlError>()
            .Any(error => error.Number is 2601 or 2627 &&
                error.Message.Contains("IX_RentalBooking_RegistrationNumber", StringComparison.OrdinalIgnoreCase));
}
