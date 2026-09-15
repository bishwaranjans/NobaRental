namespace NobaRental.Backend.Domain.Models;

public sealed record CarCategory(
    string Code,
    string Name,
    decimal DayMultiplier,
    decimal KmMultiplier,
    bool ChargesKilometers,
    bool IsActive,
    bool IsDeleted = false,
    byte[]? RowVersion = null);
