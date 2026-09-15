using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NobaRental.Backend.Data.Entities;

namespace NobaRental.Backend.Data.Configurations;

public class CarCategoryEntityConfiguration : IEntityTypeConfiguration<CarCategoryEntity>
{
    public void Configure(EntityTypeBuilder<CarCategoryEntity> b)
    {
        b.ToTable("CarCategory");
        b.HasKey(x => x.Code);
        ConfigureProperties(b);
        b.HasQueryFilter(x => !x.IsDeleted);
        ConfigureRelationships(b);
        b.HasData(GetSeedData());
    }

    private static void ConfigureProperties(EntityTypeBuilder<CarCategoryEntity> b)
    {
        b.Property(x => x.Code).HasMaxLength(20).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.DayMultiplier).HasPrecision(5, 2).IsRequired();
        b.Property(x => x.KmMultiplier).HasPrecision(5, 2).IsRequired();
        b.Property(x => x.ChargesKilometers).IsRequired();
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.IsDeleted).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();
    }

    private static void ConfigureRelationships(EntityTypeBuilder<CarCategoryEntity> b)
    {
        b.HasMany(x => x.Cars)
            .WithOne(x => x.Category)
            .HasForeignKey(x => x.CategoryCode)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Bookings)
            .WithOne(x => x.Category)
            .HasForeignKey(x => x.CategoryCode)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static CarCategoryEntity[] GetSeedData() =>
    [
        new()
        {
            Code = "SMALL",
            Name = "Small car",
            DayMultiplier = 1.0m,
            KmMultiplier = 0.0m,
            ChargesKilometers = false,
            IsActive = true
        },
        new()
        {
            Code = "COMBI",
            Name = "Combi",
            DayMultiplier = 1.3m,
            KmMultiplier = 1.0m,
            ChargesKilometers = true,
            IsActive = true
        },
        new()
        {
            Code = "TRUCK",
            Name = "Truck",
            DayMultiplier = 1.5m,
            KmMultiplier = 1.5m,
            ChargesKilometers = true,
            IsActive = true
        }
    ];
}
