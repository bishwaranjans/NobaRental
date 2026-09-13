using NobaRental.Backend.WebApi.Client.Models.Values;

namespace NobaRental.Backend.WebApi.Client.Models.Response;

public sealed record CarResponse(
    string RegistrationNumber,
    CarCategoryDto Category,
    long CurrentMeterReadingKm,
    CarStatusDto Status,
    string CurrentStationCode,
    decimal BaseDayRental,
    decimal BaseKmPrice,
    byte[]? RowVersion = null);
