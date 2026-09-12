using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NobaRental.Backend.Data.Entities;

namespace NobaRental.Backend.Data.Configurations;

public class RentalBookingEntityConfiguration : IEntityTypeConfiguration<RentalBookingEntity>
{
    public void Configure(EntityTypeBuilder<RentalBookingEntity> b)
    {
        b.ToTable("RentalBooking");

        b.HasKey(x => x.BookingNumber);
        b.Property(x => x.BookingNumber).ValueGeneratedOnAdd();

        ConfigureProperties(b);
        ConfigureRelationships(b);
    }

    private static void ConfigureProperties(EntityTypeBuilder<RentalBookingEntity> b)
    {
        b.Property(x => x.RegistrationNumber).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.RegistrationNumber);

        b.Property(x => x.CustomerSsn).HasMaxLength(20).IsRequired();
        b.Property(x => x.Category).HasConversion<int>().IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.Currency).HasMaxLength(5).IsRequired();

        b.Property(x => x.BaseDayRental).HasPrecision(18, 2);
        b.Property(x => x.BaseKmPrice).HasPrecision(18, 2);
        b.Property(x => x.TotalPrice).HasPrecision(18, 2);

        b.Property(x => x.PickupStationCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.ReturnStationCode).HasMaxLength(10);
        b.Property(x => x.RowVersion).IsRowVersion();
    }

    private static void ConfigureRelationships(EntityTypeBuilder<RentalBookingEntity> b)
    {
        b.HasOne(x => x.Car)
            .WithMany(x => x.Bookings)
            .HasForeignKey(x => x.RegistrationNumber)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        b.HasOne(x => x.PickupStation)
            .WithMany(x => x.PickupBookings)
            .HasForeignKey(x => x.PickupStationCode)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        b.HasOne(x => x.ReturnStation)
            .WithMany(x => x.ReturnBookings)
            .HasForeignKey(x => x.ReturnStationCode)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
