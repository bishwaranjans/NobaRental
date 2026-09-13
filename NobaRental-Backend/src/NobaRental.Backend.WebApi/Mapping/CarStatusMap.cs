using NobaRental.Backend.WebApi.Client.Models.Values;
using DomainCarStatus = NobaRental.Backend.Domain.Values.CarStatus;

namespace NobaRental.Backend.WebApi.Mapping;

internal static class CarStatusMap
{
    public static DomainCarStatus MapToDomain(this CarStatusDto dto) =>
        dto switch
        {
            CarStatusDto.Available => DomainCarStatus.Available,
            CarStatusDto.Rented => DomainCarStatus.Rented,
            CarStatusDto.Maintenance => DomainCarStatus.Maintenance,
            CarStatusDto.Decommissioned => DomainCarStatus.Decommissioned,
            _ => throw new ArgumentOutOfRangeException(nameof(dto), dto, $"Unsupported CarStatusDto: {dto}")
        };

    public static DomainCarStatus? MapToDomain(this CarStatusDto? dto) =>
        dto.HasValue ? dto.Value.MapToDomain() : null;

    public static CarStatusDto MapToResponse(this DomainCarStatus domain) =>
        domain switch
        {
            DomainCarStatus.Available => CarStatusDto.Available,
            DomainCarStatus.Rented => CarStatusDto.Rented,
            DomainCarStatus.Maintenance => CarStatusDto.Maintenance,
            DomainCarStatus.Decommissioned => CarStatusDto.Decommissioned,
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, $"Unsupported CarStatus: {domain}")
        };
}
