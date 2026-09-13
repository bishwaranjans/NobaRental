using NobaRental.Backend.WebApi.Client.Models.Values;

namespace NobaRental.Backend.WebApi.Client.Models.Request;

public sealed record RegisterCarRequest(
    string RegistrationNumber,
    CarCategoryDto Category,
    long InitialMeterReadingKm,
    string StationCode,
    decimal BaseDayRental,
    decimal BaseKmPrice = 0m);
