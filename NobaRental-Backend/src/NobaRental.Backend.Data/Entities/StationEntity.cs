using NobaRental.Backend.Data.Entities.Common;

namespace NobaRental.Backend.Data.Entities;

public class StationEntity : SoftDeletableEntity
{
    public required string Code { get; set; }

    public required string Name { get; set; }

    public required string City { get; set; }

    public bool IsActive { get; set; } = true;

    public byte[] RowVersion { get; set; } = [];

    public ICollection<CarEntity> Cars { get; set; } = [];

    public ICollection<RentalBookingEntity> PickupBookings { get; set; } = [];

    public ICollection<RentalBookingEntity> ReturnBookings { get; set; } = [];
}
