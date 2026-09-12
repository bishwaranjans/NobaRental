namespace NobaRental.Backend.WebApi.Client.Models.Request;

public sealed record CreateStationRequest(
    string Code,
    string Name,
    string City);
