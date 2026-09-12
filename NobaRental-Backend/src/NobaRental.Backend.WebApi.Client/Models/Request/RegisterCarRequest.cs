using NobaRental.Backend.WebApi.Client.Models.Values;

namespace NobaRental.Backend.WebApi.Client.Models.Request;

public sealed record RegisterCarRequest(
    string RegistrationNumber,
    CarCategoryDto Category,
    long InitialMeterReadingKm,
    string StationCode);
