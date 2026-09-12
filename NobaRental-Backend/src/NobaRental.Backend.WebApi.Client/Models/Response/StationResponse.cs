namespace NobaRental.Backend.WebApi.Client.Models.Response;

public sealed record StationResponse(
    string Code,
    string Name,
    string City,
    bool IsActive,
    byte[]? RowVersion = null);
