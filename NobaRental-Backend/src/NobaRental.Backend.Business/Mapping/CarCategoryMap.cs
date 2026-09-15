using NobaRental.Backend.Data.Entities;
using NobaRental.Backend.Domain.Models;

namespace NobaRental.Backend.Business.Mapping;

internal static class CarCategoryMap
{
    public static CarCategory Map(this CarCategoryEntity entity) =>
        new(
            Code: entity.Code,
            Name: entity.Name,
            DayMultiplier: entity.DayMultiplier,
            KmMultiplier: entity.KmMultiplier,
            ChargesKilometers: entity.ChargesKilometers,
            IsActive: entity.IsActive,
            IsDeleted: entity.IsDeleted,
            RowVersion: entity.RowVersion);

    public static CarCategoryEntity MapToEntity(
        string code,
        string name,
        decimal dayMultiplier,
        decimal kmMultiplier,
        bool chargesKilometers,
        bool isActive = true) =>
        new()
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            DayMultiplier = dayMultiplier,
            KmMultiplier = kmMultiplier,
            ChargesKilometers = chargesKilometers,
            IsActive = isActive,
        };
}
