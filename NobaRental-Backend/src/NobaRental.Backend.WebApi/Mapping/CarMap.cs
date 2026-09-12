using NobaRental.Backend.Domain.Models;
using NobaRental.Backend.WebApi.Client.Models.Response;
using NobaRental.Backend.WebApi.Client.Models.Values;

namespace NobaRental.Backend.WebApi.Mapping;

internal static class CarMap
{
    public static CarResponse MapToResponse(this Car domain) =>
        new(
            RegistrationNumber: domain.RegistrationNumber,
            Category: (CarCategoryDto)domain.Category,
            CurrentMeterReadingKm: domain.CurrentMeterReadingKm,
            Status: (CarStatusDto)domain.Status,
            CurrentStationCode: domain.CurrentStationCode,
            RowVersion: domain.RowVersion);

    public static IReadOnlyCollection<CarResponse> MapToResponse(this IReadOnlyCollection<Car> items) =>
        items.Select(x => x.MapToResponse()).ToList();

    public static PagedResultResponse<CarResponse> MapToPagedResponse(this PagedResult<Car> paged) =>
        new(
            Items: paged.Items.Select(x => x.MapToResponse()).ToList(),
            TotalCount: paged.TotalCount,
            PageNumber: paged.PageNumber,
            PageSize: paged.PageSize);
}
