namespace NobaRental.Backend.WebApi.Client.Models.Request;

public sealed record RegisterCarRequest(
    string RegistrationNumber,
    string CategoryCode,
    long InitialMeterReadingKm,
    string StationCode,
    decimal BaseDayRental,
    decimal BaseKmPrice = 0m);
