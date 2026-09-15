using NobaRental.Backend.Data.Entities.Common;

namespace NobaRental.Backend.Data.Entities;

public class CarCategoryEntity : SoftDeletableEntity
{
    public required string Code { get; set; }

    public required string Name { get; set; }

    public decimal DayMultiplier { get; set; } = 1.0m;

    public decimal KmMultiplier { get; set; } = 0.0m;

    public bool ChargesKilometers { get; set; }

    public bool IsActive { get; set; } = true;

    public byte[] RowVersion { get; set; } = [];

    public ICollection<CarEntity> Cars { get; set; } = [];

    public ICollection<RentalBookingEntity> Bookings { get; set; } = [];
}
