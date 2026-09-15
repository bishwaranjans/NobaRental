namespace NobaRental.Backend.WebApi.Client.Models.Request;

public sealed record UpdateCarCategoryRequest(
    string Name,
    decimal DayMultiplier,
    decimal KmMultiplier,
    bool ChargesKilometers,
    bool IsActive,
    byte[]? RowVersion = null);
