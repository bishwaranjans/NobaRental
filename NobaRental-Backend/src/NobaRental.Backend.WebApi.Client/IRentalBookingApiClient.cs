using NobaRental.Backend.WebApi.Client.Models.Request;
using NobaRental.Backend.WebApi.Client.Models.Response;
using NobaRental.Backend.WebApi.Client.Models.Values;
using RestEase;

namespace NobaRental.Backend.WebApi.Client;

public interface IRentalBookingApiClient
{
    [Post("api/v1/rentals/pickup")]
    [AllowAnyStatusCode]
    Task<Response<RentalBookingResponse>> RegisterPickup([Body] RegisterPickupRequest request, CancellationToken cancellationToken = default);

    [Post("api/v1/rentals/{bookingNumber}/return")]
    [AllowAnyStatusCode]
    Task<Response<RentalBookingResponse>> ReturnBooking(
        [Path] long bookingNumber,
        [Body] ReturnRentalRequest request,
        [Header("If-Match")] string? ifMatch = null,
        CancellationToken cancellationToken = default);

    [Get("api/v1/rentals/{bookingNumber}")]
    [AllowAnyStatusCode]
    Task<Response<RentalBookingResponse>> GetBookingByNumber([Path] long bookingNumber, CancellationToken cancellationToken = default);

    [Get("api/v1/rentals")]
    [AllowAnyStatusCode]
    Task<Response<PagedResultResponse<RentalBookingResponse>>> GetBookings(
        [Query] int? pageNumber = null,
        [Query] int? pageSize = null,
        [Query] string? searchTerm = null,
        [Query] string? stationCode = null,
        [Query] RentalStatusDto? status = null,
        [Query] string? sortBy = null,
        [Query] bool? sortDescending = null,
        CancellationToken cancellationToken = default);

    [Get("api/v1/rentals/active")]
    [AllowAnyStatusCode]
    Task<Response<IReadOnlyCollection<RentalBookingResponse>>> GetActiveBookings(CancellationToken cancellationToken = default);

    [Get("health")]
    [AllowAnyStatusCode]
    Task<Response<string>> Status(CancellationToken cancellationToken = default);

    [Post("api/v1/rentals/estimate-price")]
    [AllowAnyStatusCode]
    Task<Response<EstimatePriceResponse>> EstimatePrice([Body] EstimatePriceRequest request, CancellationToken cancellationToken = default);
}
