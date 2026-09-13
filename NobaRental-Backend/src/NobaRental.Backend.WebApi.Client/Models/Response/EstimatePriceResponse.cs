namespace NobaRental.Backend.WebApi.Client.Models.Response;

public sealed record EstimatePriceResponse(
    long BookingNumber,
    int CalculatedDays,
    long CalculatedKm,
    decimal EstimatedPrice,
    string Currency);
