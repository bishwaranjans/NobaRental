using NobaRental.Backend.Domain.Models;
using NobaRental.Backend.WebApi.Client.Models.Response;

namespace NobaRental.Backend.WebApi.Mapping;

internal static class StationMap
{
    public static StationResponse MapToResponse(this Station domain) =>
        new(
            Code: domain.Code,
            Name: domain.Name,
            City: domain.City,
            IsActive: domain.IsActive,
            RowVersion: domain.RowVersion);

    public static IReadOnlyCollection<StationResponse> MapToResponse(this IReadOnlyCollection<Station> items) =>
        items.Select(x => x.MapToResponse()).ToList();
}
