namespace NobaRental.Backend.WebApi.Client.Models.Request;

public sealed record RegisterPickupRequest(
    string RegistrationNumber,
    string CustomerSsn,
    string CategoryCode,
    string PickupStationCode,
    DateTimeOffset PickupDateTime,
    long PickupMeterReadingKm);
