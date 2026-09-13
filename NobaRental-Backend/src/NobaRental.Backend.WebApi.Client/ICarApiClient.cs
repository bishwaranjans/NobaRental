using NobaRental.Backend.WebApi.Client.Models.Request;
using NobaRental.Backend.WebApi.Client.Models.Response;
using NobaRental.Backend.WebApi.Client.Models.Values;
using RestEase;

namespace NobaRental.Backend.WebApi.Client;

public interface ICarApiClient
{
    [Post("api/v1/cars")]
    [AllowAnyStatusCode]
    Task<Response<CarResponse>> RegisterCar([Body] RegisterCarRequest request, CancellationToken cancellationToken = default);

    [Put("api/v1/cars/{registrationNumber}/tariff")]
    [AllowAnyStatusCode]
    Task<Response<CarResponse>> UpdateCarTariff(
        [Path] string registrationNumber,
        [Body] UpdateCarTariffRequest request,
        [Header("If-Match")] string? ifMatch = null,
        CancellationToken cancellationToken = default);

    [Delete("api/v1/cars/{registrationNumber}")]
    [AllowAnyStatusCode]
    Task<Response<string>> DeleteCar([Path] string registrationNumber, CancellationToken cancellationToken = default);

    [Get("api/v1/cars")]
    [AllowAnyStatusCode]
    Task<Response<PagedResultResponse<CarResponse>>> GetCars(
        [Query] int? pageNumber = null,
        [Query] int? pageSize = null,
        [Query] string? searchTerm = null,
        [Query] string? stationCode = null,
        [Query] CarStatusDto? status = null,
        [Query] string? sortBy = null,
        [Query] bool? sortDescending = null,
        CancellationToken cancellationToken = default);

    [Get("api/v1/cars/available")]
    [AllowAnyStatusCode]
    Task<Response<IReadOnlyCollection<CarResponse>>> GetAvailableCars(
        [Query] string? stationCode = null,
        [Query] CarCategoryDto? category = null,
        CancellationToken cancellationToken = default);

    [Get("api/v1/cars/{registrationNumber}")]
    [AllowAnyStatusCode]
    Task<Response<CarResponse>> GetCarByRegistrationNumber([Path] string registrationNumber, CancellationToken cancellationToken = default);
}
