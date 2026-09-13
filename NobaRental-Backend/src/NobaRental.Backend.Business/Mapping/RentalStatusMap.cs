using DataRentalStatus = NobaRental.Backend.Data.Entities.Values.RentalStatusValue;
using DomainRentalStatus = NobaRental.Backend.Domain.Values.RentalStatus;

namespace NobaRental.Backend.Business.Mapping;

internal static class RentalStatusMap
{
    public static DomainRentalStatus ToDomain(this DataRentalStatus value) =>
        value switch
        {
            DataRentalStatus.Active => DomainRentalStatus.Active,
            DataRentalStatus.Completed => DomainRentalStatus.Completed,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, $"Unsupported RentalStatusValue: {value}")
        };

    public static DataRentalStatus ToEntity(this DomainRentalStatus status) =>
        status switch
        {
            DomainRentalStatus.Active => DataRentalStatus.Active,
            DomainRentalStatus.Completed => DataRentalStatus.Completed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, $"Unsupported RentalStatus: {status}")
        };
}
