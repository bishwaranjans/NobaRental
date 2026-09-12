using NobaRental.Backend.Data.Entities.Common;
using NobaRental.Backend.Data.Entities.Values;

namespace NobaRental.Backend.Data.Entities;

public class CarEntity : SoftDeletableEntity
{
    public required string RegistrationNumber { get; set; }

    public CarCategoryValue Category { get; set; }

    public long CurrentMeterReadingKm { get; set; }

    public CarStatusValue Status { get; set; }

    public required string CurrentStationCode { get; set; }

    public StationEntity? CurrentStation { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public ICollection<RentalBookingEntity> Bookings { get; set; } = [];
}
