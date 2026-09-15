using NobaRental.Backend.Domain.Models;
using NobaRental.Backend.WebApi.Client.Models.Response;

namespace NobaRental.Backend.WebApi.Mapping;

internal static class CarMap
{
    public static CarResponse MapToResponse(this Car domain) =>
        new(
            RegistrationNumber: domain.RegistrationNumber,
            CategoryCode: domain.CategoryCode,
            CurrentMeterReadingKm: domain.CurrentMeterReadingKm,
            Status: domain.Status.MapToResponse(),
            CurrentStationCode: domain.CurrentStationCode,
            BaseDayRental: domain.BaseDayRental,
            BaseKmPrice: domain.BaseKmPrice,
            RowVersion: domain.RowVersion,
            CategoryName: domain.CategoryName);

    public static IReadOnlyCollection<CarResponse> MapToResponse(this IReadOnlyCollection<Car> items) =>
        items.Select(x => x.MapToResponse()).ToList();

    public static PagedResultResponse<CarResponse> MapToPagedResponse(this PagedResult<Car> paged) =>
        new(
            Items: paged.Items.Select(x => x.MapToResponse()).ToList(),
            TotalCount: paged.TotalCount,
            PageNumber: paged.PageNumber,
            PageSize: paged.PageSize);
}
