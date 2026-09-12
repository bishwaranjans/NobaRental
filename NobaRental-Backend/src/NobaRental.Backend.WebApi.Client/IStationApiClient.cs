using NobaRental.Backend.WebApi.Client.Models.Request;
using NobaRental.Backend.WebApi.Client.Models.Response;
using RestEase;

namespace NobaRental.Backend.WebApi.Client;

public interface IStationApiClient
{
    [Get("/api/v1/stations")]
    Task<IReadOnlyCollection<StationResponse>> GetAllStationsAsync(
        [Query] bool? includeInactive = null,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/stations/{code}")]
    Task<StationResponse> GetStationByCodeAsync(
        [Path] string code,
        CancellationToken cancellationToken = default);

    [Post("/api/v1/stations")]
    Task<StationResponse> CreateStationAsync(
        [Body] CreateStationRequest request,
        CancellationToken cancellationToken = default);

    [Put("/api/v1/stations/{code}")]
    Task<StationResponse> UpdateStationAsync(
        [Path] string code,
        [Body] UpdateStationRequest request,
        CancellationToken cancellationToken = default);

    [Delete("/api/v1/stations/{code}")]
    Task DeleteStationAsync(
        [Path] string code,
        CancellationToken cancellationToken = default);
}
