using NobaRental.Backend.Domain.Models;
using NobaRental.Backend.Domain.Values;

namespace NobaRental.Backend.Domain;

public interface IRentalBookingApi
{
    Task<RentalBooking> RegisterPickup(
        string registrationNumber,
        string customerSsn,
        CarCategory category,
        string pickupStationCode,
        DateTimeOffset pickupDateTime,
        long pickupMeterReadingKm,
        decimal baseDayRental,
        decimal baseKmPrice,
        CancellationToken cancellationToken = default);

    Task<RentalBooking> RegisterReturn(
        long bookingNumber,
        string returnStationCode,
        DateTimeOffset returnDateTime,
        long returnMeterReadingKm,
        byte[]? rowVersion = null,
        CancellationToken cancellationToken = default);

    Task<RentalBooking?> GetBookingByNumber(long bookingNumber, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RentalBooking>> GetAllBookings(CancellationToken cancellationToken = default);

    Task<PagedResult<RentalBooking>> GetBookings(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? stationCode = null,
        RentalStatus? status = null,
        string? sortBy = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RentalBooking>> GetActiveBookings(CancellationToken cancellationToken = default);
}
