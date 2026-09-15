using NobaRental.Backend.WebApi.Client.Models.Values;

namespace NobaRental.Backend.WebApi.Client.Models.Response;

public sealed record CarResponse(
    string RegistrationNumber,
    string CategoryCode,
    long CurrentMeterReadingKm,
    CarStatusDto Status,
    string CurrentStationCode,
    decimal BaseDayRental,
    decimal BaseKmPrice,
    byte[]? RowVersion = null,
    string? CategoryName = null);
