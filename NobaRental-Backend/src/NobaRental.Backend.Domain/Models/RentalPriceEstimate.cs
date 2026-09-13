namespace NobaRental.Backend.Domain.Models;

public sealed record RentalPriceEstimate(
    long BookingNumber,
    int CalculatedDays,
    long CalculatedKm,
    decimal EstimatedPrice,
    string Currency);
