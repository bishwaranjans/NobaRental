using DataCarStatus = NobaRental.Backend.Data.Entities.Values.CarStatusValue;
using DomainCarStatus = NobaRental.Backend.Domain.Values.CarStatus;

namespace NobaRental.Backend.Business.Mapping;

internal static class CarStatusMap
{
    public static DomainCarStatus ToDomain(this DataCarStatus value) =>
        value switch
        {
            DataCarStatus.Available => DomainCarStatus.Available,
            DataCarStatus.Rented => DomainCarStatus.Rented,
            DataCarStatus.Maintenance => DomainCarStatus.Maintenance,
            DataCarStatus.Decommissioned => DomainCarStatus.Decommissioned,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, $"Unsupported CarStatusValue: {value}")
        };

    public static DataCarStatus ToEntity(this DomainCarStatus status) =>
        status switch
        {
            DomainCarStatus.Available => DataCarStatus.Available,
            DomainCarStatus.Rented => DataCarStatus.Rented,
            DomainCarStatus.Maintenance => DataCarStatus.Maintenance,
            DomainCarStatus.Decommissioned => DataCarStatus.Decommissioned,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, $"Unsupported CarStatus: {status}")
        };
}
