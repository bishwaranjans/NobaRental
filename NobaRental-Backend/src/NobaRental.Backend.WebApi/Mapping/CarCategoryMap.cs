using NobaRental.Backend.Domain.Models;
using NobaRental.Backend.WebApi.Client.Models.Response;

namespace NobaRental.Backend.WebApi.Mapping;

internal static class CarCategoryMap
{
    public static CarCategoryResponse MapToResponse(this CarCategory domain) =>
        new(
            Code: domain.Code,
            Name: domain.Name,
            DayMultiplier: domain.DayMultiplier,
            KmMultiplier: domain.KmMultiplier,
            ChargesKilometers: domain.ChargesKilometers,
            IsActive: domain.IsActive,
            RowVersion: domain.RowVersion);

    public static IReadOnlyCollection<CarCategoryResponse> MapToResponse(this IEnumerable<CarCategory> items) =>
        items.Select(x => x.MapToResponse()).ToList();
}
