namespace NobaRental.Backend.WebApi.Client.Models.Response;

public sealed record CarCategoryResponse(
    string Code,
    string Name,
    decimal DayMultiplier,
    decimal KmMultiplier,
    bool ChargesKilometers,
    bool IsActive,
    byte[]? RowVersion = null);
