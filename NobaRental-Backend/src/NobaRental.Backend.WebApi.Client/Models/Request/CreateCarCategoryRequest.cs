namespace NobaRental.Backend.WebApi.Client.Models.Request;

public sealed record CreateCarCategoryRequest(
    string Code,
    string Name,
    decimal DayMultiplier,
    decimal KmMultiplier,
    bool ChargesKilometers);
