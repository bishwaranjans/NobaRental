using NobaRental.Backend.Data.Entities.Common;
using NobaRental.Backend.Data.Entities.Values;

namespace NobaRental.Backend.Data.Entities;

public class RentalBookingEntity : AuditableEntity
{
    public long BookingNumber { get; set; }

    public required string RegistrationNumber { get; set; }

    public required string CustomerSsn { get; set; }

    public CarCategoryValue Category { get; set; }

    public required string PickupStationCode { get; set; }

    public StationEntity? PickupStation { get; set; }

    public DateTimeOffset PickupDateTime { get; set; }

    public long PickupMeterReadingKm { get; set; }

    public string? ReturnStationCode { get; set; }

    public StationEntity? ReturnStation { get; set; }

    public DateTimeOffset? ReturnDateTime { get; set; }

    public long? ReturnMeterReadingKm { get; set; }

    public decimal BaseDayRental { get; set; }

    public decimal BaseKmPrice { get; set; }

    public int? CalculatedDays { get; set; }

    public long? CalculatedKm { get; set; }

    public decimal? TotalPrice { get; set; }

    public required string Currency { get; set; }

    public RentalStatusValue Status { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public CarEntity? Car { get; set; }
}
