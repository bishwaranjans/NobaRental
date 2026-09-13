using NobaRental.Backend.Data.Entities;
using NobaRental.Backend.Domain.Models;
using NobaRental.Backend.Domain.Values;

namespace NobaRental.Backend.Business.Mapping;

internal static class CarMap
{
    public static Car Map(this CarEntity entity) =>
        new(
            RegistrationNumber: entity.RegistrationNumber,
            Category: entity.Category.ToDomain(),
            CurrentMeterReadingKm: entity.CurrentMeterReadingKm,
            Status: entity.Status.ToDomain(),
            CurrentStationCode: entity.CurrentStationCode,
            BaseDayRental: entity.BaseDayRental,
            BaseKmPrice: entity.BaseKmPrice,
            IsDeleted: entity.IsDeleted,
            RowVersion: entity.RowVersion);

    public static CarEntity MapToEntity(
        string registrationNumber,
        CarCategory category,
        long initialMeterReadingKm,
        string stationCode,
        decimal baseDayRental,
        decimal baseKmPrice = 0m,
        CarStatus status = CarStatus.Available) =>
        new()
        {
            RegistrationNumber = registrationNumber.Trim().ToUpperInvariant(),
            Category = category.ToEntity(),
            CurrentMeterReadingKm = initialMeterReadingKm,
            Status = status.ToEntity(),
            CurrentStationCode = stationCode.Trim().ToUpperInvariant(),
            BaseDayRental = baseDayRental,
            BaseKmPrice = baseKmPrice,
        };
}
