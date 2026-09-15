using NobaRental.Backend.Domain.Models;
using NobaRental.Backend.Domain.Values;

namespace NobaRental.Backend.Domain;

public interface ICarFleetApi
{
    Task<Car> RegisterCar(
        string registrationNumber,
        string categoryCode,
        long initialMeterReadingKm,
        string stationCode,
        decimal baseDayRental,
        decimal baseKmPrice = 0m,
        CancellationToken cancellationToken = default);

    Task<Car> UpdateCarTariff(
        string registrationNumber,
        decimal baseDayRental,
        decimal baseKmPrice = 0m,
        byte[]? rowVersion = null,
        CancellationToken cancellationToken = default);

    Task DeleteCar(string registrationNumber, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Car>> GetAllCars(CancellationToken cancellationToken = default);

    Task<PagedResult<Car>> GetCars(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? stationCode = null,
        CarStatus? status = null,
        string? sortBy = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Car>> GetAvailableCars(
        string? stationCode = null,
        string? categoryCode = null,
        CancellationToken cancellationToken = default);

    Task<Car?> GetCarByRegistrationNumber(string registrationNumber, CancellationToken cancellationToken = default);
}
