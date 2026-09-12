namespace NobaRental.Backend.WebApi.Client.Models.Request;

public sealed record RegisterReturnRequest(
    long BookingNumber,
    string ReturnStationCode,
    DateTimeOffset ReturnDateTime,
    long ReturnMeterReadingKm,
    byte[]? RowVersion = null);
