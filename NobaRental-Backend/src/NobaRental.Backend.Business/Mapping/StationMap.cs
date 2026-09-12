using NobaRental.Backend.Data.Entities;
using NobaRental.Backend.Domain.Models;

namespace NobaRental.Backend.Business.Mapping;

internal static class StationMap
{
    public static Station Map(this StationEntity entity) =>
        new(
            Code: entity.Code,
            Name: entity.Name,
            City: entity.City,
            IsActive: entity.IsActive,
            IsDeleted: entity.IsDeleted,
            RowVersion: entity.RowVersion);

    public static StationEntity MapToEntity(
        string code,
        string name,
        string city,
        bool isActive = true) =>
        new()
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            City = city.Trim(),
            IsActive = isActive,
        };
}
