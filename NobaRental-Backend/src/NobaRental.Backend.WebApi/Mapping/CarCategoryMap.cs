using NobaRental.Backend.WebApi.Client.Models.Values;
using DomainCarCategory = NobaRental.Backend.Domain.Values.CarCategory;

namespace NobaRental.Backend.WebApi.Mapping;

internal static class CarCategoryMap
{
    public static DomainCarCategory MapToDomain(this CarCategoryDto dto) =>
        dto switch
        {
            CarCategoryDto.SmallCar => DomainCarCategory.SmallCar,
            CarCategoryDto.Combi => DomainCarCategory.Combi,
            CarCategoryDto.Truck => DomainCarCategory.Truck,
            _ => throw new ArgumentOutOfRangeException(nameof(dto), dto, $"Unsupported CarCategoryDto: {dto}")
        };

    public static DomainCarCategory? MapToDomain(this CarCategoryDto? dto) =>
        dto.HasValue ? dto.Value.MapToDomain() : null;

    public static CarCategoryDto MapToResponse(this DomainCarCategory domain) =>
        domain switch
        {
            DomainCarCategory.SmallCar => CarCategoryDto.SmallCar,
            DomainCarCategory.Combi => CarCategoryDto.Combi,
            DomainCarCategory.Truck => CarCategoryDto.Truck,
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, $"Unsupported CarCategory: {domain}")
        };
}
