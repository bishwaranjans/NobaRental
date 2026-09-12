using NobaRental.Backend.Domain.Values;

namespace NobaRental.Backend.Domain.Models;

public sealed record Car(
    string RegistrationNumber,
    CarCategory Category,
    long CurrentMeterReadingKm,
    CarStatus Status,
    string CurrentStationCode,
    bool IsDeleted = false,
    byte[]? RowVersion = null);
