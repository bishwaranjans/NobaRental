namespace NobaRental.Backend.WebApi.Client.Models.Request;

public sealed record ReturnRentalRequest(
    string ReturnStationCode,
    DateTimeOffset ReturnDateTime,
    long ReturnMeterReadingKm,
    byte[]? RowVersion = null);
