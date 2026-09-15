using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NobaRental.Backend.Data.Entities;

namespace NobaRental.Backend.Data.Configurations;

public class CarEntityConfiguration : IEntityTypeConfiguration<CarEntity>
{
    public void Configure(EntityTypeBuilder<CarEntity> b)
    {
        b.ToTable("Car");

        b.HasKey(x => x.RegistrationNumber);

        b.Property(x => x.RegistrationNumber)
            .HasMaxLength(20)
            .IsRequired();

        b.Property(x => x.CategoryCode)
            .HasMaxLength(20)
            .IsRequired();

        b.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        b.Property(x => x.CurrentStationCode)
            .HasMaxLength(10)
            .IsRequired();

        b.Property(x => x.IsDeleted)
            .IsRequired();

        b.Property(x => x.RowVersion)
            .IsRowVersion();

        b.Property(x => x.BaseDayRental)
            .HasPrecision(18, 2)
            .IsRequired();

        b.Property(x => x.BaseKmPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        b.HasQueryFilter(x => !x.IsDeleted);

        b.HasOne(x => x.Category)
            .WithMany(x => x.Cars)
            .HasForeignKey(x => x.CategoryCode)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        b.HasOne(x => x.CurrentStation)
            .WithMany(x => x.Cars)
            .HasForeignKey(x => x.CurrentStationCode)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        b.HasMany(x => x.Bookings)
            .WithOne(x => x.Car)
            .HasForeignKey(x => x.RegistrationNumber)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
