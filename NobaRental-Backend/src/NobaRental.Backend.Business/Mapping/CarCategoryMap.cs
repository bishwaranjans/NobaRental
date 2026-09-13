using DataCarCategory = NobaRental.Backend.Data.Entities.Values.CarCategoryValue;
using DomainCarCategory = NobaRental.Backend.Domain.Values.CarCategory;

namespace NobaRental.Backend.Business.Mapping;

internal static class CarCategoryMap
{
    public static DomainCarCategory ToDomain(this DataCarCategory value) =>
        value switch
        {
            DataCarCategory.SmallCar => DomainCarCategory.SmallCar,
            DataCarCategory.Combi => DomainCarCategory.Combi,
            DataCarCategory.Truck => DomainCarCategory.Truck,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, $"Unsupported CarCategoryValue: {value}")
        };

    public static DataCarCategory ToEntity(this DomainCarCategory category) =>
        category switch
        {
            DomainCarCategory.SmallCar => DataCarCategory.SmallCar,
            DomainCarCategory.Combi => DataCarCategory.Combi,
            DomainCarCategory.Truck => DataCarCategory.Truck,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, $"Unsupported CarCategory: {category}")
        };
}
