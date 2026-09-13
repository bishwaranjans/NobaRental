using NobaRental.Backend.Domain.Models;
using NobaRental.Backend.WebApi.Client.Models.Response;

namespace NobaRental.Backend.WebApi.Mapping;

internal static class RentalBookingMap
{
    public static RentalBookingResponse MapToResponse(this RentalBooking domain) =>
        new(
            BookingNumber: domain.BookingNumber,
            RegistrationNumber: domain.RegistrationNumber,
            CustomerSsn: domain.CustomerSsn,
            Category: domain.Category.MapToResponse(),
            PickupStationCode: domain.PickupStationCode,
            PickupDateTime: domain.PickupDateTime,
            PickupMeterReadingKm: domain.PickupMeterReadingKm,
            ReturnStationCode: domain.ReturnStationCode,
            ReturnDateTime: domain.ReturnDateTime,
            ReturnMeterReadingKm: domain.ReturnMeterReadingKm,
            BaseDayRental: domain.BaseDayRental,
            BaseKmPrice: domain.BaseKmPrice,
            CalculatedDays: domain.CalculatedDays,
            CalculatedKm: domain.CalculatedKm,
            TotalPrice: domain.TotalPrice,
            Currency: domain.Currency,
            Status: domain.Status.MapToResponse(),
            RowVersion: domain.RowVersion);

    public static IReadOnlyCollection<RentalBookingResponse> MapToResponse(this IReadOnlyCollection<RentalBooking> items) =>
        items.Select(x => x.MapToResponse()).ToList();

    public static PagedResultResponse<RentalBookingResponse> MapToPagedResponse(this PagedResult<RentalBooking> paged) =>
        new(
            Items: paged.Items.Select(x => x.MapToResponse()).ToList(),
            TotalCount: paged.TotalCount,
            PageNumber: paged.PageNumber,
            PageSize: paged.PageSize);
}
