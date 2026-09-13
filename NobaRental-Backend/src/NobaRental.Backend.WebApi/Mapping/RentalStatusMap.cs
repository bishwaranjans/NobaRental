using NobaRental.Backend.WebApi.Client.Models.Values;
using DomainRentalStatus = NobaRental.Backend.Domain.Values.RentalStatus;

namespace NobaRental.Backend.WebApi.Mapping;

internal static class RentalStatusMap
{
    public static DomainRentalStatus MapToDomain(this RentalStatusDto dto) =>
        dto switch
        {
            RentalStatusDto.Active => DomainRentalStatus.Active,
            RentalStatusDto.Completed => DomainRentalStatus.Completed,
            _ => throw new ArgumentOutOfRangeException(nameof(dto), dto, $"Unsupported RentalStatusDto: {dto}")
        };

    public static DomainRentalStatus? MapToDomain(this RentalStatusDto? dto) =>
        dto.HasValue ? dto.Value.MapToDomain() : null;

    public static RentalStatusDto MapToResponse(this DomainRentalStatus domain) =>
        domain switch
        {
            DomainRentalStatus.Active => RentalStatusDto.Active,
            DomainRentalStatus.Completed => RentalStatusDto.Completed,
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, $"Unsupported RentalStatus: {domain}")
        };
}
