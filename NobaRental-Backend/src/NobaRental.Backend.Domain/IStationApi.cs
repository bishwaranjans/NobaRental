using NobaRental.Backend.Domain.Models;

namespace NobaRental.Backend.Domain;

public interface IStationApi
{
    Task<Station> CreateStation(
        string code,
        string name,
        string city,
        CancellationToken cancellationToken = default);

    Task<Station> UpdateStation(
        string code,
        string name,
        string city,
        bool isActive,
        byte[]? rowVersion = null,
        CancellationToken cancellationToken = default);

    Task DeleteStation(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Station>> GetAllStations(bool includeInactive = false, CancellationToken cancellationToken = default);

    Task<Station?> GetStationByCode(string code, CancellationToken cancellationToken = default);
}
