namespace NobaRental.Backend.Domain.Models;

public sealed record RentalPriceBreakdown(
    decimal BaseDayRental,
    decimal BaseKmPrice,
    int NumberOfDays,
    long NumberOfKm,
    decimal DayRentalCost,
    decimal KmRentalCost,
    decimal TotalPrice,
    string Currency = "NOK");
