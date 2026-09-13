namespace NobaRental.Backend.WebApi.Client.Models.Request;

public sealed record EstimatePriceRequest(
    long BookingNumber,
    DateTimeOffset ReturnDateTime,
    long ReturnMeterReadingKm);
