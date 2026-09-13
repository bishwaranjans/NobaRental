using NobaRental.Backend.Domain.Values;

namespace NobaRental.Backend.Domain.Models;

public sealed record RentalBooking(
    long BookingNumber,
    string RegistrationNumber,
    string CustomerSsn,
    CarCategory Category,
    string PickupStationCode,
    DateTimeOffset PickupDateTime,
    long PickupMeterReadingKm,
    string? ReturnStationCode,
    DateTimeOffset? ReturnDateTime,
    long? ReturnMeterReadingKm,
    decimal BaseDayRental,
    decimal BaseKmPrice,
    int? CalculatedDays,
    long? CalculatedKm,
    decimal? TotalPrice,
    string Currency,
    RentalStatus Status,
    byte[]? RowVersion = null);
