using NobaRental.Backend.Domain.Values;

namespace NobaRental.Backend.Domain.Models;

public sealed record Car(
    string RegistrationNumber,
    string CategoryCode,
    long CurrentMeterReadingKm,
    CarStatus Status,
    string CurrentStationCode,
    decimal BaseDayRental = 0m,
    decimal BaseKmPrice = 0m,
    bool IsDeleted = false,
    byte[]? RowVersion = null,
    string? CategoryName = null);
