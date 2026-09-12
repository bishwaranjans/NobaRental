using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NobaRental.Backend.Data.Entities;

namespace NobaRental.Backend.Data.Configurations;

public class StationEntityConfiguration : IEntityTypeConfiguration<StationEntity>
{
    public void Configure(EntityTypeBuilder<StationEntity> b)
    {
        b.ToTable("Station");

        b.HasKey(x => x.Code);

        b.Property(x => x.Code)
            .HasMaxLength(10)
            .IsRequired();

        b.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        b.Property(x => x.City)
            .HasMaxLength(50)
            .IsRequired();

        b.Property(x => x.IsActive)
            .IsRequired();

        b.Property(x => x.IsDeleted)
            .IsRequired();

        b.Property(x => x.RowVersion)
            .IsRowVersion();

        b.HasQueryFilter(x => !x.IsDeleted);

        b.HasMany(x => x.Cars)
            .WithOne(x => x.CurrentStation)
            .HasForeignKey(x => x.CurrentStationCode)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.PickupBookings)
            .WithOne(x => x.PickupStation)
            .HasForeignKey(x => x.PickupStationCode)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.ReturnBookings)
            .WithOne(x => x.ReturnStation)
            .HasForeignKey(x => x.ReturnStationCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
