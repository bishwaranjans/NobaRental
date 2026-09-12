using NobaRental.Backend.Data.Entities;
using NobaRental.Backend.Data.Entities.Values;
using NobaRental.Backend.Domain.Models;
using NobaRental.Backend.Domain.Values;

namespace NobaRental.Backend.Business.Mapping;

internal static class RentalBookingMap
{
    public static RentalBooking Map(this RentalBookingEntity entity, RentalPriceBreakdown? priceBreakdown = null) =>
        new(
            BookingNumber: entity.BookingNumber,
            RegistrationNumber: entity.RegistrationNumber,
            CustomerSsn: entity.CustomerSsn,
            Category: (CarCategory)entity.Category,
            PickupStationCode: entity.PickupStationCode,
            PickupDateTime: entity.PickupDateTime,
            PickupMeterReadingKm: entity.PickupMeterReadingKm,
            ReturnStationCode: entity.ReturnStationCode,
            ReturnDateTime: entity.ReturnDateTime,
            ReturnMeterReadingKm: entity.ReturnMeterReadingKm,
            BaseDayRental: entity.BaseDayRental,
            BaseKmPrice: entity.BaseKmPrice,
            CalculatedDays: entity.CalculatedDays,
            CalculatedKm: entity.CalculatedKm,
            TotalPrice: entity.TotalPrice,
            Currency: entity.Currency,
            Status: (RentalStatus)entity.Status,
            PriceBreakdown: priceBreakdown,
            RowVersion: entity.RowVersion);

    public static RentalBookingEntity MapToEntity(
        string registrationNumber,
        string customerSsn,
        CarCategory category,
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
            Category = (CarCategoryValue)category,
            PickupStationCode = pickupStationCode.Trim().ToUpperInvariant(),
            PickupDateTime = pickupDateTime,
            PickupMeterReadingKm = pickupMeterReadingKm,
            BaseDayRental = baseDayRental,
            BaseKmPrice = baseKmPrice,
            Currency = currency,
            Status = RentalStatusValue.Active,
        };
}
