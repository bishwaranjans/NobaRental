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

    public DbSet<CarCategoryEntity> CarCategories => Set<CarCategoryEntity>();

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
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<string>().AreUnicode(false);
    }
}