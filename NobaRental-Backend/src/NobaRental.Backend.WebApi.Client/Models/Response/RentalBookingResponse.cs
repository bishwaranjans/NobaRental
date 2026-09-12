using NobaRental.Backend.WebApi.Client.Models.Values;

namespace NobaRental.Backend.WebApi.Client.Models.Response;

public sealed record RentalBookingResponse(
    long BookingNumber,
    string RegistrationNumber,
    string CustomerSsn,
    CarCategoryDto Category,
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
    RentalStatusDto Status,
    byte[]? RowVersion = null);
