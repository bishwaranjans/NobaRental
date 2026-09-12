namespace NobaRental.Backend.WebApi.Client.Models.Request;

public sealed record UpdateStationRequest(
    string Name,
    string City,
    bool IsActive,
    byte[]? RowVersion = null);
