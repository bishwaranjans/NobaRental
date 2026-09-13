using NobaRental.Backend.WebApi.Client.Models.Values;

namespace NobaRental.Backend.WebApi.Client.Models.Request;

public sealed record RegisterPickupRequest(
    string RegistrationNumber,
    string CustomerSsn,
    CarCategoryDto Category,
    string PickupStationCode,
    DateTimeOffset PickupDateTime,
    long PickupMeterReadingKm,
    string Currency = "NOK");
