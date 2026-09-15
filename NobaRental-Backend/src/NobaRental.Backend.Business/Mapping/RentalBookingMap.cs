using NobaRental.Backend.Business.Pricing;
using NobaRental.Backend.Data.Entities;
using NobaRental.Backend.Data.Entities.Values;
using NobaRental.Backend.Domain.Models;
using NobaRental.Backend.Domain.Values;

namespace NobaRental.Backend.Business.Mapping;

internal static class RentalBookingMap
{
    public static RentalBooking Map(this RentalBookingEntity entity) =>
        new(
            BookingNumber: entity.BookingNumber,
            RegistrationNumber: entity.RegistrationNumber,
            CustomerSsn: entity.CustomerSsn,
            CategoryCode: entity.CategoryCode,
            PickupStationCode: entity.PickupStationCode,
            PickupDateTime: entity.PickupDateTime,
            PickupMeterReadingKm: entity.PickupMeterReadingKm,
            ReturnStationCode: entity.ReturnStationCode,
            ReturnDateTime: entity.ReturnDateTime,
            ReturnMeterReadingKm: entity.ReturnMeterReadingKm,
            BaseDayRental: entity.BaseDayRental,
            BaseKmPrice: entity.BaseKmPrice,
            CalculatedDays: entity.ReturnDateTime.HasValue
                ? RentalDurationCalculator.CalculateBilledDays(entity.PickupDateTime, entity.ReturnDateTime.Value)
                : null,
            CalculatedKm: entity.ReturnMeterReadingKm.HasValue
                ? RentalDurationCalculator.CalculateKilometers(entity.PickupMeterReadingKm, entity.ReturnMeterReadingKm.Value)
                : null,
            TotalPrice: entity.TotalPrice,
            Currency: entity.Currency,
            Status: entity.Status.ToDomain(),
            RowVersion: entity.RowVersion,
            AppliedDayMultiplier: entity.AppliedDayMultiplier,
            AppliedKmMultiplier: entity.AppliedKmMultiplier,
            CategoryName: entity.Category?.Name);

    public static RentalBookingEntity MapToEntity(
        string registrationNumber,
        string customerSsn,
        string categoryCode,
        decimal appliedDayMultiplier,
        decimal appliedKmMultiplier,
        string pickupStationCode,
        DateTimeOffset pickupDateTime,
        long pickupMeterReadingKm,
        decimal baseDayRental,
        decimal baseKmPrice,
        string currency = "NOK") =>
        new()
        {
            RegistrationNumber = registrationNumber,
            CustomerSsn = customerSsn,
            CategoryCode = categoryCode.Trim().ToUpperInvariant(),
            AppliedDayMultiplier = appliedDayMultiplier,
            AppliedKmMultiplier = appliedKmMultiplier,
            PickupStationCode = pickupStationCode.Trim().ToUpperInvariant(),
            PickupDateTime = pickupDateTime,
            PickupMeterReadingKm = pickupMeterReadingKm,
            BaseDayRental = baseDayRental,
            BaseKmPrice = baseKmPrice,
            Currency = currency,
            Status = RentalStatusValue.Active,
        };
}
