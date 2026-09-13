using Microsoft.EntityFrameworkCore;
using NobaRental.Backend.Data.Entities;
using NobaRental.Backend.Data.Entities.Common;

namespace NobaRental.Backend.Data;

public class NobaRentalDbContext(
    DbContextOptions<NobaRentalDbContext> options,
    TimeProvider? timeProvider = null)
    : DbContext(options)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public DbSet<RentalBookingEntity> RentalBookings => Set<RentalBookingEntity>();

    public DbSet<CarEntity> Cars => Set<CarEntity>();

    public DbSet<StationEntity> Stations => Set<StationEntity>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditAndSoftDelete();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditAndSoftDelete();
        return base.SaveChanges();
    }

    private void ApplyAuditAndSoftDelete()
    {
        var utcNow = _timeProvider.GetUtcNow();

        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = utcNow;
            }
        }

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = utcNow;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAt = utcNow;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

        var defaultCreated = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        modelBuilder.Entity<StationEntity>().HasData(
            new StationEntity { Code = "OSL", Name = "Oslo Airport Gardermoen", City = "Oslo", IsActive = true, IsDeleted = false, CreatedAt = defaultCreated },
            new StationEntity { Code = "BGO", Name = "Bergen Airport Flesland", City = "Bergen", IsActive = true, IsDeleted = false, CreatedAt = defaultCreated },
            new StationEntity { Code = "TRD", Name = "Trondheim Airport Værnes", City = "Trondheim", IsActive = true, IsDeleted = false, CreatedAt = defaultCreated },
            new StationEntity { Code = "SVG", Name = "Stavanger Airport Sola", City = "Stavanger", IsActive = true, IsDeleted = false, CreatedAt = defaultCreated },
            new StationEntity { Code = "OSLO-C", Name = "Oslo Central Station", City = "Oslo", IsActive = true, IsDeleted = false, CreatedAt = defaultCreated });

        modelBuilder.Entity<CarEntity>().HasData(
            new CarEntity { RegistrationNumber = "EV12345", Category = Entities.Values.CarCategoryValue.SmallCar, CurrentMeterReadingKm = 1000, Status = Entities.Values.CarStatusValue.Available, CurrentStationCode = "OSL", BaseDayRental = 500m, BaseKmPrice = 0m, IsDeleted = false, CreatedAt = defaultCreated },
            new CarEntity { RegistrationNumber = "BT20001", Category = Entities.Values.CarCategoryValue.Combi, CurrentMeterReadingKm = 5000, Status = Entities.Values.CarStatusValue.Available, CurrentStationCode = "OSL", BaseDayRental = 700m, BaseKmPrice = 2.5m, IsDeleted = false, CreatedAt = defaultCreated },
            new CarEntity { RegistrationNumber = "TR99001", Category = Entities.Values.CarCategoryValue.Truck, CurrentMeterReadingKm = 15000, Status = Entities.Values.CarStatusValue.Available, CurrentStationCode = "BGO", BaseDayRental = 1200m, BaseKmPrice = 4m, IsDeleted = false, CreatedAt = defaultCreated });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<string>().AreUnicode(false);
    }
}