using NobaRental.Backend.Business.Mapping;
using NobaRental.Backend.Data.Entities.Values;
using NobaRental.Backend.Domain.Values;

namespace NobaRental.Backend.Business.Test.Mapping;

public sealed class EnumMappingTests
{
    [Theory]
    [InlineData(CarStatusValue.Available, CarStatus.Available)]
    [InlineData(CarStatusValue.Rented, CarStatus.Rented)]
    [InlineData(CarStatusValue.Maintenance, CarStatus.Maintenance)]
    [InlineData(CarStatusValue.Decommissioned, CarStatus.Decommissioned)]
    public void CarStatus_ToDomain_MapsCorrectly(CarStatusValue dataVal, CarStatus expectedDomain)
    {
        Assert.Equal(expectedDomain, dataVal.ToDomain());
    }

    [Theory]
    [InlineData(CarStatus.Available, CarStatusValue.Available)]
    [InlineData(CarStatus.Rented, CarStatusValue.Rented)]
    [InlineData(CarStatus.Maintenance, CarStatusValue.Maintenance)]
    [InlineData(CarStatus.Decommissioned, CarStatusValue.Decommissioned)]
    public void CarStatus_ToEntity_MapsCorrectly(CarStatus domain, CarStatusValue expectedData)
    {
        Assert.Equal(expectedData, domain.ToEntity());
    }

    [Fact]
    public void CarStatus_ToDomain_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((CarStatusValue)999).ToDomain());
    }

    [Fact]
    public void CarStatus_ToEntity_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((CarStatus)999).ToEntity());
    }

    [Theory]
    [InlineData(RentalStatusValue.Active, RentalStatus.Active)]
    [InlineData(RentalStatusValue.Completed, RentalStatus.Completed)]
    public void RentalStatus_ToDomain_MapsCorrectly(RentalStatusValue dataVal, RentalStatus expectedDomain)
    {
        Assert.Equal(expectedDomain, dataVal.ToDomain());
    }

    [Theory]
    [InlineData(RentalStatus.Active, RentalStatusValue.Active)]
    [InlineData(RentalStatus.Completed, RentalStatusValue.Completed)]
    public void RentalStatus_ToEntity_MapsCorrectly(RentalStatus domain, RentalStatusValue expectedData)
    {
        Assert.Equal(expectedData, domain.ToEntity());
    }

    [Fact]
    public void RentalStatus_ToDomain_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((RentalStatusValue)999).ToDomain());
    }

    [Fact]
    public void RentalStatus_ToEntity_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((RentalStatus)999).ToEntity());
    }
}
